using System.Net.Sockets;
using Dusk.Diagnostic;
using Dusk.Network;
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
    /// <param name="client">TCP client of the connection.</param>
    /// <param name="stream">Packet stream of the connection.</param>
    public ClientConnection(TcpClient client, PacketStream stream) : base(Guid.NewGuid().ToString(), client, stream)
    {
        
    }
    
    /// <summary>
    /// Starts the client.
    /// </summary>
    public static async Task StartAsync() {
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
            return;
        }
        
        // Send the authentication request.
        var packetStream = new PacketStream(client.GetStream());
        await packetStream.SendAsync(new PacketData(PacketData.PacketType.Authentication, settings.Connection.Secret));
        
        // Start the connection.
        new ClientConnection(client, packetStream).Start();
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
        // TODO
    }
}