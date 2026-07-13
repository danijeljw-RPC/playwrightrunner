namespace PlaywrightRunner.Models;

public sealed class StepResult
{
    public string Name { get; init; } = String.Empty;
    public string Action { get; init; } = String.Empty;
    public bool Passed { get; init; }
    public string? Error { get; init; }
    public long DurationMs { get; init; }

    /// <summary>
    /// Action-specific output, such as extracted text or an output file path.
    /// </summary>
    public string? Data { get; init; }
}