using System.Text.Json;
using PlaywrightRunner.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace PlaywrightRunner.Services;

public static class FlowFileLoader
{
    public static FlowDefinition Load(string path)
    {
        var text = File.ReadAllText(path);
        var extension = Path.GetExtension(path).ToLowerInvariant();

        if (extension is ".yaml" or ".yml")
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            return deserializer.Deserialize<FlowDefinition>(text);
        }

        return JsonSerializer.Deserialize<FlowDefinition>(
            text,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? throw new InvalidOperationException("Could not parse flow file.");
    }
}