using System.Net.Sockets;
using Dusk.Clipboard;
using Dusk.Configuration;
using Dusk.Diagnostic;
using Dusk.Network;
using Dusk.Network.Packet;

namespace Dusk.Client;

public class ClientConnection : BaseConnection
{
    /// <summary>
    /// Whether the connection is active.
    /// </summary>
    private bool _active = true;
    
    /// <summary>
    /// Last clipboard data that was sent.
    /// </summary>
    private ClipboardData? _lastSentClipboardData;
    
    /// <summary>
    /// Creates a client connection.
    /// </summary>
    /// <param name="id">Id of the connection.</param>
    /// <param name="client">TCP client of the connection.</param>
    /// <param name="stream">Packet stream of the connection.</param>
    public ClientConnection(string id, TcpClient client, PacketStream stream) : base(id, client, stream)
    {
        // Load the current clipboard data.
        this._lastSentClipboardData = IClipboard.GetClipboard().ReadClipboardAsync().Result;
    }

    /// <summary>
    /// Connects a client.
    /// </summary>
    /// <param name="authenticationType">Type of the authentication to connect with.</param>
    public static async Task<ClientConnection> ConnectAsync(PacketData.PacketType authenticationType = PacketData.PacketType.Authentication)
    {
        // Open the connection.
        var configuration = ClientConfiguration.State.CurrentConfiguration!;
        Logger.Info($"Starting connection to {configuration.Connection.Host}:{configuration.Connection}");
        TcpClient? client = null;
        try
        {
            client = new TcpClient(configuration.Connection.Host, configuration.Connection.Port);
        }
        catch (Exception e)
        {
            Logger.Error($"Failed to connect: {e.Message}");
            throw;
        }
        
        // Send the authentication request.
        var packetStream = new PacketStream(client.GetStream());
        await packetStream.SendAsync(new PacketData(authenticationType, configuration.Connection.Secret));
        
        // Wait for the connection id.
        var connectionIdResponse = await packetStream.ReceiveAsync();
        if (connectionIdResponse.Type != PacketData.PacketType.Authenticated)
        {
            Logger.Error($"Received unexpected packet type {connectionIdResponse.Type}.");
            throw new InvalidDataException($"Received unexpected packet type {connectionIdResponse.Type}.");
        }
        var connectionId = AuthenticatedPacket.FromPacket(connectionIdResponse).ConnectionId;
        Logger.Info($"Connected as connection {connectionId}.");
        
        // Return the client.
        return new ClientConnection(connectionId, client, packetStream);
    }

    /// <summary>
    /// Returns the ping configuration to use.
    /// </summary>
    /// <returns>Ping configuration for the connection.</returns>
    public override BaseConfiguration.PingConfiguration GetPingConfiguration()
    {
        return ClientConfiguration.State.CurrentConfiguration!.Ping;
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
            var newClipboardData = new ClipboardData()
            {
                MimeType = updateClipboardPacket.MimeType,
                Data = updateClipboardPacket.Data,
            };
            this._lastSentClipboardData = newClipboardData;
            await IClipboard.GetClipboard().WriteClipboardAsync(newClipboardData);
        }
        else
        {
            // Warn that the packet has no handler.
            Logger.Warn($"No packet processor for {packet.Type}.");
        }
    }

    /// <summary>
    /// Sends the clipboard to the server if the clipboard changed.
    /// </summary>
    public async Task SendClipboardAsync()
    {
        // Return if the clipboard data is the same.
        var lastClipboard = this._lastSentClipboardData;
        var currentClipboard = await IClipboard.GetClipboard().ReadClipboardAsync();
        if (currentClipboard == null)
        {
            Logger.Error("Clipboard read failed.");
            return;
        }
        if (lastClipboard != null && lastClipboard.MimeType == currentClipboard.MimeType && currentClipboard.Data.SequenceEqual(lastClipboard.Data)) return;
        this._lastSentClipboardData = currentClipboard;
        
        // Send the clipboard.
        await this.TrySendPacketAsync(new UpdateClipboardPacket()
        {
            MimeType = currentClipboard.MimeType,
            Data = currentClipboard.Data,
        }.ToPacketData());
    }
}