using App.Core.Models.Configuration;
using Microsoft.Extensions.Options;

namespace App.Core.Services.Configuration;

public interface IIronPdfSettingsService
{
    IronPdfSettings GetSettings();
}

public class IronPdfSettingsService : IIronPdfSettingsService
{
    private readonly IronPdfSettings _ironPdfSettings;

    public IronPdfSettingsService(IOptions<IronPdfSettings> ironPdfSettings)
    {
        _ironPdfSettings = ironPdfSettings.Value;
    }

    public IronPdfSettings GetSettings() => _ironPdfSettings;
}