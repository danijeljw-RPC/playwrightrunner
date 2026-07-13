using System.Text.Json;
using PlaywrightRunner.Models;
using PlaywrightRunner.Services;

namespace PlaywrightRunner.Reporting;

public sealed class FlowReportLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public TestReport Load(IReadOnlyList<string> inputPaths)
    {
        if (inputPaths.Count == 0)
            throw new InvalidOperationException("At least one flow input is required.");

        var flows = inputPaths
            .Select(LoadFlow)
            .ToArray();

        return new TestReport(DateTimeOffset.Now, flows);
    }

    private static FlowReport LoadFlow(string inputPath)
    {
        var fullInputPath = Path.GetFullPath(inputPath);

        if (!File.Exists(fullInputPath))
            throw new FileNotFoundException("Flow input was not found.", fullInputPath);

        var flow = FlowFileLoader.Parse(fullInputPath);
        var configuredResultPath = FlowResultPathResolver.Resolve(flow);
        var inputDirectory = Path.GetDirectoryName(fullInputPath)!;
        var resultPath = ResolveRequiredFile(
            configuredResultPath,
            inputDirectory);

        var results = JsonSerializer.Deserialize<List<StepResult>>(
                File.ReadAllText(resultPath),
                JsonOptions)
            ?? throw new InvalidOperationException(
                $"Could not parse result JSON: {resultPath}");

        if (results.Count > flow.Steps.Count)
        {
            throw new InvalidOperationException(
                $"Result file contains {results.Count} entries, but flow '{flow.Name}' contains only {flow.Steps.Count} steps.");
        }

        var reportSteps = new List<ReportStep>(flow.Steps.Count);

        for (var index = 0; index < flow.Steps.Count; index++)
        {
            var definition = flow.Steps[index];
            var result = index < results.Count ? results[index] : null;

            if (result is not null)
                ValidateResultMatchesStep(flow, index, definition, result);

            var status = result switch
            {
                null => ReportStepStatus.NotRun,
                { Passed: true } => ReportStepStatus.Passed,
                _ => ReportStepStatus.Failed
            };

            var (screenshotPath, warning) = ResolveScreenshot(
                definition,
                result,
                inputDirectory,
                Path.GetDirectoryName(resultPath)!);

            reportSteps.Add(new ReportStep(
                index + 1,
                definition,
                result,
                status,
                StepRequirementFormatter.Format(flow, definition),
                screenshotPath,
                warning));
        }

        return new FlowReport(
            fullInputPath,
            resultPath,
            flow,
            reportSteps);
    }

    private static void ValidateResultMatchesStep(
        FlowDefinition flow,
        int index,
        FlowStep definition,
        StepResult result)
    {
        if (!string.Equals(definition.Name, result.Name, StringComparison.Ordinal) ||
            !string.Equals(definition.Action, result.Action, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Result entry {index + 1} does not match flow '{flow.Name}'. " +
                $"Expected '{definition.Name}' ({definition.Action}) but received " +
                $"'{result.Name}' ({result.Action}).");
        }
    }

    private static (string? Path, string? Warning) ResolveScreenshot(
        FlowStep definition,
        StepResult? result,
        string inputDirectory,
        string resultDirectory)
    {
        if (!string.Equals(
                definition.Action,
                "screenshot",
                StringComparison.OrdinalIgnoreCase))
        {
            return (null, null);
        }

        var configuredPath = !string.IsNullOrWhiteSpace(result?.Data)
            ? result.Data
            : definition.Path;

        if (string.IsNullOrWhiteSpace(configuredPath))
            return (null, "Screenshot step did not provide an image path.");

        var resolved = ResolveOptionalFile(
            configuredPath,
            inputDirectory,
            resultDirectory);

        return resolved is null
            ? (null, $"Screenshot image was not found: {configuredPath}")
            : (resolved, null);
    }

    private static string ResolveRequiredFile(
        string path,
        params string[] baseDirectories)
    {
        var candidates = GetCandidatePaths(path, baseDirectories).ToArray();
        var match = candidates.FirstOrDefault(File.Exists);

        if (match is not null)
            return match;

        throw new FileNotFoundException(
            "Result JSON was not found. Checked: " +
            string.Join("; ", candidates));
    }

    private static string? ResolveOptionalFile(
        string path,
        params string[] baseDirectories) =>
        GetCandidatePaths(path, baseDirectories)
            .FirstOrDefault(File.Exists);

    private static IEnumerable<string> GetCandidatePaths(
        string path,
        params string[] baseDirectories)
    {
        if (Path.IsPathRooted(path))
        {
            yield return Path.GetFullPath(path);
            yield break;
        }

        var candidates = baseDirectories
            .Where(directory => !string.IsNullOrWhiteSpace(directory))
            .Select(directory => Path.GetFullPath(Path.Combine(directory, path)))
            .ToList();

        candidates.Add(Path.GetFullPath(path));

        if (baseDirectories.Length > 0)
        {
            candidates.AddRange(
                baseDirectories
                    .Where(directory => !string.IsNullOrWhiteSpace(directory))
                    .Select(directory =>
                        Path.GetFullPath(Path.Combine(directory, Path.GetFileName(path)))));
        }

        foreach (var candidate in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
            yield return candidate;
    }
}
