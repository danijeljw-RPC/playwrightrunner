using ScriptTrail.Models;

namespace ScriptTrail.Reporting;

public static class StepRequirementFormatter
{
    public static IReadOnlyList<ReportField> Format(
        FlowDefinition flow,
        FlowStep step)
    {
        var fields = new List<ReportField>();
        var action = step.Action.ToLowerInvariant();

        Add(fields, "Frame", step.FrameSelector);
        Add(fields, "Selector", step.Selector);

        if (!string.IsNullOrWhiteSpace(step.Url))
            Add(fields, "URL", ResolveUrl(flow, step.Url));

        if (action == "fill" && step.Value is not null)
        {
            Add(fields, "Value", "[redacted]");
        }
        else if (action == "expect-text")
        {
            Add(fields, "Expected text", step.Value ?? String.Empty);
        }
        else if (action == "expect-url")
        {
            Add(fields, "Expected URL pattern", step.Value ?? String.Empty);
        }
        else if (action == "press")
        {
            Add(fields, "Key", step.Value ?? String.Empty);
        }
        else if (action == "select")
        {
            IReadOnlyList<string> selectedValues = step.Values ??
                (step.Value is null ? [] : [step.Value]);

            if (selectedValues.Count > 0)
                Add(fields, "Selected value(s)", string.Join(", ", selectedValues));
        }
        else if (!string.IsNullOrWhiteSpace(step.Value))
        {
            Add(fields, "Value", step.Value);
        }

        if (step.Values is { Count: > 0 } && action is not "select")
            Add(fields, "Values", string.Join(", ", step.Values));

        if (action == "api-request")
        {
            Add(fields, "Method", step.Method);

            if (step.Status.HasValue)
                Add(fields, "Expected status", step.Status.Value.ToString());

            if (step.Headers is { Count: > 0 })
            {
                Add(
                    fields,
                    "Headers",
                    string.Join(", ", step.Headers.Select(FormatHeader)));
            }

            if (!string.IsNullOrWhiteSpace(step.Data))
                Add(fields, "Request body", "[redacted]");
        }

        Add(fields, "Path", step.Path);

        if (step.Index.HasValue)
            Add(fields, "Match index", step.Index.Value.ToString());

        if (action == "screenshot")
            Add(fields, "Full page", step.FullPage ? "Yes" : "No");

        Add(
            fields,
            action == "wait" ? "Wait duration" : "Timeout",
            $"{step.TimeoutMs:N0} ms");

        return fields;
    }

    private static string FormatHeader(KeyValuePair<string, string> header)
    {
        var sensitive = header.Key.Contains("authorization", StringComparison.OrdinalIgnoreCase) ||
            header.Key.Contains("cookie", StringComparison.OrdinalIgnoreCase) ||
            header.Key.Contains("token", StringComparison.OrdinalIgnoreCase) ||
            header.Key.Contains("secret", StringComparison.OrdinalIgnoreCase) ||
            header.Key.Contains("api-key", StringComparison.OrdinalIgnoreCase) ||
            header.Key.Contains("apikey", StringComparison.OrdinalIgnoreCase);

        return sensitive
            ? $"{header.Key}: [redacted]"
            : $"{header.Key}: {header.Value}";
    }

    private static string ResolveUrl(FlowDefinition flow, string url)
    {
        if (!string.IsNullOrWhiteSpace(flow.BaseUrl) &&
            Uri.IsWellFormedUriString(url, UriKind.Relative))
        {
            return flow.BaseUrl.TrimEnd('/') + "/" + url.TrimStart('/');
        }

        return url;
    }

    private static void Add(
        ICollection<ReportField> fields,
        string label,
        string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            fields.Add(new ReportField(label, value));
    }
}
