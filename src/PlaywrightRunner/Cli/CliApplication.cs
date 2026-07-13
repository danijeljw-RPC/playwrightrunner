using PlaywrightRunner.Models;
using PlaywrightRunner.Reporting;
using PlaywrightRunner.Runtime;
using PlaywrightRunner.Services;

namespace PlaywrightRunner.Cli;

public sealed class CliApplication
{
    private readonly string _version;

    public CliApplication(string version)
    {
        _version = version;
    }

    public async Task<int> RunAsync(string[] args)
    {
        var parsed = CliOptionsParser.Parse(args);

        if (!parsed.Success)
        {
            Console.Error.WriteLine(parsed.Error);
            Console.Error.WriteLine();
            Console.Error.WriteLine(CliOptionsParser.HelpText);
            return 2;
        }

        return parsed.Options!.Command switch
        {
            CliCommand.ShowHelp => ShowHelp(),
            CliCommand.ShowVersion => ShowVersion(),
            CliCommand.RunFlow => await RunFlowAsync(parsed.Options.FlowPath!),
            CliCommand.GenerateReport => GenerateReport(parsed.Options),
            _ => 2
        };
    }

    private static async Task<int> RunFlowAsync(string inputPath)
    {
        if (!File.Exists(inputPath))
        {
            Console.Error.WriteLine($"Input file not found: {inputPath}");
            return 2;
        }

        FlowDefinition flow;

        try
        {
            flow = FlowFileLoader.Load(inputPath);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Could not load flow file: {ex.Message}");
            return 2;
        }

        var appDir = AppContext.BaseDirectory;
        var browserDir = Path.Combine(appDir, "ms-playwright");

        Environment.SetEnvironmentVariable(
            "PLAYWRIGHT_BROWSERS_PATH",
            browserDir);

        var runner = new FlowRunner(
            new BrowserFactory(),
            new FlowStepExecutor(new SelectorResolver()),
            new ReportWriter());

        return await runner.RunAsync(flow);
    }

    private static int GenerateReport(CliOptions options)
    {
        try
        {
            var report = new FlowReportLoader().Load(options.InputPaths);
            var outputPath = new PdfReportWriter().Write(report, options.OutputPath!);

            Console.WriteLine($"PDF report written: {outputPath}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Could not generate PDF report: {ex.Message}");
            return 2;
        }
    }

    private int ShowVersion()
    {
        Console.WriteLine(_version);
        return 0;
    }

    private static int ShowHelp()
    {
        Console.WriteLine(CliOptionsParser.HelpText);
        return 0;
    }
}
