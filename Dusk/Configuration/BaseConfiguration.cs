using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace Dusk.Configuration;

public abstract class BaseConfiguration
{
    public class LoggingConfiguration
    {
        /// <summary>
        /// Minimum log level to show in the logs.
        /// </summary>
        [JsonConverter(typeof(JsonStringEnumConverter<LogLevel>))]
        public LogLevel MinimumLogLevel { get; set; } = LogLevel.Information;
    }
    
    public class PingConfiguration
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
    /// Configuration for logging.
    /// </summary>
    public LoggingConfiguration Logging { get; set; } = new LoggingConfiguration();
    
    /// <summary>
    /// Configuration for ping requests.
    /// </summary>
    public PingConfiguration Ping { get; set; } = new PingConfiguration();
}