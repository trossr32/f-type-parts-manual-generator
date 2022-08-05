namespace App.Core.Models.Data;

public class Part
{
    public string Name { get; set; }
    public string Link { get; set; }
    public string ImagePartNumber { get; set; }
    public string PartNumber { get; set; }
    public string Description { get; set; }
    public string ImageFile { get; set; }
    public string ListPrice { get; set; }
    public string OurPrice { get; set; }
    public Guid PartGroupId { get; set; }
    public Guid PartGroupPageId { get; set; }
}