using System;
using System.Collections.Generic;
using TLTLCore;
using TLTLUnity.Data;
using UnityEngine;
using UnityEngine.Events;

namespace TLTLUnity.Agents
{
    /// <summary>
    /// Componente que gestiona una m�quina de estados de premios a partir de un recurso de especificaci�n
    /// de comportamiento en l�gica temporal.
    /// 
    /// Mantiene una <see cref="UnityBlackboard"/> para poder especificar desde el editor los par�metros requeridos para
    /// definir el comportamiento dado por medio de <see cref="predicateFile"/>, y expone un <see cref="rewardAutomaton"/>
    /// para poder acceder al aut�mata desde fuera.
    /// </summary>
    public class TLTLRewardAutomatonComponent : MonoBehaviour
    {
        public enum RewardPolicy
        {
            StrictProgress, CompletedSpecificationOnly
        }

        //----------------------------------------------------------------------------
        //                  Variables p�blicas del componente
        //----------------------------------------------------------------------------
        #region Variables p�blicas del componente
        [Header("Blackboard Variables")]
        [SerializeField]
        [Tooltip("Pizarra que contiene los Par�metros pasados al comportamiento serializados por Unity")]
        public UnityBlackboard blackboard = new UnityBlackboard();

        [SerializeField]
        [Tooltip("Referencia al asset que incluye la informaci�n pura del aut�mata.")]
        private RabinAutomatonDataAsset automatonFile;

        [Header("Reward Policy")]
        [SerializeField]
        [Tooltip("Pol�tica empleada para conceder premios al agente durante el avance por el aut�mata. StrictProgress mantiene un registro del premio m�ximo" +
            " del aut�mata en el episodio actual y s�lo se concede un premio cuando el agente supera este m�ximo. Esto garantiza que el premio del episodio " +
            "est� entre 0 y 1, pero puede ser necesario incluir una penalizaci�n existencial simult�nea para garantizar que el agente trate de cumplir la " +
            "especificaci�n en el menor tiempo posible.")]
        private RewardPolicy rewardPolicy = RewardPolicy.StrictProgress;

        [SerializeField]
        [Tooltip("Determina si se aplica una penalizaci�n en cada acci�n con el valor especificado en exitentialPenaltyValue")]
        private bool applyExistentialPenalty = false;

        [SerializeField]
        [Tooltip("Valor de la penalizaci�n por acci�n. Si el episodio dura N pasos y se toma una acci�n por paso, esto se corresponde con 1/N." +
            "Si no se toma una acci�n por paso, la penalizaci�n debe ajustarse.")]
        private float existentialPenaltyValue = 0.0005f;

        [SerializeField]
        [Tooltip("Evento al que suscribirse para recibir las se�ales de premio del aut�mata de premios cada vez que se lanza un tick.")]
        public UnityEvent<float> RewardGenerated;

        [SerializeField]
        [Tooltip("Evento al que suscribirse para recibir las se�ales de penalizaci�n del aut�mata de premios cada vez que se lanza un tick.")]
        public UnityEvent<float> PenaltyGenerated;

        [SerializeField]
        [Tooltip("Evento al que suscribirse para recibir la se�al de aceptaci�n de una traza por parte del aut�mata.")]
        public UnityEvent TraceAccepted;

        [SerializeField]
        [Tooltip("Evento al que suscribirse para recibir la se�al de rechazo de una traza por parte del aut�mata.")]
        public UnityEvent TraceRejected;
        #endregion

        /// <summary>
        /// Aut�mata de premios generado a partir del predicado introducido como par�metro desde el editor.
        /// Es responsabilidad de este agente llamar al tick del aut�mata de premios con cada paso de la
        /// academia para garantizar que se asigna el premio correspondiente.
        /// </summary>
        public TLTLRewardAutomaton rewardAutomaton;

        //----------------------------------------------------------------------------
        //                  M�todos relacionados con el editor
        //----------------------------------------------------------------------------
        #region M�todos relacionados con el editor

        /// <summary>
        /// Llamado por Unity cuando se cambia alguna propiedad en el inspector.
        /// </summary>
        /// Revisa el comportamiento, actualizando si es necesario
        /// los par�metros almacenados de la pizarra.
        public void OnValidate()
        {
            UpdateBlackboardParams();
        } // OnValidate

        private void UpdateBlackboardParams()
        {
            if ((automatonFile != null))
            {
                List<TLTLBlackboardData.Entry> variables = automatonFile.BlackboardData.Parameters;
                InParamValues paramValues = new InParamValues();
                foreach (var variable in variables)
                {
                    Type variableType = Type.GetType(variable.type);
                    paramValues.values[variable.name] = new InParamValue(variable.name, variableType);
                }
                blackboard.updateParams(paramValues);
            }
            else
                blackboard.updateParams(null);
        } // UpdateBlackboardParams

