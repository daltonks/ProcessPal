namespace ProcessPal;

public class Config : Dictionary<string, ProcessGroupConfig>
{
    
}

public class ProcessGroupConfig
{
    public int Port { get; set; }
    public ScriptConfig[] Scripts { get; set; }
}

public class ScriptConfig : IScriptConfig
{
    public string Name { get; set; }
    public string EnvPath { get; set; }
    public Dictionary<string, string> Env { get; set; }
    public string WorkDir { get; set; }
    public string Script { get; set; }
    public string Path { get; set; }
    public string Args { get; set; }
    public bool AutoRestart { get; set; }
    public ShutdownScriptConfig CleanupScript { get; set; }
}

public class ShutdownScriptConfig : IScriptConfig
{
    public string EnvPath { get; set; }
    public Dictionary<string, string> Env { get; set; }
    public string Script { get; set; }
    public string Path { get; set; }
    public string Args { get; set; }
}

public interface IScriptConfig
{
    public string EnvPath { get; set; }
    public Dictionary<string, string> Env { get; set; }
    public string Script { get; set; }
    public string Path { get; set; }
    public string Args { get; set; }
}