using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TLTLUnity.Data;

namespace TLTLUnity.Agents
{

    class DistanceToGoalBFS
    {
        public static void ComputeDistancesToGoal(int startState, Dictionary<int, int> distances, TLTLRewardAutomaton automaton)
        {
            if (distances.ContainsKey(startState)) return;
            distances.Add(startState, int.MaxValue);

            int bestDistance = int.MaxValue;

            TLTLRewardAutomatonState currentState = automaton.States[startState];

            foreach (var transition in currentState.Transitions)
            {
                int neighbor = transition.endState;
                ComputeDistancesToGoal(neighbor, distances, automaton);

                // tras esta llamada ya est�n calculadas las distancias del vecino, por
                // lo que podemos calcular esta como la menor de ellas +1.
                if (distances[neighbor] < bestDistance)
                {
                    bestDistance = distances[neighbor];
                }
            }

            // si estamos en un estado de goal, a�adimos la mejor distancia es 0
            if (currentState.IsAcceptingState(automaton.AcceptancePairs))
            {
                distances[startState] = 0;
                return;
            }

            // si la distancia era inf, la dejamos igual (denota imposibilidad de alcanzar el estado final)
            distances[startState] = bestDistance == int.MaxValue ? int.MaxValue : bestDistance + 1;
        } // ComputeDistancesToGoal

        public static Dictionary<int, int> ComputeDistancesToGoal(TLTLRewardAutomaton automaton)
        {
            Dictionary<int, int> distances = new Dictionary<int, int>();
            ComputeDistancesToGoal(0, distances, automaton);
            return distances;
        } // ComputeDistancesToGoal
    } // DistanceToGoalBFS


    public class TLTLRewardAutomaton
    {
        /// <summary>
        /// Estado del aut�mata en el momento actual.
        /// </summary>
        public enum AutomatonStatus
        {
            RUNNING, WORD_REJECTED, WORD_ACCEPTED
        }

        /// <summary>
        /// Determina si el aut�mata puede seguir recibiendo ticks a partir del primer 
        /// instante en el que alcanza un estado potencialmente terminal. Si la tarea
        /// a realizar tiene un objetivo con un fin claro, este par�metro deber�a mantenerse
        /// a true. Si por el contrario la tarea define un comportamiento en el que se 
        /// desea optimizar un cierto objetivo o mantener una condici�n durante el m�ximo
        /// tiempo posible (i.e. "no morir"), entonces es necesario marcar este par�metro como
        /// false para permitir que contin�e con la ejecuci�n.
        /// </summary>
        public bool stopOnAcceptance = true;

        /// <summary>
        /// Pendiente aplicada como factor multiplicativo sobre la robustez computada
        /// cada vez que se realiza una transici�n.
        /// </summary>
        public float objectiveRegionSteepness = 1f;

        /// <summary>
        /// Estados disponibles en esta m�quina de estados.
        /// </summary>
        public TLTLRewardAutomatonState[] States { get; private set; }

        /// <summary>
        /// parejas de aceptaci�n para el aut�mata
        /// </summary>
        public RabinAcceptancePair[] AcceptancePairs { get; private set; }

        /// <summary>
        /// Estado actualmente activo dentro de los listados en states.
        /// Si este estado se encuentra en null, se entiende que la ejecuci�n del aut�mata
        /// se ha interrumpido.
        /// </summary>
        public int ActiveStateIndex { get; private set; }

        /// <summary>
        /// N�mero de ticks que ha recibido el aut�mata a lo largo de la ejecuci�n actual.
        /// </summary>
        public int TickCount { get; private set; }

        public TLTLRewardAutomatonState CurrentState { get { return States[ActiveStateIndex]; } }

        /// <summary>
        /// Estado con el que inicializar la m�quina de estados en el momento
        /// de su lanzamiento.
        /// </summary>
        public int InitialStateIndex { get; private set; }

        public float LastTickReward { get; private set; }

        public float MaxReward { get; private set; }

        public AutomatonStatus Status { get; private set; }

        public int MaxDistanceToGoal { get; private set; }

        public Dictionary<int, int> DistanceToGoal { get; private set; }

        public int CurrentDistanceToGoal
        {
            get
            {
                return ActiveStateIndex >= 0 ? DistanceToGoal[ActiveStateIndex] : -1;
            }
        }

