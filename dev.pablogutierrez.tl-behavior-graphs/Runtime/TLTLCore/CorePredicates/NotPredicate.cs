namespace TLTLCore.Framework
{
    [Predicate("Transformations/Not", "~X")]
    public class NotPredicate : TLTLPredicate
    {
        [InParam("X")]
        public TLTLPredicate Predicate { get; set; }

        public override float EvaluateRobustness()
        {
            return -Predicate.EvaluateRobustness();
        }
    } // NotPredicate
} // namespace TLTLCore.Framework
