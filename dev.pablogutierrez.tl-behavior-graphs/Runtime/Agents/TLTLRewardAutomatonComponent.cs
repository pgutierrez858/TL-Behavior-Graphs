using System;
using System.Collections.Generic;
using TLTLCore;
using TLTLUnity.Data;
using UnityEngine;
using UnityEngine.Events;

namespace TLTLUnity.Agents
{
    /// <summary>
    /// Componente que gestiona una máquina de estados de premios a partir de un recurso de especificación
    /// de comportamiento en lógica temporal.
    /// 
    /// Mantiene una <see cref="UnityBlackboard"/> para poder especificar desde el editor los parámetros requeridos para
    /// definir el comportamiento dado por medio de <see cref="predicateFile"/>, y expone un <see cref="rewardAutomaton"/>
    /// para poder acceder al autómata desde fuera.
    /// </summary>
    public class TLTLRewardAutomatonComponent : MonoBehaviour
    {
        public enum RewardPolicy
        {
            StrictProgress, CompletedSpecificationOnly
        }

        //----------------------------------------------------------------------------
        //                  Variables públicas del componente
        //----------------------------------------------------------------------------
        #region Variables públicas del componente
        [Header("Blackboard Variables")]
        [SerializeField]
        [Tooltip("Pizarra que contiene los Parámetros pasados al comportamiento serializados por Unity")]
        public UnityBlackboard blackboard = new UnityBlackboard();

        [SerializeField]
        [Tooltip("Referencia al asset que incluye la información pura del autómata.")]
        private RabinAutomatonDataAsset automatonFile;

        [Header("Reward Policy")]
        [SerializeField]
        [Tooltip("Política empleada para conceder premios al agente durante el avance por el autómata. StrictProgress mantiene un registro del premio máximo" +
            " del autómata en el episodio actual y sólo se concede un premio cuando el agente supera este máximo. Esto garantiza que el premio del episodio " +
            "esté entre 0 y 1, pero puede ser necesario incluir una penalización existencial simultánea para garantizar que el agente trate de cumplir la " +
            "especificación en el menor tiempo posible.")]
        private RewardPolicy rewardPolicy = RewardPolicy.StrictProgress;

        [SerializeField]
        [Tooltip("Determina si se aplica una penalización en cada acción con el valor especificado en exitentialPenaltyValue")]
        private bool applyExistentialPenalty = false;

        [SerializeField]
        [Tooltip("Valor de la penalización por acción. Si el episodio dura N pasos y se toma una acción por paso, esto se corresponde con 1/N." +
            "Si no se toma una acción por paso, la penalización debe ajustarse.")]
        private float existentialPenaltyValue = 0.0005f;

        [SerializeField]
        [Tooltip("Evento al que suscribirse para recibir las señales de premio del autómata de premios cada vez que se lanza un tick.")]
        public UnityEvent<float> RewardGenerated;

        [SerializeField]
        [Tooltip("Evento al que suscribirse para recibir las señales de penalización del autómata de premios cada vez que se lanza un tick.")]
        public UnityEvent<float> PenaltyGenerated;

        [SerializeField]
        [Tooltip("Evento al que suscribirse para recibir la señal de aceptación de una traza por parte del autómata.")]
        public UnityEvent TraceAccepted;

        [SerializeField]
        [Tooltip("Evento al que suscribirse para recibir la señal de rechazo de una traza por parte del autómata.")]
        public UnityEvent TraceRejected;
        #endregion

        /// <summary>
        /// Autómata de premios generado a partir del predicado introducido como parámetro desde el editor.
        /// Es responsabilidad de este agente llamar al tick del autómata de premios con cada paso de la
        /// academia para garantizar que se asigna el premio correspondiente.
        /// </summary>
        public TLTLRewardAutomaton rewardAutomaton;

        //----------------------------------------------------------------------------
        //                  Métodos relacionados con el editor
        //----------------------------------------------------------------------------
        #region Métodos relacionados con el editor

        /// <summary>
        /// Llamado por Unity cuando se cambia alguna propiedad en el inspector.
        /// </summary>
        /// Revisa el comportamiento, actualizando si es necesario
        /// los parámetros almacenados de la pizarra.
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
                // construimos el autómata de premio que será utilizado en los pasos de la academia para 
                // definir una función de premio a partir de las variables de la pizarra y del fichero de estructura
                // del autómata de Rabin.
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
            // hacemos un tick al autómata de premio, que deja el premio
            // de PROGRESO asociado al último estado (un número entre 0 y 1
            // en función del estado de avance de la especificación) en LastTickReward.
            // Este premio NO es necesariamente el premio que se le da al agente,
            // podemos estar interesados en ajustarlo en función de la variante del
            // algoritmo que queramos aplicar.
            /// <see cref="RewardPolicy"/>
            rewardAutomaton.TickStateMachine();
            float reward = rewardAutomaton.LastTickReward;

            // La primera variante considera SÓLO dar como premio al agente mejoras
            // en el progreso. Esto quiere decir que vamos guardándonos el premio
            // máximo y cada vez que el premio actual lo supere registramos esa mejora
            // como premio al agente y actualizamos el máximo. En la práctica es mejor
            // reservarnos estas actualizaciones para cuando las mejoras superen un cierto
            // umbral para evitar problemas de redondeo a 0 en variaciones muy pequeñas
            // en MLAgents (premios excesivamente pequeños se acaban ignorando si no).
            if (rewardPolicy == RewardPolicy.StrictProgress)
            {
                if (reward < rewardAutomaton.MaxReward + existentialPenaltyValue)
                {
                    // no se logró mejorar el premio actual (no hay progreso) ->
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

                // Modo CompletedSpecificationOnly: este es el único momento en el que concedemos premio bajo esta
                // modalidad, y el premio es sencillamente 1 si acabó en éxito y -1 si acaba en fracaso.
                if (rewardPolicy == RewardPolicy.CompletedSpecificationOnly)
                {
                    reward = rewardAutomaton.Status == TLTLRewardAutomaton.AutomatonStatus.WORD_ACCEPTED ? 1 : -1;
                    RewardGenerated?.Invoke(reward);
                }

                //--------------------------------------------------------------
                //     Eventos de finalización lanzados en cualquier caso.
                //--------------------------------------------------------------

                // (1) Aceptación de palabra
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
            // caso en running (sólo aplica si permitimos premios intermedios)
            else
            {
                if (rewardPolicy != RewardPolicy.CompletedSpecificationOnly)
                {
                    RewardGenerated?.Invoke(reward);
                }
                // penalización existencial
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