using App.Core.Exceptions;
using CommandLine;

namespace App.Core.Models.Options;

[Verb("build-pdf", HelpText = "Build PDF function")]
public class BuildPdfOptions
{
    [Option('d', "data-file", Required = true, HelpText = "Data file location created from a scrape run. Requires an 'img' sub-directory", Default = null)]
    public string DataFile { get; set; }

    public BuildPdfOptions()
    {}

    protected BuildPdfOptions(string dataFile) => 
        DataFile = dataFile;

    public void Validate()
    {
        if (!File.Exists(DataFile))
            throw new ArgumentValidationException($"Data file does not exist: {DataFile}");
    }
}