using UnityEngine;

namespace TLTLCore.Framework
{
    [Predicate("Conditions/True", "True")]
    public class TruePredicate : TLTLPredicate
    {

        public override float EvaluateRobustness()
        {
            // Simplemente 1
            return 1;
        }
    } // TruePredicate
} // namespace TLTLCore.Framework