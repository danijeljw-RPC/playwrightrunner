using System.Text.Json;
using ScriptTrail.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ScriptTrail.Services;

public static class FlowFileLoader
{
    public static FlowDefinition Load(string path)
    {
        var flow = Parse(path);
        ValidateForExecution(flow);
        return flow;
    }

    public static FlowDefinition Parse(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException("Flow file was not found.", path);

        var text = File.ReadAllText(path);
        var extension = Path.GetExtension(path).ToLowerInvariant();

        FlowDefinition? flow = extension switch
        {
            ".yaml" or ".yml" => ParseYaml(text),
            ".json" => ParseJson(text),
            _ => throw new InvalidOperationException(
                "Flow files must use the .yaml, .yml, or .json extension.")
        };

        if (flow is null)
            throw new InvalidOperationException("Could not parse flow file.");

        ValidateSupportedVersion(flow);
        return flow;
    }

    public static void ValidateForExecution(FlowDefinition flow)
    {
        ValidateSupportedVersion(flow);
        FlowResultPathResolver.Resolve(flow);

        if (!flow.Headless)
        {
            throw new InvalidOperationException(
                "headless: false is not supported. ScriptTrail is a CLI-only tool and always runs browsers headlessly.");
        }
    }

    private static FlowDefinition? ParseYaml(string text)
    {
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        return deserializer.Deserialize<FlowDefinition>(text);
    }

    private static FlowDefinition? ParseJson(string text) =>
        JsonSerializer.Deserialize<FlowDefinition>(
            text,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

    private static void ValidateSupportedVersion(FlowDefinition flow)
    {
        if (flow.SpecVersion < FlowSpecification.MinimumSupportedVersion)
        {
            throw new InvalidOperationException(
                $"specVersion must be {FlowSpecification.MinimumSupportedVersion} or greater.");
        }

        if (flow.SpecVersion > FlowSpecification.CurrentVersion)
        {
            throw new InvalidOperationException(
                $"specVersion {flow.SpecVersion} is not supported. " +
                $"The latest supported version is {FlowSpecification.CurrentVersion}.\n" +
                "Get the latest version from: https://github.com/danijeljw-RPC/playwrightrunner/releases/latest");
        }
    }
}
