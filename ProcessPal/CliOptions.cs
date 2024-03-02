using CommandLine;

namespace ProcessPal;

[Verb(
    "toggle", 
    aliases: new []{ "g" }, 
    HelpText = "Toggle a process group on/off. " +
               "If the process group is running, the group and all its running processes will be stopped. " +
               "If the process group is not running, the group and its processes will be started.")]
public class ToggleGroupOptions
{
    [Option('n', "name", Required = true, HelpText = "The name of the process group to toggle.")]
    public string Name { get; set; }
}