namespace App.Core.Models.Rendering;

public class TableOfContents
{
    public List<TocPartGroup> PartGroups { get; set; } = new();
}

public class TocPartGroup : TocEntry
{
    public TocPartGroup() : base(0)
    { }

    public List<TocPartGroupPage> PartGroupPages { get; set; } = new();
}

public class TocPartGroupPage : TocEntry
{
    public TocPartGroupPage() : base (1)
    { }

    public List<TocPart> Parts { get; set; } = new();
}

public class TocPart : TocEntry
{
    public TocPart() : base(2)
    { }

    public string DiagramPartNumber { get; set; }
    public string PartNumber { get; set; }
}

public abstract class TocEntry
{
    protected TocEntry(int indent) => 
        Indent = indent;

    public int Indent { get; set; }
    public int PageNumber { get; set; }
    public string Title { get; set; }
}