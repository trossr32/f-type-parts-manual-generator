using System.Text.RegularExpressions;
using App.Core.Extensions;
using App.Core.Models.Data;
using App.Core.Models.Options;
using App.Core.Services.Configuration;
using Flurl.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Newtonsoft.Json;

namespace App.Services;

public interface IScrapeService
{
    Task Process(ScrapeOptions runOptions);
}

public class ScrapeService : IScrapeService
{
    private static ILogger<ScrapeService> _logger;

    private static string _outputPath;
    private static string _imagesPath;
    private Results _results;

    private const string PartGroupElementsSelector = "a.respon-btn-design.ds_link_clr";
    private const string PartDetailElementsSelector = "div#item_list_flyout.table-responsive table.table tbody tr td table.table.table_flyout";
    private const string PartNumberElementsSelector = "div#item_list_flyout.table-responsive table.table tbody tr td.right-hotspot-td div.hotspot.right-hotspot span pp";

    private readonly PageWaitForSelectorOptions _pageWaitForSelectorOptions = new()
    {
        State = WaitForSelectorState.Attached,
        Strict = false,
        Timeout = 4000
    };

    private static readonly Regex BackGroundUrlRegex = new(@"url\((?<url>.+)\)");
    
    public ScrapeService(ILogger<ScrapeService> logger, IFileSettingsService fileSettingsSvc)
    {
        _logger = logger;

        var fileSettings = fileSettingsSvc.GetSettings();

        _outputPath = Path.Join(fileSettings.RunsDirectory, $"f-type-parts-scraper.{DateTime.Now:yyyyMMdd_HHmmss}");
        _imagesPath = Path.Join(_outputPath, "img");
    }

    public async Task Process(ScrapeOptions options)
    {
        try
        {
            await Scrape();

            await File.WriteAllTextAsync(Path.Join(_outputPath, "results.json"), JsonConvert.SerializeObject(_results, Formatting.Indented));
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Scraping failed");
        }
    }

    /// <summary>
    /// Scrape parts website
    /// </summary>
    /// <returns></returns>
    private async Task Scrape()
    {
        string searchUrl = "https://jaguarpartsestore.com/2015-jaguar-f_type-r-gas_5.0_8-transmission_automatic_8-body_hardware-parts/";
        
        // set up playwright
        using var playwright = await Playwright.CreateAsync();

        await using var browser = await playwright.Firefox.LaunchAsync();
            
        var page = await browser.NewPageAsync();
            
        await page.GotoAsync(searchUrl);

        await page.WaitForSelectorAsync(PartGroupElementsSelector, _pageWaitForSelectorOptions);

        // get result count for validation
        var resultEls = await page.QuerySelectorAllAsync(PartGroupElementsSelector);

        var partGroups = resultEls
            .Select(async el => new PartGroup
            {
                Link = await el.GetAttributeAsync("href"),
                Name = await el.InnerTextAsync()
            })
            .Select(t => t.Result)
            .ToList();
            
        Directory.CreateDirectory(_imagesPath);

        _logger?.LogInformation("Processing {Count} part groups...", partGroups.Count);

        _results = new Results();

        foreach (var (partGroup, pgi) in partGroups.WithIndex())
        {
            //if (pgi < 3)
            //    continue; 

            _logger?.LogInformation("");
            _logger?.LogInformation("Processing part group: {Name} ({Number})", partGroup.Name, pgi + 1);
            _logger?.LogInformation("");
            _logger?.LogInformation("Navigating to: {Link}", partGroup.Link);

            await page.GotoAsync(partGroup.Link);

            await page.WaitForSelectorAsync(PartDetailElementsSelector, _pageWaitForSelectorOptions);

            var filePath = await DownloadMainImage(page);

            var partDetailEls = await page.QuerySelectorAllAsync(PartDetailElementsSelector);
            var partNumberEls = await page.QuerySelectorAllAsync(PartNumberElementsSelector);

            if (partDetailEls.Count != partNumberEls.Count)
            {
                _logger?.LogError("part detail element count ({PartDetailCount}) does not match part number element count ({PartNumberCount}) at URL: {Url} for part group: {PartGroup}", partDetailEls.Count, partNumberEls.Count, partGroup.Link, JsonConvert.SerializeObject(partGroup));

                continue;
            }

            // check for sub-pages
            var subPageEls = await page.QuerySelectorAllAsync("div.thumb-image div.img-thumb-div a");

            var links = subPageEls
                .WithIndex()
                .Where(x => x.index > 0)
                .Select(x => x.item)
                .Select(l => l.GetAttributeAsync("href"))
                .Select(t => t.Result)
                .ToList();

            var partGroupPage = new PartGroupPage(partGroup.Id, filePath, partGroup.Name, 1)
            {
                Link = partGroup.Link
            };

            var parts = await GetPartsFromPage(page, partDetailEls, partNumberEls, partGroup.Id, partGroupPage.Id);

            partGroupPage.Parts.AddRange(parts);

            var partGroupPages = new List<PartGroupPage> {partGroupPage};
            
            foreach (var (link,  i) in links.WithIndex())
            {
                _logger?.LogInformation("");
                _logger?.LogInformation("Processing part group part {Part}: {Name} ({Number})", i + 2, partGroup.Name, pgi + 1);
                _logger?.LogInformation("");
                _logger?.LogInformation("Navigating to: {Link}", link);

                await page.GotoAsync(link);

                await page.WaitForSelectorAsync(PartDetailElementsSelector, _pageWaitForSelectorOptions);

                filePath = await DownloadMainImage(page);

                partDetailEls = await page.QuerySelectorAllAsync(PartDetailElementsSelector);
                partNumberEls = await page.QuerySelectorAllAsync(PartNumberElementsSelector);

                if (partDetailEls.Count != partNumberEls.Count)
                {
                    _logger?.LogError("part detail element count ({PartDetailCount}) does not match part number element count ({PartNumberCount}) at URL: {Url} for part group: {PartGroup}", partDetailEls.Count, partNumberEls.Count, link, JsonConvert.SerializeObject(partGroup));

                    continue;
                }

                var partGroupPage2 = new PartGroupPage(partGroup.Id, filePath, partGroup.Name, i + 2)
                {
                    Link = link
                };

                var parts2 = await GetPartsFromPage(page, partDetailEls, partNumberEls, partGroup.Id, partGroupPage2.Id);

                partGroupPage2.Parts.AddRange(parts2);

                partGroupPages.Add(partGroupPage2);
            }

            _results.PartGroups.Add(new PartGroup(partGroup, partGroupPages));
        }
    }

