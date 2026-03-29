using System.Text.Json.Serialization;

namespace Dusk.Configuration;

public class ClientConfiguration : BaseConfiguration
{
    public class ConnectionConfiguration
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
    /// Configuration for client connections.
    /// </summary>
    public ConnectionConfiguration Connection { get; set; } = new ConnectionConfiguration();

    /// <summary>
    /// State for the configuration.
    /// </summary>
    public static ConfigurationState<ClientConfiguration> State {
        get {
            if (field == null)
            {
                field = new ConfigurationState<ClientConfiguration>("settings-client.json", new ClientConfiguration(), ClientConfigurationJsonContext.Default.ClientConfiguration);
            }
            return field!;
        }
    }
}

[JsonSerializable(typeof(ClientConfiguration))]
[JsonSourceGenerationOptions(WriteIndented = true, IncludeFields = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
internal partial class ClientConfigurationJsonContext : JsonSerializerContext
{
}