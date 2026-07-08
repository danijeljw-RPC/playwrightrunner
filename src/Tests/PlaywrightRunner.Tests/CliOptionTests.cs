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
        Assert.Equal($"0.2.0{Environment.NewLine}", result.StandardOutput);
        Assert.Equal("", result.StandardError);
    }

    [Theory]
    [InlineData("-h")]
    [InlineData("--help")]
    public async Task HelpOption_PrintsUsageAndExitsZero(string option)
    {
        var result = await RunCliAsync(option);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Usage: PlaywrightRunner <flow.json|flow.yaml>", result.StandardOutput);
        Assert.Contains("-h, --help", result.StandardOutput);
        Assert.Contains("-v, --version", result.StandardOutput);
        Assert.Equal("", result.StandardError);
    }

    private static async Task<CliResult> RunCliAsync(string argument)
    {
        var projectPath = Path.Combine(FindRepoRoot(), "src", "PlaywrightRunner", "PlaywrightRunner.csproj");

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        startInfo.ArgumentList.Add("run");
        startInfo.ArgumentList.Add("--no-build");
        startInfo.ArgumentList.Add("--project");
        startInfo.ArgumentList.Add(projectPath);
        startInfo.ArgumentList.Add("--");
        startInfo.ArgumentList.Add(argument);

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Could not start dotnet process.");

        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));

        return new CliResult(
            process.ExitCode,
            await stdoutTask,
            await stderrTask);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);

        while (dir is not null)
        {
            var projectPath = Path.Combine(dir.FullName, "src", "PlaywrightRunner", "PlaywrightRunner.csproj");
            if (File.Exists(projectPath))
                return dir.FullName;

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root.");
    }

    private sealed record CliResult(int ExitCode, string StandardOutput, string StandardError);
}
