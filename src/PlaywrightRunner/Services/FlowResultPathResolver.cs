using PlaywrightRunner.Models;

namespace PlaywrightRunner.Services;

public static class FlowResultPathResolver
{
    public const string LegacyDefaultPath = "TestResults/report.json";

    public static string Resolve(FlowDefinition flow)
    {
        if (!string.IsNullOrWhiteSpace(flow.ReportPath))
            return flow.ReportPath;

        if (flow.SpecVersion == FlowSpecification.LegacyVersion)
            return LegacyDefaultPath;

        throw new InvalidOperationException(
            $"specVersion {flow.SpecVersion} requires reportPath.");
    }
}