        public void BuildRuntimeAutomaton()
        {
            if (automatonFile == null)
            {
                Debug.LogWarning("TLTL Character Controller without automaton. Check the object inspector");
                return;
            }

            UpdateBlackboardParams();

            if (automatonFile != null)
            {
                // construimos el aut�mata de premio que ser� utilizado en los pasos de la academia para 
                // definir una funci�n de premio a partir de las variables de la pizarra y del fichero de estructura
                // del aut�mata de Rabin.
                rewardAutomaton = RabinAutomatonDataAssetTools.BuildRuntimeAutomaton(automatonFile, blackboard.BuildBlackboard());
            }
        } // BuildRuntimeAutomaton
        #endregion


        //----------------------------------------------------------------------------
        // 
        //----------------------------------------------------------------------------
        #region

        public void TickAutomaton()
        {
            // hacemos un tick al aut�mata de premio, que deja el premio
            // de PROGRESO asociado al �ltimo estado (un n�mero entre 0 y 1
            // en funci�n del estado de avance de la especificaci�n) en LastTickReward.
            // Este premio NO es necesariamente el premio que se le da al agente,
            // podemos estar interesados en ajustarlo en funci�n de la variante del
            // algoritmo que queramos aplicar.
            /// <see cref="RewardPolicy"/>
            rewardAutomaton.TickStateMachine();
            float reward = rewardAutomaton.LastTickReward;

            // La primera variante considera S�LO dar como premio al agente mejoras
            // en el progreso. Esto quiere decir que vamos guard�ndonos el premio
            // m�ximo y cada vez que el premio actual lo supere registramos esa mejora
            // como premio al agente y actualizamos el m�ximo. En la pr�ctica es mejor
            // reservarnos estas actualizaciones para cuando las mejoras superen un cierto
            // umbral para evitar problemas de redondeo a 0 en variaciones muy peque�as
            // en MLAgents (premios excesivamente peque�os se acaban ignorando si no).
            if (rewardPolicy == RewardPolicy.StrictProgress)
            {
                if (reward < rewardAutomaton.MaxReward + existentialPenaltyValue)
                {
                    // no se logr� mejorar el premio actual (no hay progreso) ->
                    // el premio concedido pasa a ser 0
                    reward = 0f;
                }
                else
                {
                    // Hay una mejora respecto al mejor premio registrado
                    // el premio concedido es la diferencia
                    reward -= rewardAutomaton.MaxReward;
                }
            }

            // Caso final: la palabra ya se ha rechazado o aceptado
            if (rewardAutomaton.Status != TLTLRewardAutomaton.AutomatonStatus.RUNNING)
            {

                // Modo CompletedSpecificationOnly: este es el �nico momento en el que concedemos premio bajo esta
                // modalidad, y el premio es sencillamente 1 si acab� en �xito y -1 si acaba en fracaso.
                if (rewardPolicy == RewardPolicy.CompletedSpecificationOnly)
                {
                    reward = rewardAutomaton.Status == TLTLRewardAutomaton.AutomatonStatus.WORD_ACCEPTED ? 1 : -1;
                    RewardGenerated?.Invoke(reward);
                }

                //--------------------------------------------------------------
                //     Eventos de finalizaci�n lanzados en cualquier caso.
                //--------------------------------------------------------------

                // (1) Aceptaci�n de palabra
                if (rewardAutomaton.Status == TLTLRewardAutomaton.AutomatonStatus.WORD_ACCEPTED)
                {
                    TraceAccepted?.Invoke();
                }
                // (2) Rechazo de palabra
                else if (rewardAutomaton.Status == TLTLRewardAutomaton.AutomatonStatus.WORD_REJECTED)
                {
                    TraceRejected?.Invoke();
                }
            }
            // caso en running (s�lo aplica si permitimos premios intermedios)
            else
            {
                if (rewardPolicy != RewardPolicy.CompletedSpecificationOnly)
                {
                    RewardGenerated?.Invoke(reward);
                }
                // penalizaci�n existencial
                float penalty = -existentialPenaltyValue;
                if (applyExistentialPenalty)
                {
                    PenaltyGenerated?.Invoke(penalty);
                }
            }
        }
        #endregion

        public bool SetBehaviorParam(string paramName, object value)
        {
            return blackboard.SetBehaviorParam(paramName, value);
        }
    } // BaseMLCharacterController

} // namespace TLTLUnity.Agents