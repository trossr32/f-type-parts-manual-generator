using App.Core.Models.Data.Base;

namespace App.Core.Models.Data;

public class PartGroupPage : PartGroupPageBase
{
    public PartGroupPage(Guid id, string imageFile, string name, int subSection)
    {
        Id = id;
        ImageFile = imageFile;
        Name = name;
        SubSection = subSection;
    }
}