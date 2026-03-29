namespace Dusk.Configuration;

public abstract class BaseConfiguration
{
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
    /// Configuration for ping requests.
    /// </summary>
    public PingConfiguration Ping { get; set; } = new PingConfiguration();
}