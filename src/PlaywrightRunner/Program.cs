using PlaywrightRunner.Runtime;
using PlaywrightRunner.Services;

const string Version = "0.2.0";

var inputPath = args.FirstOrDefault();

if (inputPath is "-v" or "--version")
{
    Console.WriteLine(Version);
    return 0;
}

if (inputPath is "-h" or "--help")
{
    Console.WriteLine(
        """
        Usage: PlaywrightRunner <flow.json|flow.yaml>

        Options:
          -h, --help       Show help.
          -v, --version    Print version number.
        """);
    return 0;
}

if (string.IsNullOrWhiteSpace(inputPath))
{
    Console.WriteLine("Usage: PlaywrightRunner <flow.json|flow.yaml>");
    return 2;
}

if (!File.Exists(inputPath))
{
    Console.WriteLine($"Input file not found: {inputPath}");
    return 2;
}

var appDir = AppContext.BaseDirectory;
var browserDir = Path.Combine(appDir, "ms-playwright");

Environment.SetEnvironmentVariable("PLAYWRIGHT_BROWSERS_PATH", browserDir);

var flow = FlowFileLoader.Load(inputPath);

var runner = new FlowRunner(
    new BrowserFactory(),
    new FlowStepExecutor(new SelectorResolver()),
    new ReportWriter());

var exitCode = await runner.RunAsync(flow);

return exitCode;
