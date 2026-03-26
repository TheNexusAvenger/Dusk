using System.Text.Json;
using System.Text.Json.Serialization;
using Dusk.Server;

namespace Dusk.Client;

public class ClientSettings
{
    public class ConnectionSettings
    {
        /// <summary>
        /// Host port used for the server.
        /// </summary>
        public string Host { get; set; } = "localhost";
    
        /// <summary>
        /// Host port used for the server.
        /// </summary>
        public ushort Port { get; set; } = 23594;
        
        /// <summary>
        /// Secret key used to authenticate clients.
        /// </summary>
        public string Secret { get; set; } = "default";
    }
    
    /// <summary>
    /// Settings for client connections.
    /// </summary>
    public ConnectionSettings Connection { get; set; } = new ConnectionSettings();

    /// <summary>
    /// Settings for ping requests.
    /// </summary>
    public ServerSettings.PingSettings Ping { get; set; } = new ServerSettings.PingSettings();
    
    /// <summary>
    /// Returns the current client settings.
    /// If the settings don't exist, they will be created.
    /// Settings are read every time from the disk.
    /// </summary>
    /// <returns>The current server settings.</returns>
    public static async Task<ClientSettings> GetSettingsAsync()
    {
        // Create the settings if they don't exist.
        var settingsPath = Path.Combine(ServerSettings.GetSettingsDirectory(), "settings-client.json");
        if (!File.Exists(settingsPath))
        {
            await File.WriteAllTextAsync(settingsPath, JsonSerializer.Serialize(new ClientSettings(), ClientSettingsJsonContext.Default.ClientSettings));
        }
        
        // Read and return the settings.
        return JsonSerializer.Deserialize<ClientSettings>(await File.ReadAllTextAsync(settingsPath))!;
    }
}

[JsonSerializable(typeof(ClientSettings))]
[JsonSourceGenerationOptions(WriteIndented = true, IncludeFields = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
internal partial class ClientSettingsJsonContext : JsonSerializerContext
{
}