namespace ProcessPal;

public class Config : Dictionary<string, ProcessGroupConfig>
{
    
}

public class ProcessGroupConfig
{
    public int Port { get; set; }
    public ScriptConfig[] Scripts { get; set; }
}

public class ScriptConfig
{
    public string Name { get; set; }
    public string Path { get; set; }
    public string Args { get; set; }
    public bool AutoRestart { get; set; }
}