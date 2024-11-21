// File: Configuration/ConfigurationManager.cs
namespace PetitionD.Configuration;

public static class ConfigurationManager
{
    public static AppSettings LoadConfiguration()
    {
        var settings = new AppSettings
        {
            GmServicePort = GetAppSetting("GmServicePort", 3109),
            WorldServicePort = GetAppSetting("WorldServicePort", 3107),
            NoticeServicePort = GetAppSetting("NoticeServicePort", 3121),
            NoticeServiceAllowIpList = GetAppSetting("NoticeServiceAllowIpList", "192.168.0.2"),
            AuthIp = GetAppSetting("AuthIp", "192.168.0.2"),
            AuthPort = GetAppSetting("AuthPort", 2108),
            AuthConnCount = GetAppSetting("AuthConnCount", 3),
            LogDirectory = GetAppSetting("LogDirectory", "log"),
            EnableQuota = GetAppSetting("EnableQuota", false),
            MaxQuota = GetAppSetting("MaxQuota", 5),
            RunMode = GetAppSetting("RunMode", "Normal"),
            DumpPacket = GetAppSetting("DumpPacket", true),
            ServerStatusRefreshInterval = GetAppSetting("ServerStatusRefreshInterval", 30),
            DatabaseConnString = GetAppSetting("DatabaseConnString", string.Empty),
            DatabaseConnName = GetAppSetting("DatabaseConnName", "petition"),
            DatabaseConnCount = GetAppSetting("DatabaseConnCount", 10),
            DatabaseConnTimeout = GetAppSetting("DatabaseConnTimeout", 0),
            MaxActivePetition = GetAppSetting("MaxActivePetition", 100000),
            MinimumGmClientBuildNumber = GetAppSetting("MinimumGmClientBuildNumber", 0),
            EnableGmStatusDump = GetAppSetting("EnableGmStatusDump", true),
            EnableAssignment = GetAppSetting("EnableAssignment", true),
            MaxAssignmentPerGm = GetAppSetting("MaxAssignmentPerGm", 10),
            EnableOnlineCheck = GetAppSetting("EnableOnlineCheck", true)
        };

        return settings;
    }

    private static T GetAppSetting<T>(string key, T defaultValue)
    {
        var value = System.Configuration.ConfigurationManager.AppSettings[key];
        if (string.IsNullOrEmpty(value)) return defaultValue;

        try
        {
            return (T)Convert.ChangeType(value, typeof(T));
        }
        catch
        {
            return defaultValue;
        }
    }
}