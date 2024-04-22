namespace TLTLCore.Framework
{
    /// <summary>
    /// Comprueba si el valor decimal especificado en el parámetro A es estrictamente inferior
    /// al dado por B.
    /// </summary>
    [Predicate("Conditions/Boolean Condition", "True?")]
    public class BooleanConditionPredicate : TLTLPredicate
    {
        [InParam("Value")]
        public bool Value { get; set; }


        public override float EvaluateRobustness()
        {
            return Value ? 1 : -1;
        }
    } // BooleanConditionPredicate
} // namespace TLTLCore.Framework
