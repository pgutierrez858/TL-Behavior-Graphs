
using System.Collections.Generic;
using TLTLUnity.Agents.Tests;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.Sentis;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace TLTLUnity.Agents
{
    /// <summary>
    /// Component intended to manage the basic coupling between an ML-Agents agent and a TLTL award automaton.
    /// This component automatically builds a Runtime automaton from the asset selected in the TLTLRewardAutomatonComponent of the GameObject, 
    /// and calls the Tick method of <see cref="TLTLRewardAutomatonComponent"/> on every ML-Agents call to <see cref="OnActionReceived(ActionBuffers)"/>.
    /// Additionally, it makes sure to reset the internal state of the automaton when a new training episode is started in the <see cref="OnEpisodeBegin"/> method,
    /// and triggers a corresponding event to notify of the episode restart.
    /// </summary>
    [RequireComponent(typeof(TLTLRewardAutomatonComponent))]
    public class BaseMLCharacterController : Agent
    {
        protected TLTLRewardAutomatonComponent rewardAutomatonComponent;

        /// <summary>
        /// Event triggered to denote the start of an ML-Agents training episode (when <see cref="OnEpisodeBegin"/> is called).
        /// </summary>
        public UnityEvent onEpisodeBegin;

        [SerializeField]
        [Tooltip("ONNX Model to load on this agent. This is here to address the issue of ML-Agents enforcing " +
            "strict compliance between input/ output size for NNs assigned from the editor; instead, here we allow " +
            "users to select their desired model asset and assign it to the agent on Start, bypassing the ML-Agents check. " +
            "Please ensure that your model shape and the reward automaton inputs match when using this field.")]
        protected ModelAsset asset;

        // s�lo necesario para poder acceder a cu�nta penalizaci�n se aplic� en el �ltimo episodio.
        /// <summary>
        /// Penalizaci�n, en NEGATIVO, acumulada en el episodio actual.
        /// </summary>
        float accPenalty = 0f;

        // TODO: Ahora mismo la gesti�n de estos test writers es 0 gen�rica
        // Idealmente habr�a que incluir m�todos a sobrescribir dentro de una clase
        // abstracta y desde ah� abstraer toda la l�gica para que lo �nico que hiciera
        // falta desde la perspectiva del usuario fuera registrar los test writers
        // con las variables a trackear y las llamadas al record/ flush
        List<RegressionTestWriter> episodeTestWriters;

        public void Awake()
        {
            rewardAutomatonComponent = GetComponent<TLTLRewardAutomatonComponent>();
            rewardAutomatonComponent.BuildRuntimeAutomaton();

            episodeTestWriters = new List<RegressionTestWriter>();
        } // Awake

        private void OnEnable()
        {
            rewardAutomatonComponent.RewardGenerated.AddListener(AddReward);
            rewardAutomatonComponent.PenaltyGenerated.AddListener(AddPenalty);
        } // OnEnable

        private void OnDisable()
        {
            rewardAutomatonComponent.RewardGenerated.RemoveListener(AddReward);
            rewardAutomatonComponent.PenaltyGenerated.RemoveListener(AddPenalty);
        } // OnDisable

        private void Start()
        {
            if (asset)
            {
                SetModel("MLPlayerController", asset);
                var behaviorParams = GetComponent<BehaviorParameters>();
                behaviorParams.BehaviorType = BehaviorType.InferenceOnly;
            }
        } // Start

        /// <summary>
        /// A�ade una penalizaci�n a la penalizaci�n acumulada.
        /// NOTA: La penalizaci�n acumulada se cuenta en negativo, por lo que 
        /// el par�metro de penalizaci�n deber�a ser tambi�n negativo. En caso
        /// contrario, se seguir� a�adiendo, pero se mostrar� un warning por consola.
        /// </summary>
        /// <param name="penalty">Penalizaci�n a a�adir, EN NEGATIVO.</param>
        public void AddPenalty(float penalty)
        {
            if (penalty > 0f)
            {
                Debug.LogWarning("Trying to add a positive penalty. This is typically not desirable.");
            }
            accPenalty += penalty;
        } // AddPenalty

        public void AddEpisodeTestWriter(RegressionTestWriter writer)
        {
            episodeTestWriters.Add(writer);
            writer.RegisterMetric("specification_fulfillment");
            writer.RegisterMetric("reward");
            writer.WriteHeaders();
        } // AddEpisodeTestWriter

        public override void OnEpisodeBegin()
        {
            // registro de las estad�sticas de ejecuci�n asociadas al aut�mata de premios
            var statsRecorder = Academy.Instance.StatsRecorder;
            var maxReward = rewardAutomatonComponent.rewardAutomaton.MaxReward;
            var reward = maxReward + accPenalty;
            statsRecorder.Add("reward_histogram", reward, StatAggregationMethod.Histogram);
            statsRecorder.Add("reward_average", reward, StatAggregationMethod.Average);
            statsRecorder.Add("specification_fulfillment_histogram", maxReward, StatAggregationMethod.Histogram);
            statsRecorder.Add("specification_fulfillment_average", maxReward, StatAggregationMethod.Average);

            foreach (var episodeTestWriter in episodeTestWriters)
            {
                episodeTestWriter.Record("specification_fulfillment", maxReward);
                // premio ajustado por penalizaci�n existencial
                episodeTestWriter.Record("reward", reward);
                episodeTestWriter.Flush();
            }
            base.OnEpisodeBegin();
            accPenalty = 0f;
            rewardAutomatonComponent.rewardAutomaton.Reset();
            onEpisodeBegin?.Invoke();
        } // OnEpisodeBegin

        public override void OnActionReceived(ActionBuffers actionBuffers)
        {
            // hacemos un tick al aut�mata de premio en cada paso de simulaci�n.
            // esto a su vez desencadenar� eventos de premio, penalizaci�n y posiblemente
            // aceptaci�n o rechazo de palabra.
            rewardAutomatonComponent.TickAutomaton();
        } // OnActionReceived

    } // BaseMLCharacterController

} // namespace TLTLUnity.Agents