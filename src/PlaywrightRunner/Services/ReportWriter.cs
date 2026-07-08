using System.Text.Json;
using PlaywrightRunner.Models;

namespace PlaywrightRunner.Services;

public sealed class ReportWriter
{
    public async Task WriteAsync(IReadOnlyCollection<StepResult> results)
    {
        Directory.CreateDirectory("TestResults");

        var reportPath = Path.Combine("TestResults", "report.json");

        await File.WriteAllTextAsync(
            reportPath,
            JsonSerializer.Serialize(results, new JsonSerializerOptions
            {
                WriteIndented = true
            }));

        Console.WriteLine();
        Console.WriteLine($"Report written: {reportPath}");
    }
}