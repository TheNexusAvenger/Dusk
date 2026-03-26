using System.Net.Sockets;
using Dusk.Network;
using Dusk.Server.Model;

namespace Dusk.Server.Network;

public class ServerDomainConnection : BaseConnection
{
    /// <summary>
    /// Server domain of the connection.
    /// </summary>
    public readonly ServerDomain ServerDomain;

    /// <summary>
    /// Creates a server domain connection.
    /// </summary>
    /// <param name="serverDomain">Parent server domain of the connection.</param>
    /// <param name="id">Id of the connection.</param>
    /// <param name="client">TCP client of the connection.</param>
    /// <param name="stream">Packet stream of the connection.</param>
    public ServerDomainConnection(ServerDomain serverDomain, string id, TcpClient client, PacketStream stream) : base(id, client, stream)
    {
        this.ServerDomain = serverDomain;
    }

    /// <summary>
    /// Returns the ping settings to use.
    /// </summary>
    /// <returns>Ping settings for the connection.</returns>
    public override async Task<ServerSettings.PingSettings> GetPingSettingsAsync()
    {
        return (await ServerSettings.GetSettingsAsync()).Ping;
    }

    /// <summary>
    /// Returns if the connection is active.
    /// </summary>
    /// <returns>Whether the connection is active.</returns>
    public override bool IsActive()
    {
        return this.ServerDomain.Connections.ContainsKey(this.Id);
    }
    
    /// <summary>
    /// Handles the connection being closed.
    /// </summary>
    public override void OnClose()
    {
        this.ServerDomain.Connections.Remove(this.Id);
    }
}