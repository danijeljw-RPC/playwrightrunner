using System.Text.RegularExpressions;
using Microsoft.Playwright;
using PlaywrightRunner.Models;

namespace PlaywrightRunner.Runtime;

public sealed class FlowStepExecutor
{
    private readonly SelectorResolver _selectorResolver;

    public FlowStepExecutor(SelectorResolver selectorResolver)
    {
        _selectorResolver = selectorResolver;
    }

    public async Task RunAsync(IPage page, FlowDefinition flow, FlowStep step)
    {
        switch (step.Action.ToLowerInvariant())
        {
            case "goto":
                await GotoAsync(page, flow, step);
                break;

            case "click":
                await ClickAsync(page, step);
                break;

            case "fill":
                await FillAsync(page, step);
                break;

            case "select":
                await SelectAsync(page, step);
                break;

            case "check":
                await CheckAsync(page, step);
                break;

            case "uncheck":
                await UncheckAsync(page, step);
                break;

            case "press":
                await PressAsync(page, step);
                break;

            case "hover":
                await HoverAsync(page, step);
                break;

            case "upload":
                await UploadAsync(page, step);
                break;

            case "download":
                await DownloadAsync(page, step);
                break;

            case "api-request":
                await ApiRequestAsync(page, flow, step);
                break;

            case "trace-start":
                await TraceStartAsync(page);
                break;

            case "trace-stop":
                await TraceStopAsync(page, step);
                break;

            case "expect-visible":
                await ExpectVisibleAsync(page, step);
                break;

            case "expect-text":
                await ExpectTextAsync(page, step);
                break;

            case "get-text":
                await GetTextAsync(page, step);
                break;

            case "expect-url":
                await ExpectUrlAsync(page, step);
                break;

            case "screenshot":
                await ScreenshotAsync(page, step);
                break;

            case "wait":
                await page.WaitForTimeoutAsync(step.TimeoutMs);
                break;

            default:
                throw new InvalidOperationException($"Unsupported action: {step.Action}");
        }
    }

    private static async Task GotoAsync(IPage page, FlowDefinition flow, FlowStep step)
    {
        var url = step.Url ?? throw new InvalidOperationException("goto requires url.");

        url = ResolveUrl(flow, url);

        await page.GotoAsync(url, new()
        {
            Timeout = step.TimeoutMs
        });
    }

    private async Task ClickAsync(IPage page, FlowStep step)
    {
        var locator = _selectorResolver.Resolve(page, step);

        await locator.ClickAsync(new()
        {
            Timeout = step.TimeoutMs
        });
    }

    private async Task FillAsync(IPage page, FlowStep step)
    {
        var locator = _selectorResolver.Resolve(page, step);

        await locator.FillAsync(step.Value ?? "", new()
        {
            Timeout = step.TimeoutMs
        });
    }

    private async Task SelectAsync(IPage page, FlowStep step)
    {
        var locator = _selectorResolver.Resolve(page, step);
        var values = step.Values ?? [step.Value ?? throw new InvalidOperationException("select requires value or values.")];

        await locator.SelectOptionAsync(values, new()
        {
            Timeout = step.TimeoutMs
        });
    }

    private async Task CheckAsync(IPage page, FlowStep step)
    {
        var locator = _selectorResolver.Resolve(page, step);

        await locator.CheckAsync(new()
        {
            Timeout = step.TimeoutMs
        });
    }

    private async Task UncheckAsync(IPage page, FlowStep step)
    {
        var locator = _selectorResolver.Resolve(page, step);

        await locator.UncheckAsync(new()
        {
            Timeout = step.TimeoutMs
        });
    }

    private async Task PressAsync(IPage page, FlowStep step)
    {
        var key = step.Value
            ?? throw new InvalidOperationException("press requires value.");
        var locator = _selectorResolver.Resolve(page, step);

        await locator.PressAsync(key, new()
        {
            Timeout = step.TimeoutMs
        });
    }

    private async Task HoverAsync(IPage page, FlowStep step)
    {
        var locator = _selectorResolver.Resolve(page, step);

        await locator.HoverAsync(new()
        {
            Timeout = step.TimeoutMs
        });
    }

