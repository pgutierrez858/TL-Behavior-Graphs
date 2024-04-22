using System;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class Predicate : Attribute
{
    // Private fields.
    private string name;

    // Private fields.
    private string outputExpression;

    // This constructor defines two required parameters: name & output expression text.

    public Predicate(string name, string outputExpression)
    {
        this.name = name;
        this.outputExpression = outputExpression;
    }

    // Define Name property.
    // This is a read-only attribute.

    public virtual string Name
    {
        get { return name; }
    }

    // Define OutputExpression property.
    // This is a read-only attribute.

    public virtual string OutputExpression
    {
        get { return outputExpression; }
    }
} // Predicate