namespace PlaywrightRunner.Cli;

public sealed record CliOptions(
    CliCommand Command,
    string? FlowPath,
    string? OutputPath,
    IReadOnlyList<string> InputPaths)
{
    public static CliOptions RunFlow(string flowPath) =>
        new(CliCommand.RunFlow, flowPath, null, []);

    public static CliOptions GenerateReport(
        string outputPath,
        IReadOnlyList<string> inputPaths) =>
        new(CliCommand.GenerateReport, null, outputPath, inputPaths);

    public static CliOptions ShowHelp() =>
        new(CliCommand.ShowHelp, null, null, []);

    public static CliOptions ShowVersion() =>
        new(CliCommand.ShowVersion, null, null, []);
}

public sealed record CliParseResult(CliOptions? Options, string? Error)
{
    public bool Success => Options is not null;

    public static CliParseResult Parsed(CliOptions options) =>
        new(options, null);

    public static CliParseResult Failed(string error) =>
        new(null, error);
}