    private async Task UploadAsync(IPage page, FlowStep step)
    {
        var locator = _selectorResolver.Resolve(page, step);
        var paths = step.Values ?? [step.Path ?? throw new InvalidOperationException("upload requires path or values.")];

        await locator.SetInputFilesAsync(paths, new()
        {
            Timeout = step.TimeoutMs
        });
    }

    private async Task DownloadAsync(IPage page, FlowStep step)
    {
        var path = step.Path
            ?? throw new InvalidOperationException("download requires path.");
        var locator = _selectorResolver.Resolve(page, step);

        EnsureDirectory(path);

        var download = await page.RunAndWaitForDownloadAsync(
            async () => await locator.ClickAsync(new() { Timeout = step.TimeoutMs }),
            new() { Timeout = step.TimeoutMs });

        await download.SaveAsAsync(path);
    }

    private static async Task ApiRequestAsync(IPage page, FlowDefinition flow, FlowStep step)
    {
        var url = step.Url
            ?? throw new InvalidOperationException("api-request requires url.");

        var response = await page.APIRequest.FetchAsync(ResolveUrl(flow, url), new()
        {
            Method = step.Method,
            Headers = step.Headers,
            DataString = step.Data,
            Timeout = step.TimeoutMs
        });

        if (step.Status.HasValue && response.Status != step.Status.Value)
        {
            throw new InvalidOperationException(
                $"api-request expected status {step.Status.Value} but received {response.Status}.");
        }

        if (!string.IsNullOrWhiteSpace(step.Path))
        {
            EnsureDirectory(step.Path);
            await File.WriteAllTextAsync(step.Path, await response.TextAsync());
        }
    }

    private static async Task TraceStartAsync(IPage page)
    {
        await page.Context.Tracing.StartAsync(new()
        {
            Screenshots = true,
            Snapshots = true,
            Sources = true
        });
    }

    private static async Task TraceStopAsync(IPage page, FlowStep step)
    {
        var path = step.Path
            ?? throw new InvalidOperationException("trace-stop requires path.");

        EnsureDirectory(path);

        await page.Context.Tracing.StopAsync(new()
        {
            Path = path
        });
    }

    private async Task ExpectVisibleAsync(IPage page, FlowStep step)
    {
        var locator = _selectorResolver.Resolve(page, step);

        await Assertions.Expect(locator).ToBeVisibleAsync(new()
        {
            Timeout = step.TimeoutMs
        });
    }

    private async Task ExpectTextAsync(IPage page, FlowStep step)
    {
        var locator = _selectorResolver.Resolve(page, step);

        await Assertions.Expect(locator).ToHaveTextAsync(step.Value ?? "", new()
        {
            Timeout = step.TimeoutMs
        });
    }

    private async Task GetTextAsync(IPage page, FlowStep step)
    {
        var locator = _selectorResolver.Resolve(page, step);

        await locator.WaitForAsync(new()
        {
            State = WaitForSelectorState.Visible,
            Timeout = step.TimeoutMs
        });

        var text = (await locator.InnerTextAsync()).Trim();

        Console.WriteLine($"     {step.Name}: {text}");
    }

    private static async Task ExpectUrlAsync(IPage page, FlowStep step)
    {
        var value = step.Value
            ?? throw new InvalidOperationException("expect-url requires value.");

        await Assertions.Expect(page).ToHaveURLAsync(
            new Regex(value),
            new()
            {
                Timeout = step.TimeoutMs
            });
    }

    private static async Task ScreenshotAsync(IPage page, FlowStep step)
    {
        var path = step.Path
            ?? throw new InvalidOperationException("screenshot requires path.");

        var dir = Path.GetDirectoryName(path);

        EnsureDirectory(path);

        await page.ScreenshotAsync(new()
        {
            Path = path,
            FullPage = step.FullPage
        });
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

    private static void EnsureDirectory(string path)
    {
        var dir = Path.GetDirectoryName(path);

        if (!string.IsNullOrWhiteSpace(dir))
            Directory.CreateDirectory(dir);
    }
}
