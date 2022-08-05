namespace App.Core.Models.Data;

public class PartGroup
{
    public PartGroup()
    { }

    public PartGroup(PartGroup partGroup, List<PartGroupPage> pages)
    {
        Id = partGroup.Id;
        Name = partGroup.Name;
        Link = partGroup.Link;

        Pages = pages;
    }

    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; }
    public string Link { get; set; }

    public List<PartGroupPage> Pages { get; set; } = new();
}