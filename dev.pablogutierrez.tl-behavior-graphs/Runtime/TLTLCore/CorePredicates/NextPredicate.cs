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
            // Irrelevante -> abstra�do en estructura del aut�mata
            return 0;
        }
    } // NextPredicate
} // namespace TLTLCore.Framework