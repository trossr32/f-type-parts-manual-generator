using App.Core.Models.Configuration;
using Microsoft.Extensions.Options;

namespace App.Core.Services.Configuration;

public interface IFileSettingsService
{
    FileSettings GetSettings();
}

public class FileSettingsService : IFileSettingsService
{
    private readonly FileSettings _fileSettings;

    public FileSettingsService(IOptions<FileSettings> fileSettings)
    {
        _fileSettings = fileSettings.Value;
    }

    public FileSettings GetSettings() => _fileSettings;
}