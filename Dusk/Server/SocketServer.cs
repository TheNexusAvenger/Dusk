using System.Net;
using System.Net.Sockets;
using Dusk.Diagnostic;
using Dusk.Network;
using Dusk.Network.Packet;
using Dusk.Server.Model;
using Dusk.Server.Network;

namespace Dusk.Server;

public class SocketServer
{
    /// <summary>
    /// Listener for accepting traffic forwarders.
    /// </summary>
    private readonly TcpListener _listener;
    
    /// <summary>
    /// Domains of the connection.
    /// </summary>
    private readonly Dictionary<string, ServerDomain> _domains = new Dictionary<string, ServerDomain>();
    
    /// <summary>
    /// Creates the server.
    /// </summary>
    public SocketServer()
    {
        var settings = ServerSettings.GetSettingsAsync().Result;
        Logger.Info($"Accepting connections on port {settings.Port}.");
        this._listener = new TcpListener(IPAddress.Any, settings.Port);
    }
        
    /// <summary>
    /// Starts listening on the server.
    /// </summary>
    public async Task StartAsync()
    {
        // Start the listener.
        this._listener.Start();
            
        // Start accepting connections.
        while (true)
        {
            // Listen for a connection.
            var connection = await this._listener.AcceptTcpClientAsync();
                
            // Start the connection.
            var _ = Task.Run(async () =>
            {
                await this.ProcessConnectionAsync(connection);
            });
        }
    }

    /// <summary>
    /// Starts processing a connection.
    /// </summary>
    /// <param name="client">Client that is attempting to set up traffic forwarding.</param>
    private async Task ProcessConnectionAsync(TcpClient client)
    {
        // Read the secret from the client.
        var connectionId = Guid.NewGuid().ToString();
        Logger.Debug($"Accepting new connection {connectionId}.");
        var settings = await ServerSettings.GetSettingsAsync();
        var stream = client.GetStream();
        var packetStream = new PacketStream(stream);
        var maxSecretSize = settings.Domains.Max(domain => domain.Secret.Length);
        var authenticationPacket = await packetStream.ReceiveAsync(maxSecretSize);
        if (authenticationPacket.Type != PacketData.PacketType.Authentication)
        {
            Logger.Debug($"Disconnecting {connectionId} due to wrong packet type.");
            client.Close();
            return;
        }
        
        // Disconnect the client if the secret doesn't match any domain.
        var domain = settings.Domains.FirstOrDefault(domain => domain.Secret == authenticationPacket.StringPayload);
        if (domain == null)
        {
            Logger.Warn($"Disconnecting {connectionId} due to incorrect secret.");
            client.Close();
            return;
        }
        Logger.Debug($"Client {connectionId} connected to domain {domain.Name}.");
        
        // Create the domain if it doesn't exist.
        if (!this._domains.ContainsKey(domain.Name))
        {
            this._domains[domain.Name] = new ServerDomain()
            {
                Name = domain.Name,
            };
        }
        
        // Store the connection.
        var serverDomain = this._domains[domain.Name];
        var serverDomainConnection = new ServerDomainConnection(serverDomain, connectionId, client, packetStream);
        serverDomain.Connections[connectionId] = serverDomainConnection;
        
        // Send the connection id.
        await packetStream.SendAsync(new AuthenticatedPacket()
        {
            ConnectionId = connectionId,
        }.ToPacketData());
        
        // Start sending pin requests and listening for packets.
        serverDomainConnection.Start();
    }
}