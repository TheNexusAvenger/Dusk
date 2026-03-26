using System.Text.Json;
using System.Text.Json.Serialization;

namespace Dusk.Server;

public class ServerSettings
{
    public class DomainSettings
    {
        /// <summary>
        /// Name of the domain (mainly used for logging).
        /// </summary>
        public string Name { get; set; } = "default";
        
        /// <summary>
        /// Secret key used to authenticate clients.
        /// </summary>
        public string Secret { get; set; } = "default";
    }

    public class PingSettings
    {
        /// <summary>
        /// Interval for sending ping requests in seconds.
        /// </summary>
        public int PingInterval { get; set; } = 30;

        /// <summary>
        /// Number of pings require being missed to disconnect.
        /// </summary>
        public int MissedPingRequestsDisconnect = 3;
    }
    
    /// <summary>
    /// Port used for the server.
    /// </summary>
    public ushort Port { get; set; } = 23594;

    /// <summary>
    /// List of domains set up for the server.
    /// </summary>
    public List<DomainSettings> Domains { get; set; } = new List<DomainSettings>() { new DomainSettings() };

    /// <summary>
    /// Settings for ping requests.
    /// </summary>
    public PingSettings Ping { get; set; } = new PingSettings();
    
    /// <summary>
    /// Determines the directory of the settings for Dusk.
    /// The directory will be created if it doesn't exist.
    /// </summary>
    /// <returns>Path of the settings directory.</returns>
    public static string GetSettingsDirectory()
    {
        var settingsDirectory = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Dusk");
        if (!Directory.Exists(settingsDirectory))
        {
            Directory.CreateDirectory(settingsDirectory);
        }
        return settingsDirectory;
    }

    /// <summary>
    /// Returns the current server settings.
    /// If the settings don't exist, they will be created.
    /// Settings are read every time from the disk.
    /// </summary>
    /// <returns>The current server settings.</returns>
    public static async Task<ServerSettings> GetSettingsAsync()
    {
        // Create the settings if they don't exist.
        var settingsPath = Path.Combine(GetSettingsDirectory(), "settings-server.json");
        if (!File.Exists(settingsPath))
        {
            await File.WriteAllTextAsync(settingsPath, JsonSerializer.Serialize(new ServerSettings(), ServerSettingsJsonContext.Default.ServerSettings));
        }
        
        // Read and return the settings.
        return JsonSerializer.Deserialize<ServerSettings>(await File.ReadAllTextAsync(settingsPath))!;
    }
}

[JsonSerializable(typeof(ServerSettings))]
[JsonSourceGenerationOptions(WriteIndented = true, IncludeFields = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
internal partial class ServerSettingsJsonContext : JsonSerializerContext
{
}