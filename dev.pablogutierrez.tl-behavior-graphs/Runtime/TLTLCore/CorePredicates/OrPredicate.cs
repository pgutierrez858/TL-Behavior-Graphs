using UnityEngine;

namespace TLTLCore.Framework
{
    [Predicate("Flow/Or", "A | B")]
    public class OrPredicate : TLTLPredicate
    {
        [InParam("A")]
        public TLTLPredicate A { get; set; }

        [InParam("B")]
        public TLTLPredicate B { get; set; }

        public override float EvaluateRobustness()
        {
            // TODO: Cambiar a la forma suavizada de calcular robustez
            return Mathf.Max(A.EvaluateRobustness(), B.EvaluateRobustness());
        }
    } // OrPredicate
} // namespace TLTLCore.Framework