using UnityEngine;

namespace TLTLCore.Framework
{
    [Predicate("Flow/Then", "A => B")]
    public class ThenPredicate : TLTLPredicate
    {
        [InParam("A")]
        public TLTLPredicate A { get; set; }

        [InParam("B")]
        public TLTLPredicate B { get; set; }

        public override float EvaluateRobustness()
        {
            // equivalente a !A | B
            return Mathf.Max(-A.EvaluateRobustness(), B.EvaluateRobustness());
        }
    } // ThenPredicate
} // namespace TLTLCore.Framework