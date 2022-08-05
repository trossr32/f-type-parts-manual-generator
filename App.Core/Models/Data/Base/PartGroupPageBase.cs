namespace App.Core.Models.Data.Base;

public abstract class PartGroupPageBase
{
    public Guid Id { get; set; }
    public string ImageFile { get; set; }
    public string Name { get; set; }
    public string Link { get; set; }
    public int SubSection { get; set; }

    public List<Part> Parts { get; set; } = new();
}