using UnityEngine;

namespace TLTLCore.Framework
{
    [Predicate("Flow/Next", "Next X")]
    public class NextPredicate : TLTLPredicate
    {

        [InParam("X")]
        public TLTLPredicate Predicate { get; set; }

        public override float EvaluateRobustness()
        {
            // Irrelevante -> abstraído en estructura del autómata
            return 0;
        }
    } // NextPredicate
} // namespace TLTLCore.Framework