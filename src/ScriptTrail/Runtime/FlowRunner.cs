using System.Diagnostics;
using ScriptTrail.Models;
using ScriptTrail.Services;

namespace ScriptTrail.Runtime;

public sealed class FlowRunner
{
    private readonly BrowserFactory _browserFactory;
    private readonly FlowStepExecutor _stepExecutor;
    private readonly ReportWriter _reportWriter;

    public FlowRunner(
        BrowserFactory browserFactory,
        FlowStepExecutor stepExecutor,
        ReportWriter reportWriter)
    {
        _browserFactory = browserFactory;
        _stepExecutor = stepExecutor;
        _reportWriter = reportWriter;
    }

    public async Task<int> RunAsync(FlowDefinition flow)
    {
        var results = new List<StepResult>();

        var (playwright, browser) = await _browserFactory.CreateAsync(flow);

        using (playwright)
        {
            await using (browser)
            {
                var context = await _browserFactory.CreateContextAsync(browser, flow);

                await using (context)
                {
                    var page = await context.NewPageAsync();

                    Console.WriteLine($"Running: {flow.Name}");
                    Console.WriteLine();

                    foreach (var step in flow.Steps)
                    {
                        var result = await RunStepAsync(page, flow, step);
                        results.Add(result);

                        if (!result.Passed)
                            break;
                    }
                }
            }
        }

        await _reportWriter.WriteAsync(
            results,
            FlowResultPathResolver.Resolve(flow));

        return results.All(x => x.Passed) ? 0 : 1;
    }

    private async Task<StepResult> RunStepAsync(
        Microsoft.Playwright.IPage page,
        FlowDefinition flow,
        FlowStep step)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            var data = await _stepExecutor.RunAsync(page, flow, step);

            sw.Stop();
            Console.WriteLine($"PASS {step.Name} ({sw.ElapsedMilliseconds}ms)");

            return new StepResult
            {
                Name = step.Name,
                Action = step.Action,
                Passed = true,
                DurationMs = sw.ElapsedMilliseconds,
                Data = data
            };
        }
        catch (Exception ex)
        {
            sw.Stop();

            Console.WriteLine($"FAIL {step.Name} ({sw.ElapsedMilliseconds}ms)");
            Console.WriteLine($"     {ex.Message}");

            return new StepResult
            {
                Name = step.Name,
                Action = step.Action,
                Passed = false,
                Error = ex.Message,
                DurationMs = sw.ElapsedMilliseconds
            };
        }
    }
}
