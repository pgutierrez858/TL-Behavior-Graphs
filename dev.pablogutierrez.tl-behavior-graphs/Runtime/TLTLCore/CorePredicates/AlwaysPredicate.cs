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
            // irrelevante: nunca se va a poder calcular esta robustez por estar abstraída
            // en la estructura del autómata.
            return 0; // placeholder
        }
    } // AlwaysPredicate

} // namespace TLTLCore.Framework