using System.Text.Json.Serialization;

namespace Dusk.Configuration;

public class ServerConfiguration : BaseConfiguration
{
    public class DomainConfiguration
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
    
    /// <summary>
    /// Port used for the server.
    /// </summary>
    public ushort Port { get; set; } = 23594;

    /// <summary>
    /// List of domains set up for the server.
    /// </summary>
    public List<DomainConfiguration> Domains { get; set; } = new List<DomainConfiguration>() { new DomainConfiguration() };

    /// <summary>
    /// State for the configuration.
    /// </summary>
    public static ConfigurationState<ServerConfiguration> State {
        get {
            if (field == null)
            {
                field = new ConfigurationState<ServerConfiguration>("settings-server.json", new ServerConfiguration(), ServerConfigurationJsonContext.Default.ServerConfiguration);
            }
            return field!;
        }
    }
}

[JsonSerializable(typeof(ServerConfiguration))]
[JsonSourceGenerationOptions(WriteIndented = true, IncludeFields = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
internal partial class ServerConfigurationJsonContext : JsonSerializerContext
{
}