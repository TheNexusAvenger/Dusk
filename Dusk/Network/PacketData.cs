using System.Text;

namespace Dusk.Network;

public class PacketData
{
    public enum PacketType
    {
        Authentication,
        PingSend,
        PingResponse,
        UpdateClipboard,
    }
    
    /// <summary>
    /// Type of the packet.
    /// </summary>
    public PacketType Type { get; }
        
    /// <summary>
    /// Payload of the packet.
    /// </summary>
    public byte[] Payload { get; }
    
    /// <summary>
    /// Payload of the packet a string.
    /// </summary>
    public string StringPayload => Encoding.UTF8.GetString(this.Payload);

    /// <summary>
    /// Creates a packet.
    /// </summary>
    /// <param name="type">Type of the packet.</param>
    public PacketData(PacketType type)
    {
        this.Type = type;
        this.Payload = Array.Empty<byte>();
    }
        
    /// <summary>
    /// Creates a packet.
    /// </summary>
    /// <param name="type">Type of the packet.</param>
    /// <param name="data">Payload of the packet.</param>
    public PacketData(PacketType type, string data)
    {
        this.Type = type;
        this.Payload = Encoding.UTF8.GetBytes(data);
    }
        
    /// <summary>
    /// Creates a packet.
    /// </summary>
    /// <param name="type">Type of the packet.</param>
    /// <param name="data">Payload of the packet.</param>
    public PacketData(PacketType type, byte[] data)
    {
        this.Type = type;
        this.Payload = data;
    }
}