        public int StateCount
        {
            get { return States.Length; }
        }

        public TLTLRewardAutomaton(
            TLTLRewardAutomatonState[] states,
            int initialState,
            RabinAcceptancePair[] acceptancePairs
        )
        {
            States = states;
            InitialStateIndex = initialState;
            AcceptancePairs = acceptancePairs;
            TickCount = 0;

            DistanceToGoal = DistanceToGoalBFS.ComputeDistancesToGoal(this);
            // s�lo tenemos en cuenta las distancias no infinitas para el c�lculo
            MaxDistanceToGoal = DistanceToGoal.Max(pair => pair.Value == int.MaxValue ? -1 : pair.Value);

            ResetMachineState();
        } // RewardFiniteStateMachine

        /// <summary>
        /// Eval�a los predicados de todas las transiciones asociadas al estado actualmente
        /// activo y aplica aquella con mayor valor de robustez. En el proceso, computa el premio 
        /// a aplicar al agente por esta transici�n. Este m�todo debe llamarse en principio tras
        /// realizar una acci�n para ser consistentes con el algoritmo 1 en el paper
        /// "RL Agent Training with Goals for Real World Tasks".
        /// </summary>
        public void TickStateMachine()
        {
            // -----------------------------------------------------------------------
            // BEGIN TICK
            // -----------------------------------------------------------------------
            TickCount++;
            float reward = 0f;
            Debug.LogFormat("[RewardFiniteStateMachine] Active state index: {0}", ActiveStateIndex);

            if (Status != AutomatonStatus.RUNNING && stopOnAcceptance)
            {
                Debug.LogFormat("[RewardFiniteStateMachine] Automaton was ticked, but it is no longer running. Current Status: {0}", Status.ToString());
                return;
            }

            // -----------------------------------------------------------------------
            // Take best transition based on robustness. 
            // -----------------------------------------------------------------------

            // transiciones que mejorar�an nuestra distancia actual a un estado objetivo
            // el punto aqu� es que si no cambiamos de estado durante este tick, la m�trica 
            // que nos indicar� cu�nto nos estamos acercando a un estado bueno ser� la mejor robustez
            // de entre todas las aristas salientes de mejora de distancia. intuitivamente, esto
            // se corresponde con "cu�nto queda para tomar una transici�n deseable".
            List<TLTLRewardAutomatonEdge> improvingTransitions = new List<TLTLRewardAutomatonEdge>();

            // mejor robustez en general (determinar� qu� transici�n se toma)
            float bestRobustness = float.MinValue;
            // mejor robustez de entre dentro de las transiciones de mejora de distancia
            float bestImprovingRobustness = float.MinValue;
            // transici�n que se tomar� en base a la robustez m�xima
            TLTLRewardAutomatonEdge mostRobustEdge = null;

            // b�squeda de la mejor robustez.
            // a la par vamos rellenando la lista de transiciones de mejora
            // seg�n las vamos encontrando sobre la marcha
            foreach (var transition in CurrentState.Transitions)
            {
                float r = transition.predicate.EvaluateRobustness();
                if (r > bestRobustness)
                {
                    mostRobustEdge = transition;
                    bestRobustness = r; // actualizaci�n de la mejor robustez
                }

                if (DistanceToGoal[transition.endState] < DistanceToGoal[ActiveStateIndex])
                {
                    // la transici�n mejorar�a nuestra distancia actual a un objetivo
                    improvingTransitions.Add(transition);

                    if (r > bestImprovingRobustness)
                    {
                        bestImprovingRobustness = r; // actualizaci�n de la mejor robustez de mejora
                    }
                }
            }

            // distancia del siguiente estado al objetivo
            int nextStateDistanceToGoal = DistanceToGoal[mostRobustEdge.endState];
            // el premio base desde el estado actual ser� la noci�n de "progreso"
            // es decir, cu�ntos pasos hemos avanzado para llegar a un estado de aceptaci�n
            // si la distancia m�xima es 4, por ejemplo, y nuestra distancia al objetivo es 1
            // entonces podemos decir que llevamos, al menos, un 75% de la tarea completada
            // y el premio base ser�a (4-1)/4 = 0.75.
            reward += ((float)MaxDistanceToGoal - nextStateDistanceToGoal) / MaxDistanceToGoal;

            // si nos fu�ramos a quedar en el estado actual, el premio viene ajustado por 
            // la mejor robustez que mejorar�a la distancia, ya que esta informa sobre
            // cu�nto queda para avanzar la tarea (si por ejemplo esa robustez fuera -0.5,
            // significa que hemos avanzado en la tarea actual un 50% y nos falta otro 50%).
            // Podr�a darse el caso,
            // no obstante, de que no existiera ninguna transici�n que nos permitiera 
            // mejorar. En este �ltimo caso, lo que ocurre es que o estamos atascados
            // en un estado de trampa desde el que no podemos alcanzar nunca un objetivo,
            // o en un estado objetivo desde el que no podemos mejorar porque ya es lo 
            // mejor posible.
            if (mostRobustEdge.endState == ActiveStateIndex)
            {
                // caso 1: no hay ninguna transici�n mejor
                if (improvingTransitions.Count == 0)
                {
                    if (mostRobustEdge.IsAcceptingTransition(AcceptancePairs))
                    {
                        Status = AutomatonStatus.WORD_ACCEPTED;
                        if (!stopOnAcceptance)
                        {
                            reward += bestRobustness;
                        }
                    }
                    else
                    {
                        Status = AutomatonStatus.WORD_REJECTED;
                        reward = 0f;
                    }
                    // nuestra propia robustez: + si estamos en aceptaci�n, - en caso contrario (atrapados)
                    // Nota: por c�mo se construyen los aut�matas de Rabin, si llegamos a un estado
                    // de trampa en el que no tengamos ninguna otra transici�n m�s all� de al propio
                    // estado, esta estar� siempre etiquetada con T.
                    // Este no es el caso con los estados objetivo, ni tampoco se pretende que lo sea:
                    // lo que interesa en esos casos es que el agente aprenda a mejorar la robustez
                    // en el estado actual lo m�ximo posible (tarea de optimizaci�n).
                }
                else
                {
                    // siempre estaremos aqu� en un estado intermedio, si hubi�ramos estado
                    // en un estado de aceptaci�n no habr�a ninguna transici�n que lograse 
                    // mejorar nuestra distancia actual.
                    Status = AutomatonStatus.RUNNING;
                    // adem�s, no hemos tomado la transici�n saliente, por lo que podemos asumir 
                    // que su robustez era negativa para que as� el premio concedido sea
                    // el premio base por estado + cu�nto hemos avanzado para realizar un paso hacia el objetivo.
                    reward += (1 + bestImprovingRobustness) / MaxDistanceToGoal;
                }
            }

            // -----------------------------------------------------------------------
            // END TICK
            // -----------------------------------------------------------------------

            // (1) sAutomata(t) <- sAutomata(t + 1)
            // avanzamos el estado actual con el estado calculado durante el tick
            ActiveStateIndex = mostRobustEdge.endState;

            // mostramos mensaje de prompt por pantalla en caso de que hubiera alguno
            Debug.Log(CurrentState.promptText);

            LastTickReward = reward;
            MaxReward = Mathf.Max(MaxReward, reward);

            // guardamos el premio en la propiedad LastTickReward para poder acceder a ella desde 
            // fuera.
        } // TickStateMachine

        /// <summary>
        /// Devuelve a la m�quina de estados a su estado original.
        /// </summary>
        public void Reset()
        {
            // -----------------------------------------------------------------------
            // BEGIN REWARD GENERATION
            // -----------------------------------------------------------------------
            ResetMachineState();
        } // Reset


        private void ResetMachineState()
        {
            Debug.Log("[RewardFiniteStateMachine] Resetting machine state.");
            // (1) Initialize MDP state as s0_mdp.
            // (2) Initialize Automata state as s0_automata.
            ActiveStateIndex = InitialStateIndex;

            TickCount = 0;
            LastTickReward = 0f;
            MaxReward = 0f;

            // restablece el estado por defecto de todos los predicados utilizados
            foreach (var state in States)
            {
                foreach (var edge in state.Transitions)
                {
                    edge.predicate.ResetPredicate();
                }
            }

            Status = AutomatonStatus.RUNNING;

            Debug.Log(CurrentState.promptText);
        } // ResetMachineState
    } // TLTLRewardAutomaton
} // namespace TLTLUnity.Agents