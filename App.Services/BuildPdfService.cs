using App.Core.Extensions;
using App.Core.Models.Configuration;
using App.Core.Models.Data;
using App.Core.Models.Options;
using App.Core.Models.Rendering;
using App.Core.Services.Configuration;
using App.Pdf.Templates.Models;
using IronPdf;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Razor.Templating.Core;

namespace App.Services;

public interface IBuildPdfService
{
    Task Process(BuildPdfOptions options);
}

public class BuildPdfService : IBuildPdfService
{
    private readonly ILogger<BuildPdfService> _logger;
    private readonly IronPdfSettings _ironPdfSettings;
    private readonly FileSettings _fileSettings;

    public BuildPdfService(ILogger<BuildPdfService> logger, IIronPdfSettingsService ironPdfSettingsSvc, IFileSettingsService fileSettingsSvc)
    {
        _logger = logger;

        _ironPdfSettings = ironPdfSettingsSvc.GetSettings();
        _fileSettings = fileSettingsSvc.GetSettings();
    }

    public async Task Process(BuildPdfOptions options)
    {
        var json = await File.ReadAllTextAsync(options.DataFile);

        var data = JsonConvert.DeserializeObject<Results>(json);

        await GeneratePdf(data);
    }

    private async Task GeneratePdf(Results results)
    {
        try
        {
            License.LicenseKey = _ironPdfSettings.LicenseKey;

            HtmlToPdf renderer = Renderer(0);

            //var html = await RazorTemplateEngine.RenderAsync("/Views/Cover.cshtml");

            //var coverImg = Path.Combine(Directory.GetCurrentDirectory(), "images", "f-type.cover.jpg")
            //    .ToDataUri();

            var pdf = renderer.RenderHtmlAsPdf($"<div style=\"background: url('images/f-type.cover.jpg') no-repeat center center fixed; -webkit-background-size: cover; -moz-background-size: cover; -o-background-size: cover; background-size: cover; width: 100%; height: 100%;font-family: 'Helvetica Neue', Helvetica, Arial, sans-serif; font-size: 40px; text-align: center;\"><div style=\"height: 300px;\">&nbsp;</div><div style=\"width: calc(100% - 30px); background-color: darkcyan; padding: 15px; color: #fff;\">Jag F-Type Parts Manual</div></div>");

            // build table of contents wrapper
            var toc = BuildToc(results);

            // generate toc pdf
            var tocHtml = await RazorTemplateEngine.RenderAsync("/Views/TableOfContents.cshtml", toc);

            var tocPdf = Renderer().RenderHtmlAsPdf(tocHtml);

            var tocPageCount = tocPdf.PageCount;

            pdf.AppendPdf(tocPdf);

            // build part group and sub-sections to pdfs
            foreach (var partGroup in results.PartGroups)
            {
                pdf.AppendPdf(await ProcessPartGroup(partGroup));
            }

            // add footer
            pdf.AddHTMLFooters(await Footer(), true);

            // build toc bookmarks
            // page index is page number - 1 + toc page count
            toc.PartGroups.Reverse();

            foreach (var (tocPartGroup, tpgi) in toc.PartGroups.WithIndex())
            {
                tocPartGroup.PartGroupPages.Reverse();

                foreach (var (tocPartGroupPage, tpgpi) in tocPartGroup.PartGroupPages.WithIndex())
                {
                    tocPartGroupPage.Parts.Reverse();

                    foreach (var (tocPart, tpi) in tocPartGroupPage.Parts.WithIndex())
                    {
                        pdf.BookMarks.AddBookMarkAtStart(tocPart.Title, tocPart.PageNumber - 1 + tocPageCount, tocPart.Indent);
                    }

                    pdf.BookMarks.AddBookMarkAtStart(tocPartGroupPage.Title, tocPartGroupPage.PageNumber - 1 + tocPageCount, tocPartGroupPage.Indent);
                }

                pdf.BookMarks.AddBookMarkAtStart(tocPartGroup.Title, tocPartGroup.PageNumber - 1 + tocPageCount, tocPartGroup.Indent);
            }
            
            // create output directory and build final PDF
            var runDateTime = DateTime.Now;

            var outputPath = Path.Join(_fileSettings.RunsDirectory, $"f-type-parts-pdf-builder.{runDateTime:yyyyMMdd_HHmmss}");

            Directory.CreateDirectory(outputPath);

            pdf.SaveAs(Path.Join(outputPath, $"f-type-parts-reference.{runDateTime:yyyyMMdd_HHmmss}.pdf"));
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Failed to generate PDF");
        }
    }

