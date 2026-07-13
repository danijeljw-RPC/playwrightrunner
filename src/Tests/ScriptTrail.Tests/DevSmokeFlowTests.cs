using ScriptTrail.Services;
using Xunit;

namespace ScriptTrail.Tests;

public sealed class DevSmokeFlowTests
{
    [Theory]
    [InlineData("dev-smoke-v1.yaml", 1, null)]
    [InlineData(
        "dev-smoke-v2.yaml",
        2,
        "TestResults/dev-smoke-v2/report.json")]
    public void RootSmokeFlow_IsAValidSpecification(
        string fileName,
        int expectedVersion,
        string? expectedReportPath)
    {
        var flow = FlowFileLoader.Load(Path.Combine(FindRepoRoot(), fileName));

        Assert.Equal(expectedVersion, flow.SpecVersion);
        Assert.Equal(expectedReportPath, flow.ReportPath);
        Assert.Equal("chromium", flow.Browser);
        Assert.True(flow.Headless);
        Assert.NotEmpty(flow.Steps);
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "dev-smoke-v1.yaml")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root.");
    }
}
