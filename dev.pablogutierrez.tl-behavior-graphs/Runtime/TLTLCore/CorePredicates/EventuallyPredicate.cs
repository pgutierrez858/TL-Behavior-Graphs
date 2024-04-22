using UnityEngine;

namespace TLTLCore.Framework
{
    [Predicate("Flow/Eventually", "Eventually X")]
    public class EventuallyPredicate : TLTLPredicate
    {
        [InParam("X")]
        public TLTLPredicate Predicate { get; set; }

        public override float EvaluateRobustness()
        {
            // irrelevante: nunca se va a poder calcular esta robustez por estar abstraída
            // en la estructura del autómata.
            return 0; // placeholder
        }
    } // EventuallyPredicate

} // namespace TLTLCore.Framework