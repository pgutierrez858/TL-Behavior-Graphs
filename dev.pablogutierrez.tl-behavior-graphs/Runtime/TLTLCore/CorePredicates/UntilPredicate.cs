using UnityEngine;

namespace TLTLCore.Framework
{
    [Predicate("Flow/Until", "P Until Q")]
    public class UntilPredicate : TLTLPredicate
    {
        [InParam("P")]
        public TLTLPredicate P { get; set; }

        [InParam("Q")]
        public TLTLPredicate Q { get; set; }

        public override float EvaluateRobustness()
        {
            // irrelevante: nunca se va a poder calcular esta robustez por estar abstra�da
            // en la estructura del aut�mata.
            return 0; // placeholder
        }
    } // UntilPredicate
} // namespace TLTLCore.Framework