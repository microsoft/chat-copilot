namespace ADME.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class FacetSortAttribute : Attribute
{
    public enum SortType
    {
        SortByValue,
        SortByCount
    }
    public SortType Sort { get; }

    public FacetSortAttribute(SortType sort)
    {
        Sort = sort;
    }
}