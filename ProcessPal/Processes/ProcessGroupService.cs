using ProcessPal.Util;

namespace ProcessPal.Processes;

public class ProcessGroupService
{
    private ProcessManager[] _processes;

    private readonly (ConsoleColor, ConsoleColor)[] _consoleColors = {
        (ConsoleColor.Yellow,      ConsoleColor.Black),
        (ConsoleColor.Cyan,        ConsoleColor.Black),
        (ConsoleColor.Magenta,     ConsoleColor.Black),
        (ConsoleColor.White,       ConsoleColor.Black),
        (ConsoleColor.Green,       ConsoleColor.Black),
        (ConsoleColor.DarkCyan,    ConsoleColor.Black),
        (ConsoleColor.Gray,        ConsoleColor.Black)
    };
    
    private readonly TaskQueue _outputTaskQueue = new();
    
    public void Start(ProcessGroupConfig config)
    {
        var maxNameLength = config.Scripts.Select(x => x.Name.Length).Max();
        _processes = config.Scripts
            .Select((x, i) =>
            {
                var colors = _consoleColors[i % _consoleColors.Length];
                return new ProcessManager(
                    x,
                    colors.Item1,
                    colors.Item2,
                    namePadRight: maxNameLength,
                    _outputTaskQueue);
            })
            .ToArray();
        
        foreach(var process in _processes)
        {
            process.Start();
        }
    }

    public void Stop()
    {
        foreach (var process in _processes)
        {
            process.Stop();
        }
    }
}