using System.Diagnostics;
using Xunit;

namespace PlaywrightRunner.Tests;

public sealed class CliOptionTests
{
    [Fact]
    public async Task VersionOption_PrintsOnlyVersionAndExitsZero()
    {
        var result = await RunCliAsync("-v");

        Assert.Equal(0, result.ExitCode);
        Assert.Equal($"0.3.0{Environment.NewLine}", result.StandardOutput);
        Assert.Equal("", result.StandardError);
    }

    [Theory]
    [InlineData("-h")]
    [InlineData("--help")]
    public async Task HelpOption_PrintsUsageAndExitsZero(string option)
    {
        var result = await RunCliAsync(option);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("PlaywrightRunner <flow.json|flow.yaml>", result.StandardOutput);
        Assert.Contains("PlaywrightRunner --report", result.StandardOutput);
        Assert.Contains("--input", result.StandardOutput);
        Assert.Contains("--output", result.StandardOutput);
        Assert.Equal("", result.StandardError);
    }

    [Fact]
    public async Task ReportOption_GeneratesPdf()
    {
        var root = Path.Combine(Path.GetTempPath(), $"playwright-report-{Guid.NewGuid():N}");
        var resultsDirectory = Path.Combine(root, "TestResults");
        Directory.CreateDirectory(resultsDirectory);

        var flowPath = Path.Combine(root, "flow.yaml");
        var outputPath = Path.Combine(root, "combined.pdf");

        await File.WriteAllTextAsync(
            flowPath,
            """
            specVersion: 1
            name: Report Test
            browser: chromium
            headless: true
            steps:
              - name: Open page
                action: goto
                url: https://example.com
            """);

        await File.WriteAllTextAsync(
            Path.Combine(resultsDirectory, "report.json"),
            """
            [
              {
                "Name": "Open page",
                "Action": "goto",
                "Passed": true,
                "Error": null,
                "DurationMs": 25,
                "Data": null
              }
            ]
            """);

        var result = await RunCliAsync(
            "--report",
            "--output",
            outputPath,
            "--input",
            flowPath);

        Assert.Equal(0, result.ExitCode);
        Assert.True(File.Exists(outputPath));
        Assert.Equal("%PDF", await ReadPrefixAsync(outputPath, 4));
        Assert.Contains("PDF report written:", result.StandardOutput);
        Assert.Equal("", result.StandardError);
    }

    private static async Task<CliResult> RunCliAsync(params string[] arguments)
    {
        var projectPath = Path.Combine(
            FindRepoRoot(),
            "src",
            "PlaywrightRunner",
            "PlaywrightRunner.csproj");

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        startInfo.ArgumentList.Add("run");
        startInfo.ArgumentList.Add("--no-build");
        startInfo.ArgumentList.Add("--configuration");
        startInfo.ArgumentList.Add(GetBuildConfiguration());
        startInfo.ArgumentList.Add("--project");
        startInfo.ArgumentList.Add(projectPath);
        startInfo.ArgumentList.Add("--");

        foreach (var argument in arguments)
            startInfo.ArgumentList.Add(argument);

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Could not start dotnet process.");

        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(30));

        return new CliResult(
            process.ExitCode,
            await stdoutTask,
            await stderrTask);
    }

    private static async Task<string> ReadPrefixAsync(string path, int length)
    {
        var buffer = new byte[length];
        await using var stream = File.OpenRead(path);
        _ = await stream.ReadAsync(buffer);
        return System.Text.Encoding.ASCII.GetString(buffer);
    }

    private static string GetBuildConfiguration()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (directory.Name is "Debug" or "Release")
                return directory.Name;

            directory = directory.Parent;
        }

        return "Debug";
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);

        while (dir is not null)
        {
            var projectPath = Path.Combine(
                dir.FullName,
                "src",
                "PlaywrightRunner",
                "PlaywrightRunner.csproj");

            if (File.Exists(projectPath))
                return dir.FullName;

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root.");
    }

    private sealed record CliResult(
        int ExitCode,
        string StandardOutput,
        string StandardError);
}
