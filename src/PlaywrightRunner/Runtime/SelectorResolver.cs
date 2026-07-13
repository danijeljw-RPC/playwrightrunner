using Microsoft.Playwright;
using PlaywrightRunner.Models;

namespace PlaywrightRunner.Runtime;

public sealed class SelectorResolver
{
    public ILocator Resolve(IPage page, FlowStep step)
    {
        var selector = step.Selector
            ?? throw new InvalidOperationException(
                $"{step.Action} requires selector.");

        var locator = string.IsNullOrWhiteSpace(step.FrameSelector)
            ? ResolveFromPage(page, selector)
            : ResolveFromFrame(
                page.FrameLocator(step.FrameSelector),
                selector);

        return step.Index.HasValue
            ? locator.Nth(step.Index.Value)
            : locator;
    }

    private static ILocator ResolveFromPage(
        IPage page,
        string selector)
    {
        if (selector.StartsWith(
                "placeholder=",
                StringComparison.OrdinalIgnoreCase))
        {
            return page.GetByPlaceholder(
                selector["placeholder=".Length..]);
        }

        if (selector.StartsWith(
                "text=",
                StringComparison.OrdinalIgnoreCase))
        {
            return page.GetByText(
                selector["text=".Length..]);
        }

        if (selector.StartsWith(
                "testid=",
                StringComparison.OrdinalIgnoreCase))
        {
            return page.GetByTestId(
                selector["testid=".Length..]);
        }

        if (selector.StartsWith(
                "role=button",
                StringComparison.OrdinalIgnoreCase))
        {
            return page.GetByRole(
                AriaRole.Button,
                new() { Name = ExtractName(selector) });
        }

        return page.Locator(selector);
    }

    private static ILocator ResolveFromFrame(
        IFrameLocator frame,
        string selector)
    {
        if (selector.StartsWith(
                "placeholder=",
                StringComparison.OrdinalIgnoreCase))
        {
            return frame.GetByPlaceholder(
                selector["placeholder=".Length..]);
        }

        if (selector.StartsWith(
                "text=",
                StringComparison.OrdinalIgnoreCase))
        {
            return frame.GetByText(
                selector["text=".Length..]);
        }

        if (selector.StartsWith(
                "testid=",
                StringComparison.OrdinalIgnoreCase))
        {
            return frame.GetByTestId(
                selector["testid=".Length..]);
        }

        if (selector.StartsWith(
                "role=button",
                StringComparison.OrdinalIgnoreCase))
        {
            return frame.GetByRole(
                AriaRole.Button,
                new() { Name = ExtractName(selector) });
        }

        return frame.Locator(selector);
    }

    private static string ExtractName(string selector)
    {
        const string marker = "name='";

        var start = selector.IndexOf(
            marker,
            StringComparison.OrdinalIgnoreCase);

        if (start < 0)
        {
            throw new InvalidOperationException(
                $"Role selector requires name='...': {selector}");
        }

        start += marker.Length;

        var end = selector.IndexOf('\'', start);

        if (end < 0)
        {
            throw new InvalidOperationException(
                $"Invalid role selector: {selector}");
        }

        return selector[start..end];
    }
}