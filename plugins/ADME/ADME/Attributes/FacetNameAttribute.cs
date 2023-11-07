namespace ADME.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class FacetNameAttribute : Attribute
{
    public string Name { get; }
    public bool Collection { get; }

    public FacetNameAttribute(string name)
    {
        Name = name;
        Collection = false;
    }

    public FacetNameAttribute(string name, bool collection)
    {
        Name = name;
        Collection = collection;
    }
}