using CommandLine;

namespace ProcessPal;

[Verb(
    "toggle-server", 
    aliases: new []{ "s" }, 
    HelpText = "Toggle the server on/off. The server manages the lifetime of the process groups. " +
               "If toggled off, all managed processes will be stopped.")]
public class ToggleServerOptions
{
    
}

[Verb(
    "toggle-group", 
    aliases: new []{ "g" }, 
    HelpText = "Toggle a process group on/off. " +
               "If any process in the group is running, all processes in the group will be stopped. " +
               "If no process in the group is running, all processes in the group will be started.")]
public class ToggleGroupOptions
{
    [Option('n', "name", Required = true, HelpText = "The name of the process group to toggle.")]
    public string Name { get; set; }
}