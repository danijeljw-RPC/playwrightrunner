using Microsoft.Playwright;
using PlaywrightRunner.Models;

namespace PlaywrightRunner.Runtime;

public sealed class BrowserFactory
{
    public async Task<(IPlaywright Playwright, IBrowser Browser)> CreateAsync(FlowDefinition flow)
    {
        var playwright = await Playwright.CreateAsync();

        var browserType = flow.Browser.ToLowerInvariant() switch
        {
            "chromium" => playwright.Chromium,
            "firefox" => playwright.Firefox,
            "webkit" => playwright.Webkit,
            _ => throw new InvalidOperationException($"Unsupported browser: {flow.Browser}")
        };

        var browser = await browserType.LaunchAsync(new()
        {
            Headless = flow.Headless
        });

        return (playwright, browser);
    }
}