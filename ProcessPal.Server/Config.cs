namespace ProcessPal.Server;

public class Config
{
    public int Port { get; set; }
    public Dictionary<string, ProcessConfig[]> ProcessGroups { get; set; }
}

public class ProcessConfig
{
    public string FileName { get; set; }
    public string Args { get; set; }
    public bool RestartOnExit { get; set; }
}