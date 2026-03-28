using System.Net.Sockets;
using Dusk.Diagnostic;
using Dusk.Network;
using Dusk.Network.Packet;
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

    /// <summary>
    /// Process a packet.
    /// </summary>
    /// <param name="packet">Packet to process.</param>
    public override async Task ProcessPacketAsync(PacketData packet)
    {
        if (packet.Type == PacketData.PacketType.UpdateClipboard)
        {
            // Warn if the source connection id doesn't exist.
            var updateClipboardPacket = UpdateClipboardPacket.FromPacket(packet);
            var sourceConnectionId = updateClipboardPacket.SourceConnectionId;
            Logger.Info($"Replicating clipboard in domain {this.ServerDomain.Name} from connection {updateClipboardPacket.SourceConnectionId}.");
            if (!this.ServerDomain.Connections.ContainsKey(sourceConnectionId))
            {
                Logger.Warn($"Connection {sourceConnectionId} sent an updated clipboard for {this.ServerDomain.Name}, but the connection does not exist. This might cause clipboard setting loopback.");
            }
            
            // Replicate the clipboard to the other clients.
            var replicationTasks = new List<Task>();
            foreach (var connection in this.ServerDomain.Connections.Values)
            {
                if (connection.Id == sourceConnectionId) continue;
                replicationTasks.Add(connection.TrySendPacketAsync(packet));
            }
            await Task.WhenAll(replicationTasks);
        }
        else
        {
            // Warn that the packet has no handler.
            Logger.Warn($"No packet processor for {packet.Type}.");
        }
    }
}