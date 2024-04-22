using System.Collections.Generic;
using System.Linq;
using TLTLCore.Framework;
using TLTLUnity.Data;

namespace TLTLUnity.Agents
{
    public class TLTLRewardAutomatonState
    {
        public List<TLTLRewardAutomatonEdge> Transitions { get; private set; }
        public readonly string promptText;

        public TLTLRewardAutomatonState(string promptText = "")
        {
            this.promptText = promptText;
            this.Transitions = new List<TLTLRewardAutomatonEdge>();
        } // RewardFiniteStateMachineState

        public void AddTransition(TLTLPredicate edgePredicate, int endState, string[] transitionTokens)
        {
            Transitions.Add(new TLTLRewardAutomatonEdge(edgePredicate, endState, transitionTokens));
        } // AddTransition

        /// <summary>
        /// determina, dado un conjunto de condiciones de aceptaci�n, si el estado denota 
        /// una posible aceptaci�n. Esto se traduce en que exista una pareja de aceptaci�n de 
        /// Rabin cuyos tokens finitos no se est�n visitando en alguna transici�n del estado
        /// y sus tokens infinitos s�.
        /// </summary>
        public bool IsAcceptingState(RabinAcceptancePair[] acceptancePairs)
        {
            return Transitions.Any(t => t.IsAcceptingTransition(acceptancePairs));
        } // IsAcceptingState

        public TLTLRewardAutomatonEdge SelectMostRobustTransition()
        {
            float bestRobustness = float.MinValue;
            TLTLRewardAutomatonEdge mostRobustEdge = null;

            foreach (var transition in Transitions)
            {
                float r = transition.predicate.EvaluateRobustness();
                if(r > bestRobustness)
                {
                    mostRobustEdge = transition;
                    bestRobustness = r;
                }
            }

            return mostRobustEdge;
        } // SelectMostRobustTransition

    } // TLTLRewardAutomatonState

} // namespace TLTLUnity.Agents