    private async Task<PdfDocument> ProcessPartGroup(PartGroup partGroup)
    {
        _logger?.LogInformation("Processing part group: {PartGroup}", partGroup.Name);

        var renderer = Renderer();

        var html = await RazorTemplateEngine.RenderAsync("/Views/PartGroup.cshtml", partGroup);

        var pdf = renderer.RenderHtmlAsPdf(html);

        foreach (var (page, i) in partGroup.Pages.WithIndex())
        {
            pdf.AppendPdf(await ProcessPartGroupPage(partGroup, page, i));
        }

        return pdf;
    }

    private async Task<PdfDocument> ProcessPartGroupPage(PartGroup partGroup, PartGroupPage partGroupPage, int? sectionPartNumber)
    {
        _logger?.LogInformation("Processing part group page: {PartGroup}", partGroup.Pages.Count > 1 ? $"{partGroup.Name} - Part {sectionPartNumber ?? 0 + 1}" : partGroup.Name);

        var renderer = Renderer();
        
        var model = new PartGroupPageViewModel(partGroup, partGroupPage, sectionPartNumber);

        var html = await RazorTemplateEngine.RenderAsync("/Views/PartGroupPage.cshtml", model);

        var pdf = renderer.RenderHtmlAsPdf(html);

        foreach (var part in partGroupPage.Parts)
        {
            pdf.AppendPdf(await ProcessPart(part));
        }

        return pdf;
    }

    private async Task<PdfDocument> ProcessPart(Part part)
    {
        _logger?.LogInformation("Processing part: {Part}", part.Name);

        var renderer = Renderer();

        var html = await RazorTemplateEngine.RenderAsync("/Views/Part.cshtml", part);

        return renderer.RenderHtmlAsPdf(html);
    }

    private TableOfContents BuildToc(Results results)
    {
        var toc = new TableOfContents();

        int pageNumber = 2;

        foreach (var partGroup in results.PartGroups)
        {
            var tocPartGroup = new TocPartGroup
            {
                PageNumber = pageNumber,
                Title = partGroup.Name
            };

            pageNumber++;

            foreach (var (partGroupPage, pgpi) in partGroup.Pages.WithIndex())
            {
                var tocPartGroupPage = new TocPartGroupPage
                {
                    Title = partGroup.Pages.Count < 2
                        ? partGroupPage.Name
                        : $"{partGroupPage.Name} - part {pgpi + 1}",
                    PageNumber = pageNumber
                };

                pageNumber += partGroupPage.Parts.Batch(8).Count;

                foreach (var part in partGroupPage.Parts)
                {
                    tocPartGroupPage.Parts.Add(new TocPart
                    {
                        Title = part.Name,
                        PageNumber = pageNumber,
                        PartNumber = part.PartNumber,
                        DiagramPartNumber = part.ImagePartNumber
                    });

                    pageNumber++;
                }

                tocPartGroup.PartGroupPages.Add(tocPartGroupPage);
            }

            toc.PartGroups.Add(tocPartGroup);
        }

        return toc;
    }

    private static HtmlToPdf Renderer(int margin = 5) =>
        new()
        {
            PrintOptions =
            {
                MarginTop = margin,
                MarginRight = margin,
                MarginBottom = margin == 5 ? 10 : margin,
                MarginLeft = margin
            }
        };

    private static async Task<HtmlHeaderFooter> Footer() =>
        new()
        {
            DrawDividerLine = true,
            HtmlFragment = await RazorTemplateEngine.RenderAsync("/Views/Shared/_Footer.cshtml"),
            Height = 10
        };
}