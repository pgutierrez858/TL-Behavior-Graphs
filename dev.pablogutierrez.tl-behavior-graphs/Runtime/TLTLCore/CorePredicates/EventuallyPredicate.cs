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
            // irrelevante: nunca se va a poder calcular esta robustez por estar abstra�da
            // en la estructura del aut�mata.
            return 0; // placeholder
        }
    } // EventuallyPredicate

} // namespace TLTLCore.Framework