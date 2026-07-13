using PlaywrightRunner.Models;
using PlaywrightRunner.Reporting;
using Xunit;

namespace PlaywrightRunner.Tests;

public sealed class StepRequirementFormatterTests
{
    [Fact]
    public void Format_RedactsFillValues()
    {
        var fields = StepRequirementFormatter.Format(
            new FlowDefinition(),
            new FlowStep
            {
                Action = "fill",
                Selector = "#password",
                Value = "secret-value"
            });

        Assert.Contains(fields, field =>
            field.Label == "Value" && field.Value == "[redacted]");
        Assert.DoesNotContain(fields, field => field.Value == "secret-value");
    }

    [Fact]
    public void Format_RedactsSensitiveApiHeadersAndBody()
    {
        var fields = StepRequirementFormatter.Format(
            new FlowDefinition
            {
                BaseUrl = "https://example.com"
            },
            new FlowStep
            {
                Action = "api-request",
                Url = "/private",
                Method = "POST",
                Headers = new Dictionary<string, string>
                {
                    ["Authorization"] = "Bearer secret-token",
                    ["content-type"] = "application/json"
                },
                Data = "{\"password\":\"secret\"}"
            });

        var headers = Assert.Single(fields.Where(field => field.Label == "Headers"));
        var body = Assert.Single(fields.Where(field => field.Label == "Request body"));

        Assert.Contains("Authorization: [redacted]", headers.Value);
        Assert.Contains("content-type: application/json", headers.Value);
        Assert.DoesNotContain("secret-token", headers.Value);
        Assert.Equal("[redacted]", body.Value);
    }

    [Fact]
    public void Format_ResolvesRelativeUrlAgainstBaseUrl()
    {
        var fields = StepRequirementFormatter.Format(
            new FlowDefinition
            {
                BaseUrl = "https://example.com/root"
            },
            new FlowStep
            {
                Action = "goto",
                Url = "/page"
            });

        var url = Assert.Single(fields.Where(field => field.Label == "URL"));

        Assert.Equal("https://example.com/root/page", url.Value);
    }
}
