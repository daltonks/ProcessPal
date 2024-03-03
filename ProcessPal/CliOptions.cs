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
    [Option('c', "config", Required = true, HelpText = "The path to the process group config yaml.")]
    public string ConfigPath { get; set; }
}