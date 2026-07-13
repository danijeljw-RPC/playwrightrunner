using System.Text.Json;
using ScriptTrail.Models;

namespace ScriptTrail.Services;

public sealed class ReportWriter
{
    public async Task WriteAsync(
        IReadOnlyCollection<StepResult> results,
        string reportPath)
    {
        var directory = Path.GetDirectoryName(reportPath);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

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