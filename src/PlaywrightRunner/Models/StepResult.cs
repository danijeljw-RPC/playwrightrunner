namespace PlaywrightRunner.Models;

public sealed class StepResult
{
    public string Name { get; init; } = "";
    public string Action { get; init; } = "";
    public bool Passed { get; init; }
    public string? Error { get; init; }
    public long DurationMs { get; init; }
}