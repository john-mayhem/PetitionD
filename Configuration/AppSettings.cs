namespace PetitionD.Configuration;

public class AppSettings
{
    // Server Settings
    public const int SERVER_BUILD_NUMBER = 20011;
    public int MinimumGmClientBuildNumber { get; set; } = 20011;

    // Network Settings
    public int GmServicePort { get; set; } = 3109;
    public int WorldServicePort { get; set; } = 3107;
    public int NoticeServicePort { get; set; } = 3121;
    public string NoticeServiceAllowIpList { get; set; } = "192.168.0.2";

    // Auth Settings
    public string AuthIp { get; set; } = "192.168.0.2";
    public int AuthPort { get; set; } = 2108;
    public int AuthConnCount { get; set; } = 3;

    // Database Settings
    public string DatabaseConnString { get; set; } = "";
    public string DatabaseConnName { get; set; } = "petition";
    public int DatabaseConnCount { get; set; } = 10;
    public int DatabaseConnTimeout { get; set; } = 0;

    // Logging Settings
    public string LogDirectory { get; set; } = "log";
    public bool DumpPacket { get; set; } = true;

    // Service Settings
    public int ServerStatusRefreshInterval { get; set; } = 30;
    public string RunMode { get; set; } = "Normal";

    // Petition Settings
    public bool EnableQuota { get; set; } = false;
    public const int MAX_QUOTA = 5;
    public const int MAX_ACTIVE_PETITION = 100000;

    // GM Settings
    public bool EnableGmStatusDump { get; set; } = true;
    public bool EnableAssignment { get; set; } = true;
    public int MaxAssignmentPerGm { get; set; } = 10;
    public bool EnableOnlineCheck { get; set; } = true;

    // Timer Settings
    public const int ONLINE_CHECK_TIMER_INTERVAL = 5000;
    public const int ONLINE_CHECK_INTERVAL_PER_WORLD = 60000;
    public const int FEEDBACK_CHECK_TIMER_INTERVAL = 600000;
    public const int MAX_FEEDBACK_VALID_TIME = 1800000;
    public const int COMMAND_TIMEOUT = 0;

    public bool IsTestMode => RunMode.Equals("Test", StringComparison.OrdinalIgnoreCase);
}