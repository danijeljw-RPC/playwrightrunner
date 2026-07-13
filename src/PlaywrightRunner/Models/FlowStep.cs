namespace PlaywrightRunner.Models;

public sealed class FlowStep
{
    public string Name { get; set; } = "Unnamed Step";
    public string Action { get; set; } = "";
    public string? Url { get; set; }
    public string? FrameSelector { get; set; }
    public string? Selector { get; set; }
    public string? Value { get; set; }
    public List<string>? Values { get; set; }
    public string? Path { get; set; }
    public string Method { get; set; } = "GET";
    public int? Status { get; set; }
    public Dictionary<string, string>? Headers { get; set; }
    public string? Data { get; set; }
    public int? Index { get; set; }
    public bool FullPage { get; set; }
    public int TimeoutMs { get; set; } = 10_000;
}