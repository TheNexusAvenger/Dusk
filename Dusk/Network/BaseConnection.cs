using System.Net.Sockets;
using Dusk.Diagnostic;
using Dusk.Server;

namespace Dusk.Network;

public abstract class BaseConnection
{
    /// <summary>
    /// Id of the connection.
    /// </summary>
    public readonly string Id;
    
    /// <summary>
    /// Packet stream of the connection.
    /// </summary>
    public readonly PacketStream Stream;
    
    /// <summary>
    /// TCP client of the connection.
    /// </summary>
    private readonly TcpClient _client;

    /// <summary>
    /// Total ping requests that are missing responses.
    /// </summary>
    private int MissedPingResponses { get; set; } = 0;

    /// <summary>
    /// Creates a base connection.
    /// </summary>
    /// <param name="id">Id of the connection.</param>
    /// <param name="client">TCP client of the connection.</param>
    /// <param name="stream">Packet stream of the connection.</param>
    public BaseConnection(string id, TcpClient client, PacketStream stream)
    {
        this.Id = id;
        this.Stream = stream;
        this._client = client;
    }
    
    /// <summary>
    /// Closes the connection.
    /// </summary>
    public void Close()
    {
        if (!this.IsActive()) return;
        Logger.Info($"Connection {this.Id} closed.");
        this.Stream.Close();
        this.OnClose();
    }

    /// <summary>
    /// Tries to send a packet. Returns if it was successful.
    /// Disconnects the client if it fails.
    /// </summary>
    /// <param name="packet">Packet to send.</param>
    /// <returns>Whether the packet was sent.</returns>
    public async Task<bool> TrySendPacketAsync(PacketData packet)
    {
        try
        {
            await this.Stream.SendAsync(packet);
            return true;
        }
        catch (Exception)
        {
            this.Close();
            return false;
        }
    }
    
    /// <summary>
    /// Starts handling the connection.
    /// </summary>
    public void Start()
    {
        // Handle requests from the client.
        var receiveTask = Task.Run(async () =>
        {
            try
            {
                // Handle requests.
                while (this.IsActive())
                {
                    // Wait for the request.
                    var request = await this.Stream.ReceiveAsync();
                    Logger.Debug($"Received {request.Type} request from connection {this.Id}.");
                    
                    // Handle the request.
                    if (request.Type == PacketData.PacketType.PingSend)
                    {
                        Logger.Debug($"Sending ping response to connection {this.Id}.");
                        this.MissedPingResponses = 0;
                        await TrySendPacketAsync(new PacketData(PacketData.PacketType.PingResponse));
                    }
                    else if (request.Type != PacketData.PacketType.PingResponse)
                    {
                        await this.ProcessPacketAsync(request);
                    }
                }
            }
            catch (InvalidOperationException)
            {
                // Close the connection.
                this.Close();
            }
        });

        // Send ping requests.
        var pingTask = Task.Run(async () =>
        {
            while (this.IsActive())
            {
                // Close the connection if no ping responses were returned recently.
                var pingSettings = await this.GetPingSettingsAsync();
                if (this.MissedPingResponses >= pingSettings.MissedPingRequestsDisconnect)
                {
                    Logger.Warn($"Disconnecting client {this.Id} due to {this.MissedPingResponses} missed ping responses.");
                    this.Close();
                    break;
                }
                
                // Send the new request and wait to try again.
                Logger.Debug($"Sending ping request to connection {this.Id}.");
                this.MissedPingResponses += 1;
                await TrySendPacketAsync(new PacketData(PacketData.PacketType.PingSend));
                await Task.Delay(TimeSpan.FromSeconds(pingSettings.PingInterval));
            }
        });
        
        // Wait for one task to complete.
        // Either one finishing closes the client.
        Task.WaitAny(receiveTask, pingTask);
    }
    
    /// <summary>
    /// Returns the ping settings to use.
    /// </summary>
    /// <returns>Ping settings for the connection.</returns>
    public abstract Task<ServerSettings.PingSettings> GetPingSettingsAsync();
    
    /// <summary>
    /// Returns if the connection is active.
    /// </summary>
    /// <returns>Whether the connection is active.</returns>
    public abstract bool IsActive();

    /// <summary>
    /// Handles the connection being closed.
    /// </summary>
    public abstract void OnClose();
    
    /// <summary>
    /// Process a packet.
    /// </summary>
    /// <param name="packet">Packet to process.</param>
    public abstract Task ProcessPacketAsync(PacketData packet);
}