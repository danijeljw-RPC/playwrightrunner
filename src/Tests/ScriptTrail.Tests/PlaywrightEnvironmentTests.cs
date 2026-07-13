using ScriptTrail.Runtime;
using Xunit;

namespace ScriptTrail.Tests;

public sealed class PlaywrightEnvironmentTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void IsUsableDriverSearchPath_AcceptsUnsetValues(string? path)
    {
        Assert.True(PlaywrightEnvironment.IsUsableDriverSearchPath(path));
    }

    [Fact]
    public void IsUsableDriverSearchPath_RejectsInvalidOverride()
    {
        Assert.False(PlaywrightEnvironment.IsUsableDriverSearchPath(
            "PLAYWRIGHT_DRIVER_SEARCH_PATH"));
    }

    [Fact]
    public void IsUsableDriverSearchPath_AcceptsDirectoryContainingDriverBundle()
    {
        var root = Path.Combine(Path.GetTempPath(), $"playwright-driver-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(root, ".playwright"));

        try
        {
            Assert.True(PlaywrightEnvironment.IsUsableDriverSearchPath(root));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
