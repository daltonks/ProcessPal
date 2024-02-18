using CommandLine;

namespace ProcessPal.Client;

[Verb("toggle", aliases: new []{ "t" }, HelpText = "Toggle a process group")]
public class ToggleOptions
{
    [Value(0, Required = true, HelpText = "The name of the process group to toggle")]
    public string Name { get; set; }
}

[Verb("shutdown", aliases: new []{ "s" }, HelpText = "Shutdown the server")]
public class ShutdownOptions
{
    
}