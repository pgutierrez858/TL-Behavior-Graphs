using UnityEngine;

namespace TLTLCore.Framework
{
    [Predicate("Flow/And", "A & B")]
    public class AndPredicate : TLTLPredicate
    {
        [InParam("A")]
        public TLTLPredicate A { get; set; }

        [InParam("B")]
        public TLTLPredicate B { get; set; }

        public AndPredicate(TLTLPredicate a, TLTLPredicate b)
        {
            A = a;
            B = b;
        }

        public AndPredicate() { }

        public override float EvaluateRobustness()
        {
            float rA = A.EvaluateRobustness();
            float rB = B.EvaluateRobustness();
            /*

            if (rA < 0.0f && rB >= 0f) return rA / 2;

            if (rA >= 0.0f && rB < 0f) return rB / 2;

            // ambas partes positivas o negativas
            return (rA + rB) / 2;
            */

            return Mathf.Min(rA, rB);
        }
    } // AndPredicate
} // namespace TLTLCore.Framework