using System.Net.Sockets;
using Dusk.Clipboard;
using Dusk.Diagnostic;
using Dusk.Network;
using Dusk.Network.Packet;
using Dusk.Server;

namespace Dusk.Client;

public class ClientConnection : BaseConnection
{
    /// <summary>
    /// Whether the connection is active.
    /// </summary>
    private bool _active = true;
    
    /// <summary>
    /// Creates a client connection.
    /// </summary>
    /// <param name="id">Id of the connection.</param>
    /// <param name="client">TCP client of the connection.</param>
    /// <param name="stream">Packet stream of the connection.</param>
    public ClientConnection(string id, TcpClient client, PacketStream stream) : base(Guid.NewGuid().ToString(), client, stream)
    {
        
    }

    /// <summary>
    /// Connects a client.
    /// </summary>
    public static async Task<ClientConnection> ConnectAsync()
    {
        // Open the connection.
        var settings = await ClientSettings.GetSettingsAsync();
        Logger.Info($"Starting connection to {settings.Connection.Host}:{settings.Connection}");
        TcpClient? client = null;
        try
        {
            client = new TcpClient(settings.Connection.Host, settings.Connection.Port);
        }
        catch (Exception e)
        {
            Logger.Error($"Failed to connect: {e.Message}");
            throw;
        }
        
        // Send the authentication request.
        var packetStream = new PacketStream(client.GetStream());
        await packetStream.SendAsync(new PacketData(PacketData.PacketType.Authentication, settings.Connection.Secret));
        
        // Wait for the connection id.
        var connectionIdResponse = await packetStream.ReceiveAsync();
        if (connectionIdResponse.Type != PacketData.PacketType.Authenticated)
        {
            Logger.Error($"Received unexpected packet type {connectionIdResponse.Type}.");
            throw new InvalidDataException($"Received unexpected packet type {connectionIdResponse.Type}.");
        }
        var connectionId = AuthenticatedPacket.FromPacket(connectionIdResponse).ConnectionId;
        Logger.Debug($"Connected as connection {connectionId}.");
        
        // Return the client.
        return new ClientConnection(connectionId, client, packetStream);
    }
    
    /// <summary>
    /// Returns the ping settings to use.
    /// </summary>
    /// <returns>Ping settings for the connection.</returns>
    public override async Task<ServerSettings.PingSettings> GetPingSettingsAsync()
    {
        return (await ClientSettings.GetSettingsAsync()).Ping;
    }

    /// <summary>
    /// Returns if the connection is active.
    /// </summary>
    /// <returns>Whether the connection is active.</returns>
    public override bool IsActive()
    {
        return this._active;
    }
    
    /// <summary>
    /// Handles the connection being closed.
    /// </summary>
    public override void OnClose()
    {
        this._active = false;
    }

    /// <summary>
    /// Process a packet.
    /// </summary>
    /// <param name="packet">Packet to process.</param>
    public override async Task ProcessPacketAsync(PacketData packet)
    {
        if (packet.Type == PacketData.PacketType.UpdateClipboard)
        {
            // Update the clipboard.
            var updateClipboardPacket = UpdateClipboardPacket.FromPacket(packet);
            Logger.Info($"Updating clipboard with MIME type {updateClipboardPacket.MimeType}.");
            await IClipboard.GetClipboard().WriteClipboardAsync(new ClipboardData()
            {
                MimeType = updateClipboardPacket.MimeType,
                Data = updateClipboardPacket.Data,
            });
        }
        else
        {
            // Warn that the packet has no handler.
            Logger.Warn($"No packet processor for {packet.Type}.");
        }
    }
}