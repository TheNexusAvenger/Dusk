using System.Text;

namespace Dusk.Network.Packet;

public class AuthenticatedPacket
{
    /// <summary>
    /// Id of the client on the server.
    /// </summary>
    public string ConnectionId { get; set; } = null!;
    
    /// <summary>
    /// Converts PacketData to a packet.
    /// </summary>
    /// <param name="packet">PacketData to convert from.</param>
    /// <returns>Converted packet.</returns>
    public static AuthenticatedPacket FromPacket(PacketData packet)
    {
        using var reader = new BinaryReader(new MemoryStream(packet.Payload), Encoding.UTF8, false);
        var connectionId = reader.ReadString();
        return new AuthenticatedPacket()
        {
            ConnectionId = connectionId,
        };
    }

    /// <summary>
    /// Converts the packet to PacketData.
    /// </summary>
    /// <returns>PacketData to send.</returns>
    public PacketData ToPacketData()
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.UTF8, false);
        writer.Write(this.ConnectionId);
        return new PacketData(PacketData.PacketType.Authenticated, stream.ToArray());
    }
}