namespace ProcessPal;

public class Config
{
    public int Port { get; set; }
    public Dictionary<string, ProcessConfig[]> ProcessGroups { get; set; }
}

public class ProcessConfig
{
    public string ScriptPath { get; set; }
    public string Args { get; set; }
    public bool AutoRestart { get; set; }
}