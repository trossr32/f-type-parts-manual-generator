using App.Core.Models.Data;
using App.Core.Models.Data.Base;

namespace App.Pdf.Templates.Models;

public class PartGroupPageViewModel : PartGroupPageBase
{
    public PartGroupPageViewModel(PartGroup partGroup, PartGroupPage partGroupPage, int? sectionPartNumber)
    {
        PartGroup = partGroup;

        Id = partGroupPage.Id;
        Link = partGroupPage.Link;
        ImageFile = partGroupPage.ImageFile;
        Name = partGroupPage.Name;
        Parts = partGroupPage.Parts;
        SectionPartNumber = sectionPartNumber;
    }

    public PartGroup PartGroup { get; set; }
    public int? SectionPartNumber { get; set; }
}