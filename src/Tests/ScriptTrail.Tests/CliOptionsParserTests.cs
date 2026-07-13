using ScriptTrail.Cli;
using ScriptTrail.Reporting;
using Xunit;

namespace ScriptTrail.Tests;

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
        Assert.Equal(PdfReportWriter.DefaultReportName, result.Options.ReportName);
    }

    [Theory]
    [InlineData("--report-name", "Custom Test Report")]
    [InlineData("--report-name=Custom Test Report", null)]
    public void Parse_AcceptsCustomReportName(string option, string? value)
    {
        var arguments = value is null
            ? new[] { "--report", option, "--input", "flow.yaml" }
            : new[] { "--report", option, value, "--input", "flow.yaml" };

        var result = CliOptionsParser.Parse(arguments);

        Assert.True(result.Success);
        Assert.Equal("Custom Test Report", result.Options!.ReportName);
    }

    [Fact]
    public void Parse_RejectsReportNameWithoutValue()
    {
        var result = CliOptionsParser.Parse(
            ["--report", "--report-name", "--input", "flow.yaml"]);

        Assert.False(result.Success);
        Assert.Contains("requires a value", result.Error);
    }

    [Fact]
    public void Parse_RejectsDuplicateReportName()
    {
        var result = CliOptionsParser.Parse(
            [
                "--report",
                "--report-name=First",
                "--report-name=Second",
                "--input=flow.yaml"
            ]);

        Assert.False(result.Success);
        Assert.Contains("only once", result.Error);
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
