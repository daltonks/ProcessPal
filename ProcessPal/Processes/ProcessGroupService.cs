using System.Collections.Concurrent;

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
    
    private int _maxNameLength;
    private readonly BlockingCollection<(ProcessManager, string, bool)> _outputCollection = new();
    
    public ProcessGroupService()
    {
        StartOutputThread();
    }
    
    public void Start(ProcessGroupConfig config)
    {
        _maxNameLength = config.Scripts.Select(x => x.Name.Length).Max();
        _processes = config.Scripts
            .Select((x, i) =>
            {
                var colors = _consoleColors[i % _consoleColors.Length];
                var processManager = new ProcessManager(x, colors.Item1, colors.Item2);
                
                processManager.OutputDataReceived += 
                    line =>  _outputCollection.Add((processManager, line, false));
                processManager.ErrorDataReceived +=
                    line =>  _outputCollection.Add((processManager, line, true));
                
                return processManager;
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

    private void StartOutputThread()
    {
        var thread = new Thread(() =>
        {
            foreach (var (processManager, line, isError) in _outputCollection.GetConsumingEnumerable())
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }
                
                Console.BackgroundColor = processManager.NameBackground;
                Console.ForegroundColor = processManager.NameForeground;
                Console.Write($"{processManager.Name.PadRight(_maxNameLength)} |");

                Console.ResetColor();
                if (isError)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                }

                Console.WriteLine($" {line}");
                if (isError)
                {
                    Console.ResetColor();
                }
            }
        });
        thread.Start();
    }
}