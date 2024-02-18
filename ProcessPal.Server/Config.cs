namespace ProcessPal.Server;

public class Config
{
    public int Port { get; set; }
    public ProcessGroupConfig[] ProcessGroups { get; set; }
}

public class ProcessGroupConfig
{
    public string Name { get; set; }
    public ProcessConfig[] Processes { get; set; }
}

public class ProcessConfig
{
    public string FileName { get; set; }
    public string Args { get; set; }
    public bool RestartOnExit { get; set; }
}