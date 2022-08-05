using App.Core.Exceptions;
using App.Core.Models.Configuration;
using App.Core.Models.Options;
using App.Core.Services.Configuration;
using App.Services;
using CommandLine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace App.Console;

class Program
{
    private static readonly IServiceProvider ServiceProvider;
    private static readonly IConfigurationRoot Configuration;
    private static ILogger<Program> _logger;

    const string configuration = "Configuration";

    static Program()
    {
        var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

        var builder = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), configuration))
            .AddEnvironmentVariables();

        // add all json config files to configuration
        foreach (var configFile in Directory.GetFiles(Path.Join(Directory.GetCurrentDirectory(), configuration), "*.json"))
        {
            builder.AddJsonFile(Path.GetFileName(configFile), optional: true, reloadOnChange: true);
        }

        Configuration = builder.Build();

        var fileSettings = Configuration.GetSection(nameof(FileSettings)).Get<FileSettings>();

        Log.Logger = new LoggerConfiguration()
            .WriteTo.File(Path.Combine(fileSettings.LogDirectory, $"{DateTime.Now:yyyyMMdd_HHmmss}.log"))   // log to file system
            .WriteTo.Console()                                                                              // log to console
            .CreateLogger();

        var serviceCollection = new ServiceCollection();
        ConfigureServices(serviceCollection);

        ServiceProvider = serviceCollection.BuildServiceProvider();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddOptions();

        // logging
        services.AddLogging(configure => configure.AddSerilog());

        // configuration
        services.Configure<FileSettings>(options => Configuration.GetSection(nameof(FileSettings)).Bind(options));
        services.AddSingleton<IFileSettingsService, FileSettingsService>();
        
        services.Configure<IronPdfSettings>(options => Configuration.GetSection(nameof(IronPdfSettings)).Bind(options));
        services.AddSingleton<IIronPdfSettingsService, IronPdfSettingsService>();

        // services
        services.AddSingleton<IScrapeService, ScrapeService>();
        services.AddSingleton<IBuildPdfService, BuildPdfService>();
    }

    /// <summary>
    /// App entry point. Use CommandLineParser to configure any command line arguments.
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    static async Task Main(string[] args)
    {
        _logger = ServiceProvider.GetService<ILogger<Program>>();

        try
        {
            await Parser.Default.ParseArguments<ScrapeOptions, BuildPdfOptions>(args)
                .MapResult(
                    async (ScrapeOptions options) => await Scrape(options),
                    async (BuildPdfOptions options) =>
                    {
                        options.Validate();

                        await BuildPdf(options);
                    },
                    err => Task.FromResult(-1)
                );
        }
        catch (ArgumentValidationException e)
        {
            _logger?.LogError(e, "Invalid arguments detected");
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Unknown error");

            throw;
        }
    }

    /// <summary>
    /// Run on 'scrape' verb, e.g. App.Console.exe run
    /// </summary>
    /// <returns></returns>
    private static async Task Scrape(ScrapeOptions options)
    {
        var scrapeSvc = ServiceProvider.GetService<IScrapeService>();

        try
        {
            await scrapeSvc.Process(options);
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Failed to run process");

            throw;
        }
    }

    /// <summary>
    /// Run on 'build-pdf' verb, e.g. App.Console.exe run
    /// </summary>
    /// <returns></returns>
    private static async Task BuildPdf(BuildPdfOptions options)
    {
        var buildPdfSvc = ServiceProvider.GetService<IBuildPdfService>();

        try
        {
            await buildPdfSvc.Process(options);
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Failed to run process");

            throw;
        }
    }
}