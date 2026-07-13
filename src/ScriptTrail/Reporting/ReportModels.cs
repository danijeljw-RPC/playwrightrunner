using ScriptTrail.Models;

namespace ScriptTrail.Reporting;

public enum ReportStepStatus
{
    Passed,
    Failed,
    NotRun
}

public sealed record ReportField(string Label, string Value);

public sealed record ReportStep(
    int Number,
    FlowStep Definition,
    StepResult? Result,
    ReportStepStatus Status,
    IReadOnlyList<ReportField> Requirements,
    string? ScreenshotPath,
    string? Warning);

public sealed record FlowReport(
    string InputPath,
    string ResultPath,
    FlowDefinition Flow,
    IReadOnlyList<ReportStep> Steps)
{
    public int PassedCount =>
        Steps.Count(step => step.Status == ReportStepStatus.Passed);

    public int FailedCount =>
        Steps.Count(step => step.Status == ReportStepStatus.Failed);

    public int NotRunCount =>
        Steps.Count(step => step.Status == ReportStepStatus.NotRun);

    public long DurationMs =>
        Steps.Sum(step => step.Result?.DurationMs ?? 0);
}

public sealed record TestReport(
    DateTimeOffset GeneratedAt,
    IReadOnlyList<FlowReport> Flows)
{
    public int PassedCount => Flows.Sum(flow => flow.PassedCount);
    public int FailedCount => Flows.Sum(flow => flow.FailedCount);
    public int NotRunCount => Flows.Sum(flow => flow.NotRunCount);
    public int StepCount => Flows.Sum(flow => flow.Steps.Count);
    public long DurationMs => Flows.Sum(flow => flow.DurationMs);
}
