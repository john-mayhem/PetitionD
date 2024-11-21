// File: Configuration/AppSettings.cs
namespace PetitionD.Configuration;

public class AppSettings
{
    public int GmServicePort { get; set; } = 3109;
    public int WorldServicePort { get; set; } = 3107;
    public int NoticeServicePort { get; set; } = 3121;
    public string NoticeServiceAllowIpList { get; set; } = "192.168.0.2";
    public string AuthIp { get; set; } = "192.168.0.2";
    public int AuthPort { get; set; } = 2108;
    public int AuthConnCount { get; set; } = 3;
    public string LogDirectory { get; set; } = "log";
    public bool EnableQuota { get; set; } = false;
    public int MaxQuota { get; set; } = 5;
    public string RunMode { get; set; } = "Normal";
    public bool DumpPacket { get; set; } = true;
    public int ServerStatusRefreshInterval { get; set; } = 30;
    public string DatabaseConnString { get; set; } = "";
    public string DatabaseConnName { get; set; } = "petition";
    public int DatabaseConnCount { get; set; } = 10;
    public int DatabaseConnTimeout { get; set; } = 0;
    public int MaxActivePetition { get; set; } = 100000;
    public int MinimumGmClientBuildNumber { get; set; } = 0;
    public bool EnableGmStatusDump { get; set; } = true;
    public bool EnableAssignment { get; set; } = true;
    public int MaxAssignmentPerGm { get; set; } = 10;
    public bool EnableOnlineCheck { get; set; } = true;
    public int ServerBuildNumber { get; set; } = 20011;
}