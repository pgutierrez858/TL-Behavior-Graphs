using System;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class InParam : Attribute
{
    // Private fields.
    private string name;

    // This constructor defines a required parameters: name.

    public InParam(string name)
    {
        this.name = name;
    }

    // Define Name property.
    // This is a read-only attribute.

    public virtual string Name
    {
        get { return name; }
    }
} // InParam