    private async Task<List<Part>> GetPartsFromPage(IPage page, IReadOnlyList<IElementHandle> partDetailEls, IReadOnlyList<IElementHandle> partNumberEls, Guid partGroupId, Guid partGroupPageId)
    {
        var parts = new List<Part>();

        foreach (var (partDetailEl, i) in partDetailEls.WithIndex())
        {
            var partNumberEl = partNumberEls[i];

            var imagePartNumber = (await partNumberEl.GetAttributeAsync("class")).Replace("left_call_out_hide_show_", "") + await partNumberEl.InnerTextAsync();

            var linkEl = await partDetailEl.QuerySelectorAsync("a.title");
            var name = await linkEl.InnerTextAsync();
            var link = await linkEl.GetAttributeAsync("href");

            var jagPartNumber = link
                .Split('_')
                .Last()
                .Split('-')
                .First();

            var descriptionEl = await partDetailEl.QuerySelectorAsync("tbody tr td div.flayout_partno p span");
            var description = await descriptionEl.InnerTextAsync();
            
            parts.Add(new Part
            {
                PartGroupId = partGroupId,
                PartGroupPageId = partGroupPageId,
                Name = name,
                Link = link,
                ImagePartNumber = imagePartNumber,
                PartNumber = jagPartNumber,
                Description = description
            });
        }

        foreach (var part in parts)
        {
            _logger?.LogInformation("Navigating to: {Link}", part.Link);

            await page.GotoAsync(part.Link);

            await page.WaitForSelectorAsync("div.prod-benefit span.part-num", _pageWaitForSelectorOptions);

            part.ImageFile = await DownloadMainImage(page);
            
            var listPriceEl = await page.QuerySelectorAsync("div.prod-benefit span div span.line-cut");

            if (listPriceEl is not null)
                part.ListPrice = await listPriceEl.InnerTextAsync();

            var ourPriceEl = await page.QuerySelectorAsync("div.prod-benefit span h5");

            if (ourPriceEl is not null)
                part.OurPrice = (await ourPriceEl.InnerTextAsync()).Replace("Price : ", "").Replace("Our", "").Trim();
        }

        return parts;
    }

    private async Task<string> DownloadMainImage(IPage page)
    {
        var mainImage = await page.QuerySelectorAsync("div.image-svg img");

        if (mainImage is not null)
        {
            var image = await mainImage.GetAttributeAsync("src");

            _logger?.LogInformation("Downloading image: {Image}", image);

            return await $"http:{image}"
                .DownloadFileAsync(_imagesPath);
        }

        mainImage = await page.QuerySelectorAsync("div#currentImg");

        if (mainImage is not null)
        {
            var image = await mainImage.GetAttributeAsync("style");

            var url = BackGroundUrlRegex.Match(image).Groups["url"].Value;

            _logger?.LogInformation("Downloading image: {Image}", url);

            return await $"http:{url}"
                .DownloadFileAsync(_imagesPath);
        }

        return null;
    }
}
