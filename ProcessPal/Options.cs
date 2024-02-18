using CommandLine;

namespace ProcessPal;

[Verb("toggle", aliases: new []{ "t" }, HelpText = "Toggle a process group.")]
public class ToggleOptions
{
    [Option('n', "name", Required = true, HelpText = "The name of the process group to toggle.")]
    public string Name { get; set; }
}

[Verb("shutdown", aliases: new []{ "s" }, HelpText = "Shutdown the server.")]
public class ShutdownOptions
{
    
}