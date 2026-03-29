using System.IO.Pipes;
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
    /// Creates a client connection.
    /// </summary>
    /// <param name="id">Id of the connection.</param>
    /// <param name="client">TCP client of the connection.</param>
    /// <param name="stream">Packet stream of the connection.</param>
    public ClientConnection(string id, TcpClient client, PacketStream stream) : base(id, client, stream)
    {
        
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
    
    /// <summary>
    /// Runs the pipe server for inter-process communication (mainly for Linux).
    /// </summary>
    public async Task RunPipeServerAsync()
    {
        // Listen for requests to send the clipboard.
        while (this.IsActive())
        {
            try
            {
                // Start the pipe server.
                await using var pipeServer = new NamedPipeServerStream("DuskClient", PipeDirection.In);
                Logger.Debug("Started pipe server.");
                using var streamReader = new StreamReader(pipeServer);
                
                // Wait for the connection.
                await pipeServer.WaitForConnectionAsync();
                var request = await streamReader.ReadLineAsync();
                if (request == null) break;
                if (request == "UpdateClipboard")
                {
                    // Send the updated clipboard.
                    var clipboard = IClipboard.GetClipboard();
                    var clipboardData = await clipboard.ReadClipboardAsync();
                    if (clipboardData != null)
                    {
                        await this.TrySendPacketAsync(new UpdateClipboardPacket()
                        {
                            MimeType = clipboardData.MimeType,
                            Data = clipboardData.Data,
                        }.ToPacketData());
                    }
                    else
                    {
                        Logger.Error("Clipboard read failed.");
                    }
                }
                else
                {
                    // Warn if there is no handler.
                    Logger.Warn($"No pipe handler for \"{request}\".");
                }
            }
            catch (IOException e)
            {
                // Pipe closed.
            }
        }
    }
}