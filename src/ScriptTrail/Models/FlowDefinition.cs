namespace ScriptTrail.Models;

public sealed class FlowDefinition
{
    public int SpecVersion { get; set; } = FlowSpecification.LegacyVersion;
    public string Name { get; set; } = "Unnamed Flow";
    public string? ReportPath { get; set; }
    public string? BaseUrl { get; set; }
    public string Browser { get; set; } = "chromium";
    public bool Headless { get; set; } = true;
    public string? UserAgent { get; set; }
    public string? Locale { get; set; }
    public string? TimezoneId { get; set; }
    public FlowViewport? Viewport { get; set; }
    public Dictionary<string, string>? ExtraHttpHeaders { get; set; }
    public List<FlowStep> Steps { get; set; } = [];
}

public sealed class FlowViewport
{
    public int Width { get; set; }
    public int Height { get; set; }
}
