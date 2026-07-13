using ScriptTrail.Reporting;

namespace ScriptTrail.Cli;

public static class CliOptionsParser
{
    public const string DefaultReportOutputPath = "TestResults/playwright-report.pdf";

    public static string HelpText =>
        """
        Usage:
          ScriptTrail <flow.json|flow.yaml>
          ScriptTrail --report [--output <report.pdf>] [--report-name <name>] --input <flow.yaml> [--input <flow.yaml> ...]

        Options:
          -h, --help            Show help.
          -v, --version         Print version number.
          --report              Generate a PDF report from existing flow result files.
          --report-name         Cover title. Defaults to Playwright Test Report.
          -o, --output, --path  PDF output path. Defaults to TestResults/playwright-report.pdf.
          -i, --input           Flow YAML or JSON file. Repeat for multiple report sections.

        Examples:
          ScriptTrail saucedemo.yaml
          ScriptTrail --report --input saucedemo.yaml
          ScriptTrail --report --output TestResults/report.pdf --input saucedemo.yaml --input qgao_uat_2.yaml
        """;

    public static CliParseResult Parse(IReadOnlyList<string> args)
    {
        if (args.Count == 0)
            return CliParseResult.Failed("A flow file or command is required.");

        if (args.Count == 1)
        {
            if (Is(args[0], "-h", "--help"))
                return CliParseResult.Parsed(CliOptions.ShowHelp());

            if (Is(args[0], "-v", "--version"))
                return CliParseResult.Parsed(CliOptions.ShowVersion());

            if (!args[0].StartsWith("-", StringComparison.Ordinal))
                return CliParseResult.Parsed(CliOptions.RunFlow(args[0]));
        }

        if (!Is(args[0], "--report"))
        {
            return CliParseResult.Failed(
                "Execution mode accepts exactly one flow file. Use --report for PDF report generation.");
        }

        var outputPath = DefaultReportOutputPath;
        var outputSpecified = false;
        var reportName = PdfReportWriter.DefaultReportName;
        var reportNameSpecified = false;
        var inputPaths = new List<string>();

        for (var index = 1; index < args.Count; index++)
        {
            var argument = args[index];

            if (TryReadInline(argument, "--input", out var inlineInput))
            {
                if (string.IsNullOrWhiteSpace(inlineInput))
                    return CliParseResult.Failed("--input requires a file path.");

                inputPaths.Add(inlineInput);
                continue;
            }

            if (Is(argument, "-i", "--input"))
            {
                var read = ReadNext(args, ref index, argument);
                if (!read.Success)
                    return CliParseResult.Failed(read.Error!);

                inputPaths.Add(read.Value!);
                continue;
            }

            if (TryReadInline(argument, "--output", out var inlineOutput) ||
                TryReadInline(argument, "--path", out inlineOutput))
            {
                if (outputSpecified)
                    return CliParseResult.Failed("Specify the PDF output path only once.");

                if (string.IsNullOrWhiteSpace(inlineOutput))
                    return CliParseResult.Failed("--output requires a PDF file path.");

                outputPath = inlineOutput;
                outputSpecified = true;
                continue;
            }

            if (Is(argument, "-o", "--output", "--path"))
            {
                if (outputSpecified)
                    return CliParseResult.Failed("Specify the PDF output path only once.");

                var read = ReadNext(args, ref index, argument);
                if (!read.Success)
                    return CliParseResult.Failed(read.Error!);

                outputPath = read.Value!;
                outputSpecified = true;
                continue;
            }

            if (TryReadInline(argument, "--report-name", out var inlineReportName))
            {
                if (reportNameSpecified)
                    return CliParseResult.Failed("Specify the report name only once.");

                if (string.IsNullOrWhiteSpace(inlineReportName))
                    return CliParseResult.Failed("--report-name requires a value.");

                reportName = inlineReportName;
                reportNameSpecified = true;
                continue;
            }

            if (Is(argument, "--report-name"))
            {
                if (reportNameSpecified)
                    return CliParseResult.Failed("Specify the report name only once.");

                var read = ReadNext(args, ref index, argument);
                if (!read.Success)
                    return CliParseResult.Failed(read.Error!);

                reportName = read.Value!;
                reportNameSpecified = true;
                continue;
            }

            return CliParseResult.Failed($"Unknown report option: {argument}");
        }

        if (inputPaths.Count == 0)
            return CliParseResult.Failed("Report mode requires at least one --input flow file.");

        if (!string.Equals(Path.GetExtension(outputPath), ".pdf", StringComparison.OrdinalIgnoreCase))
            return CliParseResult.Failed("The report output path must use the .pdf extension.");

        return CliParseResult.Parsed(
            CliOptions.GenerateReport(outputPath, reportName, inputPaths));
    }

    private static (bool Success, string? Value, string? Error) ReadNext(
        IReadOnlyList<string> args,
        ref int index,
        string option)
    {
        if (index + 1 >= args.Count ||
            string.IsNullOrWhiteSpace(args[index + 1]) ||
            args[index + 1].StartsWith("-", StringComparison.Ordinal))
        {
            return (false, null, $"{option} requires a value.");
        }

        index++;
        return (true, args[index], null);
    }

    private static bool TryReadInline(
        string argument,
        string option,
        out string value)
    {
        var prefix = option + "=";

        if (!argument.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            value = String.Empty;
            return false;
        }

        value = argument[prefix.Length..];
        return true;
    }

    private static bool Is(string value, params string[] options) =>
        options.Any(option =>
            string.Equals(value, option, StringComparison.OrdinalIgnoreCase));
}
