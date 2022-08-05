using CommandLine;

namespace App.Core.Models.Options;

[Verb("scrape", HelpText = "Scrape function")]
public class ScrapeOptions
{
    //[Option('o', "output", Required = false, HelpText = "Folder into which the generated website will be saved", Default = null)]
    //public string OutputDirectory { get; set; }
}