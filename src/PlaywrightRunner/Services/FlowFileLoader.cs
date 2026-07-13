using System.Text.Json;
using PlaywrightRunner.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace PlaywrightRunner.Services;

public static class FlowFileLoader
{
    private const int CurrentSpecVersion = 2;

    public static FlowDefinition Load(string path)
    {
        var text = File.ReadAllText(path);
        var extension = Path.GetExtension(path).ToLowerInvariant();

        FlowDefinition? flow;

        if (extension is ".yaml" or ".yml")
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            flow = deserializer.Deserialize<FlowDefinition>(text);
        }
        else
        {
            flow = JsonSerializer.Deserialize<FlowDefinition>(
                text,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
        }

        if (flow is null)
            throw new InvalidOperationException("Could not parse flow file.");

        if (flow.SpecVersion < FlowSpecification.MinimumSupportedVersion)
        {
            throw new InvalidOperationException(
                $"specVersion must be {FlowSpecification.MinimumSupportedVersion} or greater.");
        }

        if (flow.SpecVersion > FlowSpecification.CurrentVersion)
        {
            throw new InvalidOperationException(
                $"specVersion {flow.SpecVersion} is not supported. " +
                $"The latest supported version is {FlowSpecification.CurrentVersion}.");
        }

        if (!flow.Headless)
        {
            throw new InvalidOperationException(
                "headless: false is not supported. PlaywrightRunner is a CLI-only tool and always runs browsers headlessly.");
        }

        return flow;
    }
}
