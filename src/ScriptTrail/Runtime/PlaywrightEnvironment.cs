namespace ScriptTrail.Runtime;

internal static class PlaywrightEnvironment
{
    private const string BrowsersPathVariable = "PLAYWRIGHT_BROWSERS_PATH";
    private const string DriverSearchPathVariable = "PLAYWRIGHT_DRIVER_SEARCH_PATH";

    public static void Configure(string applicationDirectory)
    {
        Environment.SetEnvironmentVariable(
            BrowsersPathVariable,
            Path.Combine(applicationDirectory, "ms-playwright"));

        var driverSearchPath = Environment.GetEnvironmentVariable(DriverSearchPathVariable);

        if (!IsUsableDriverSearchPath(driverSearchPath))
            Environment.SetEnvironmentVariable(DriverSearchPathVariable, null);
    }

    internal static bool IsUsableDriverSearchPath(string? path) =>
        string.IsNullOrWhiteSpace(path) ||
        Directory.Exists(Path.Combine(path, ".playwright"));
}
