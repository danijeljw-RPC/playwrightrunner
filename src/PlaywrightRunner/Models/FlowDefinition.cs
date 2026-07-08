namespace PlaywrightRunner.Models;

public sealed class FlowDefinition
{
    public string Name { get; set; } = "Unnamed Flow";
    public string? BaseUrl { get; set; }
    public string Browser { get; set; } = "chromium";
    public bool Headless { get; set; } = true;
    public List<FlowStep> Steps { get; set; } = [];
}