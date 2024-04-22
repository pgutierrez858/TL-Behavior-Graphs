namespace TLTLCore.Framework
{
    /// <summary>
    /// Comprueba si el valor decimal especificado en el parámetro A es estrictamente inferior
    /// al dado por B.
    /// </summary>
    [Predicate("Conditions/Less Than", "A < B")]
    public class LessThanPredicate : TLTLPredicate
    {
        [InParam("A")]
        public float A { get; set; }

        [InParam("B")]
        public float B { get; set; }

        public override float EvaluateRobustness()
        {
            return B - A;
        }
    } // LessThanPredicate
} // namespace TLTLCore.Framework
