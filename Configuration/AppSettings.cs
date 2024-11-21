// File: Configuration/AppSettings.cs
namespace PetitionD.Configuration;

public class AppSettings
{
    public int ServerBuildNumber { get; set; } = 20011;
    public int MaxQuota { get; set; } = 5;
    public int MaxActivePetition { get; set; } = 100000;
    public int MaxAssignmentPerGm { get; set; } = 10;
    public bool EnableAssignment { get; set; } = true;
    public bool EnableQuota { get; set; } = true;
    public string AuthIp { get; set; } = string.Empty;
    public int AuthPort { get; set; }
    public int AuthConnCount { get; set; }
    public string LogDirectory { get; set; } = string.Empty;
    public int ServerStatusRefreshInterval { get; set; }
    public string DatabaseConnString { get; set; } = string.Empty;
    public string DatabaseConnName { get; set; } = string.Empty;
    public int DatabaseConnCount { get; set; }
    public int DatabaseConnTimeout { get; set; }
    public int MinimumGmClientBuildNumber { get; set; }
    public bool EnableGmStatusDump { get; set; }
    public bool EnableOnlineCheck { get; set; }
}