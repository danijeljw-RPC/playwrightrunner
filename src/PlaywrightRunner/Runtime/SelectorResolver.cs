using Microsoft.Playwright;
using PlaywrightRunner.Models;

namespace PlaywrightRunner.Runtime;

public sealed class SelectorResolver
{
    public ILocator Resolve(IPage page, FlowStep step)
    {
        var selector = step.Selector
            ?? throw new InvalidOperationException($"{step.Action} requires selector.");

        ILocator locator;

        if (selector.StartsWith("placeholder=", StringComparison.OrdinalIgnoreCase))
        {
            locator = page.GetByPlaceholder(selector["placeholder=".Length..]);
        }
        else if (selector.StartsWith("text=", StringComparison.OrdinalIgnoreCase))
        {
            locator = page.GetByText(selector["text=".Length..]);
        }
        else if (selector.StartsWith("testid=", StringComparison.OrdinalIgnoreCase))
        {
            locator = page.GetByTestId(selector["testid=".Length..]);
        }
        else if (selector.StartsWith("role=button", StringComparison.OrdinalIgnoreCase))
        {
            var name = ExtractName(selector);
            locator = page.GetByRole(AriaRole.Button, new() { Name = name });
        }
        else
        {
            locator = page.Locator(selector);
        }

        return step.Index.HasValue
            ? locator.Nth(step.Index.Value)
            : locator;
    }

    private static string ExtractName(string selector)
    {
        const string marker = "name='";

        var start = selector.IndexOf(marker, StringComparison.OrdinalIgnoreCase);

        if (start < 0)
            throw new InvalidOperationException($"Role selector requires name='...': {selector}");

        start += marker.Length;

        var end = selector.IndexOf('\'', start);

        if (end < 0)
            throw new InvalidOperationException($"Invalid role selector: {selector}");

        return selector[start..end];
    }
}