using PlaywrightRunner.Models;
using PlaywrightRunner.Reporting;
using Xunit;

namespace PlaywrightRunner.Tests;

public sealed class PdfReportWriterTests
{
    [Fact]
    public async Task Write_CreatesPdfWithScreenshot()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            $"playwright-pdf-{Guid.NewGuid():N}");

        Directory.CreateDirectory(root);

        var screenshotPath = Path.Combine(root, "screen.png");
        await File.WriteAllBytesAsync(screenshotPath, TinyPng);

        var definition = new FlowStep
        {
            Name = "Capture page",
            Action = "screenshot",
            Path = screenshotPath,
            FullPage = true
        };

        var result = new StepResult
        {
            Name = definition.Name,
            Action = definition.Action,
            Passed = true,
            DurationMs = 15,
            Data = screenshotPath
        };

        var flow = new FlowDefinition
        {
            SpecVersion = 2,
            Name = "PDF Test",
            ReportPath = "results.json",
            BaseUrl = "https://example.com",
            Browser = "chromium",
            Steps = [definition]
        };

        var step = new ReportStep(
            1,
            definition,
            result,
            ReportStepStatus.Passed,
            StepRequirementFormatter.Format(flow, definition),
            screenshotPath,
            null);

        var report = new TestReport(
            DateTimeOffset.Now,
            [new FlowReport("flow.yaml", "results.json", flow, [step])]);

        var outputPath = Path.Combine(root, "report.pdf");
        var writtenPath = new PdfReportWriter().Write(report, outputPath);

        Assert.Equal(Path.GetFullPath(outputPath), writtenPath);
        Assert.True(new FileInfo(outputPath).Length > 1_000);
        Assert.Equal("%PDF", await ReadPrefixAsync(outputPath, 4));

        var customOutputPath = Path.Combine(root, "custom-report.pdf");
        new PdfReportWriter().Write(report, customOutputPath, "Custom Test Report");

        Assert.False(File.ReadAllBytes(outputPath).SequenceEqual(
            File.ReadAllBytes(customOutputPath)));
    }

    private static async Task<string> ReadPrefixAsync(string path, int length)
    {
        var buffer = new byte[length];
        await using var stream = File.OpenRead(path);
        _ = await stream.ReadAsync(buffer);
        return System.Text.Encoding.ASCII.GetString(buffer);
    }

    private static readonly byte[] TinyPng = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");
}
