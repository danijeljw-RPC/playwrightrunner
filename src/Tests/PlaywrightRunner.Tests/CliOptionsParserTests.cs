using PlaywrightRunner.Cli;
using Xunit;

namespace PlaywrightRunner.Tests;

public sealed class CliOptionsParserTests
{
    [Fact]
    public void Parse_PreservesPositionalExecutionMode()
    {
        var result = CliOptionsParser.Parse(["flow.yaml"]);

        Assert.True(result.Success);
        Assert.Equal(CliCommand.RunFlow, result.Options!.Command);
        Assert.Equal("flow.yaml", result.Options.FlowPath);
    }

    [Fact]
    public void Parse_AcceptsRepeatedInputArgumentsInOrder()
    {
        var result = CliOptionsParser.Parse(
            [
                "--report",
                "--output=TestResults/report.pdf",
                "--input=first.yaml",
                "--input",
                "second.yaml"
            ]);

        Assert.True(result.Success);
        Assert.Equal(CliCommand.GenerateReport, result.Options!.Command);
        Assert.Equal("TestResults/report.pdf", result.Options.OutputPath);
        Assert.Equal(new[] { "first.yaml", "second.yaml" }, result.Options.InputPaths);
    }

    [Fact]
    public void Parse_AcceptsPathAsOutputAlias()
    {
        var result = CliOptionsParser.Parse(
            ["--report", "--path=report.pdf", "--input=flow.yaml"]);

        Assert.True(result.Success);
        Assert.Equal("report.pdf", result.Options!.OutputPath);
    }

    [Fact]
    public void Parse_UsesDefaultReportOutput()
    {
        var result = CliOptionsParser.Parse(
            ["--report", "--input", "flow.yaml"]);

        Assert.True(result.Success);
        Assert.Equal(
            CliOptionsParser.DefaultReportOutputPath,
            result.Options!.OutputPath);
    }

    [Fact]
    public void Parse_RejectsReportWithoutInputs()
    {
        var result = CliOptionsParser.Parse(["--report"]);

        Assert.False(result.Success);
        Assert.Contains("at least one --input", result.Error);
    }

    [Fact]
    public void Parse_RejectsNonPdfOutput()
    {
        var result = CliOptionsParser.Parse(
            ["--report", "--output", "report.json", "--input", "flow.yaml"]);

        Assert.False(result.Success);
        Assert.Contains(".pdf", result.Error);
    }
}
