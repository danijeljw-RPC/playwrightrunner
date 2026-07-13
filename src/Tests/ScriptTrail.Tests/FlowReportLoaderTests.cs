using ScriptTrail.Reporting;
using Xunit;

namespace ScriptTrail.Tests;

public sealed class FlowReportLoaderTests
{
    [Fact]
    public void Load_CombinesVersionOneAndVersionTwoFlowsInInputOrder()
    {
        var root = CreateTempDirectory();
        var first = CreateVersionOneFlow(root);
        var second = CreateVersionTwoFlow(root);

        var report = new FlowReportLoader().Load([first, second]);

        Assert.Equal(2, report.Flows.Count);
        Assert.Equal("Legacy Flow", report.Flows[0].Flow.Name);
        Assert.Equal("Current Flow", report.Flows[1].Flow.Name);
        Assert.Equal(2, report.PassedCount);
        Assert.Equal(1, report.NotRunCount);
    }

    [Fact]
    public void Load_UsesResultDataForScreenshotPath()
    {
        var root = CreateTempDirectory();
        var resultsDirectory = Path.Combine(root, "TestResults");
        Directory.CreateDirectory(resultsDirectory);

        var screenshotPath = Path.Combine(resultsDirectory, "screen.png");
        File.WriteAllBytes(screenshotPath, TinyPng);

        var flowPath = Path.Combine(root, "screenshot.yaml");
        File.WriteAllText(
            flowPath,
            """
            specVersion: 1
            name: Screenshot Flow
            steps:
              - name: Capture
                action: screenshot
                path: TestResults/fallback.png
            """);

        File.WriteAllText(
            Path.Combine(resultsDirectory, "report.json"),
            """
            [
              {
                "Name": "Capture",
                "Action": "screenshot",
                "Passed": true,
                "DurationMs": 12,
                "Data": "TestResults/screen.png"
              }
            ]
            """);

        var report = new FlowReportLoader().Load([flowPath]);
        var step = Assert.Single(report.Flows[0].Steps);

        Assert.Equal(Path.GetFullPath(screenshotPath), step.ScreenshotPath);
        Assert.Null(step.Warning);
    }

    [Fact]
    public void Load_MarksUnexecutedStepsAsNotRun()
    {
        var root = CreateTempDirectory();
        var flowPath = CreateVersionTwoFlow(root);

        var report = new FlowReportLoader().Load([flowPath]);
        var flow = Assert.Single(report.Flows);

        Assert.Equal(ReportStepStatus.Passed, flow.Steps[0].Status);
        Assert.Equal(ReportStepStatus.NotRun, flow.Steps[1].Status);
    }

    [Fact]
    public void Load_RejectsMismatchedResultEntry()
    {
        var root = CreateTempDirectory();
        var resultsDirectory = Path.Combine(root, "TestResults");
        Directory.CreateDirectory(resultsDirectory);

        var flowPath = Path.Combine(root, "flow.yaml");
        File.WriteAllText(
            flowPath,
            """
            specVersion: 1
            name: Mismatch Flow
            steps:
              - name: Expected name
                action: goto
                url: https://example.com
            """);

        File.WriteAllText(
            Path.Combine(resultsDirectory, "report.json"),
            """
            [
              {
                "Name": "Different name",
                "Action": "goto",
                "Passed": true,
                "DurationMs": 1
              }
            ]
            """);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            new FlowReportLoader().Load([flowPath]));

        Assert.Contains("does not match", ex.Message);
    }

    private static string CreateVersionOneFlow(string root)
    {
        var directory = Path.Combine(root, "legacy");
        var resultsDirectory = Path.Combine(directory, "TestResults");
        Directory.CreateDirectory(resultsDirectory);

        var flowPath = Path.Combine(directory, "legacy.yaml");
        File.WriteAllText(
            flowPath,
            """
            specVersion: 1
            name: Legacy Flow
            browser: chromium
            steps:
              - name: Open legacy
                action: goto
                url: https://example.com
            """);

        File.WriteAllText(
            Path.Combine(resultsDirectory, "report.json"),
            """
            [
              {
                "Name": "Open legacy",
                "Action": "goto",
                "Passed": true,
                "DurationMs": 10
              }
            ]
            """);

        return flowPath;
    }

    private static string CreateVersionTwoFlow(string root)
    {
        var directory = Path.Combine(root, "current");
        var resultsDirectory = Path.Combine(directory, "TestResults", "current");
        Directory.CreateDirectory(resultsDirectory);

        var flowPath = Path.Combine(directory, "current.yaml");
        File.WriteAllText(
            flowPath,
            """
            specVersion: 2
            name: Current Flow
            reportPath: TestResults/current/results.json
            browser: chromium
            steps:
              - name: Read text
                action: get-text
                selector: '#version'
              - name: Capture page
                action: screenshot
                path: TestResults/current/page.png
            """);

        File.WriteAllText(
            Path.Combine(resultsDirectory, "results.json"),
            """
            [
              {
                "Name": "Read text",
                "Action": "get-text",
                "Passed": true,
                "DurationMs": 20,
                "Data": "Version 1.0"
              }
            ]
            """);

        return flowPath;
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            $"playwright-report-loader-{Guid.NewGuid():N}");

        Directory.CreateDirectory(path);
        return path;
    }

    private static readonly byte[] TinyPng = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");
}
