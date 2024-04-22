using UnityEngine;

namespace TLTLCore.Framework
{
    [Predicate("Flow/Always", "Always X")]
    public class AlwaysPredicate : TLTLPredicate
    {
        [InParam("X")]
        public TLTLPredicate Predicate { get; set; }

        public override float EvaluateRobustness()
        {
            // irrelevante: nunca se va a poder calcular esta robustez por estar abstra�da
            // en la estructura del aut�mata.
            return 0; // placeholder
        }
    } // AlwaysPredicate

} // namespace TLTLCore.Framework