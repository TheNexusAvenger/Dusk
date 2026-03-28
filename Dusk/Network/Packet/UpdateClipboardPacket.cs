using System.Text;

namespace Dusk.Network.Packet;

public class UpdateClipboardPacket
{
    /// <summary>
    /// Source connection id of the clipboard update.
    /// </summary>
    public string SourceConnectionId { get; set; } = null!;
    
    /// <summary>
    /// MIME type of the clipboard.
    /// </summary>
    public string MimeType { get; set; } = null!;

    /// <summary>
    /// Data of the clipboard.
    /// </summary>
    public byte[] Data { get; set; } = null!;

    /// <summary>
    /// Converts PacketData to a packet.
    /// </summary>
    /// <param name="packet">PacketData to convert from.</param>
    /// <returns>Converted packet.</returns>
    public static UpdateClipboardPacket FromPacket(PacketData packet)
    {
        using var reader = new BinaryReader(new MemoryStream(packet.Payload), Encoding.UTF8, false);
        var sourceConnectionId = reader.ReadString();
        var mimeType = reader.ReadString();
        var length = reader.ReadInt32();
        var data = reader.ReadBytes(length);
        return new UpdateClipboardPacket()
        {
            SourceConnectionId = sourceConnectionId,
            MimeType = mimeType,
            Data = data,
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
        writer.Write(this.SourceConnectionId);
        writer.Write(this.MimeType);
        writer.Write(this.Data.Length);
        writer.Write(this.Data);
        return new PacketData(PacketData.PacketType.UpdateClipboard, stream.ToArray());
    }
}