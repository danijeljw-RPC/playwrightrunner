using PlaywrightRunner.Services;
using Xunit;

namespace PlaywrightRunner.Tests;

public sealed class FlowFileLoaderTests
{
    [Fact]
    public void Load_AcceptsYamlWithoutSpecVersionAsVersionOne()
    {
        var path = WriteTempFile(
            ".yaml",
            """
            name: Legacy Flow
            browser: chromium
            headless: true
            steps: []
            """);

        var flow = FlowFileLoader.Load(path);

        Assert.Equal(1, flow.SpecVersion);
        Assert.Equal("Legacy Flow", flow.Name);
    }

    [Fact]
    public void Load_AcceptsSupportedSpecVersion()
    {
        var path = WriteTempFile(
            ".json",
            """
            {
              "specVersion": 1,
              "name": "Versioned Flow",
              "steps": []
            }
            """);

        var flow = FlowFileLoader.Load(path);

        Assert.Equal(1, flow.SpecVersion);
        Assert.Equal("Versioned Flow", flow.Name);
    }

    [Fact]
    public void Load_RejectsNewerSpecVersion()
    {
        var path = WriteTempFile(
            ".yaml",
            """
            specVersion: 2
            name: Future Flow
            steps: []
            """);

        var ex = Assert.Throws<InvalidOperationException>(() => FlowFileLoader.Load(path));

        Assert.Contains("specVersion 2", ex.Message);
        Assert.Contains("supports up to specVersion 1", ex.Message);
    }

    [Fact]
    public void Load_ParsesBrowserContextOptions()
    {
        var path = WriteTempFile(
            ".yaml",
            """
            specVersion: 1
            name: Context Flow
            userAgent: Mozilla/5.0
            locale: en-US
            timezoneId: America/New_York
            viewport:
              width: 1365
              height: 768
            extraHttpHeaders:
              accept-language: en-US,en;q=0.9
            steps: []
            """);

        var flow = FlowFileLoader.Load(path);

        Assert.Equal("Mozilla/5.0", flow.UserAgent);
        Assert.Equal("en-US", flow.Locale);
        Assert.Equal("America/New_York", flow.TimezoneId);
        Assert.Equal(1365, flow.Viewport!.Width);
        Assert.Equal(768, flow.Viewport.Height);
        Assert.Equal("en-US,en;q=0.9", flow.ExtraHttpHeaders!["accept-language"]);
    }

    [Fact]
    public void Load_RejectsHeadedBrowserMode()
    {
        var path = WriteTempFile(
            ".yaml",
            """
            specVersion: 1
            name: Headed Flow
            browser: chromium
            headless: false
            steps: []
            """);

        var ex = Assert.Throws<InvalidOperationException>(() => FlowFileLoader.Load(path));

        Assert.Contains("headless: false", ex.Message);
        Assert.Contains("not supported", ex.Message);
    }

    private static string WriteTempFile(string extension, string content)
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}{extension}");
        File.WriteAllText(path, content);
        return path;
    }
}
