namespace BProMax.Configuration;

public class AppSettings
{
    public string Name { get; set; } = "b-pro-max";
    public string Version { get; set; } = "1.0.0";
    public string DataPath { get; set; } = "./data";
}

public class DatabaseSettings
{
    public string ConnectionString { get; set; } = string.Empty;
}

public class SoundSettings
{
    public bool Enabled { get; set; } = true;
    public double Volume { get; set; } = 0.7;
}

public class TimeLockSettings
{
    public bool Enabled { get; set; }
    public int StartHour { get; set; } = 9;
    public int EndHour { get; set; } = 17;
}

public class SupabaseSettings
{
    public string Url { get; set; } = string.Empty;
    public string AnonKey { get; set; } = string.Empty;
    public string ServiceRoleKey { get; set; } = string.Empty;
}
