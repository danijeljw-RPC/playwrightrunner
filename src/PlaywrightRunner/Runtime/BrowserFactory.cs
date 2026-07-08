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
            Headless = true
        });

        return (playwright, browser);
    }

    public async Task<IBrowserContext> CreateContextAsync(IBrowser browser, FlowDefinition flow)
    {
        var options = new BrowserNewContextOptions
        {
            ExtraHTTPHeaders = flow.ExtraHttpHeaders,
            Locale = flow.Locale,
            TimezoneId = flow.TimezoneId,
            UserAgent = flow.UserAgent
        };

        if (flow.Viewport is not null)
        {
            options.ViewportSize = new ViewportSize
            {
                Width = flow.Viewport.Width,
                Height = flow.Viewport.Height
            };
        }

        return await browser.NewContextAsync(options);
    }
}
