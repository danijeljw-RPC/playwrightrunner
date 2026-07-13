using System.Reflection;
using ScriptTrail.Cli;

var version = Assembly.GetExecutingAssembly()
    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
    .InformationalVersion ?? "0.0.0";

return await new CliApplication(version).RunAsync(args);
