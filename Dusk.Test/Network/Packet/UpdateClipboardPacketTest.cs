using System.Text;
using Dusk.Network.Packet;

namespace Dusk.Test.Network.Packet;

public class UpdateClipboardPacketTest
{
    [Test]
    public void TestToPacketDataFromPacket()
    {
        var packet = new UpdateClipboardPacket()
        {
            MimeType = "text/plain",
            Data = Encoding.UTF8.GetBytes("Test string"),
        };
        
        var newPacket = UpdateClipboardPacket.FromPacket(packet.ToPacketData());
        Assert.That(newPacket.MimeType, Is.EqualTo("text/plain"));
        Assert.That(newPacket.Data, Is.EqualTo(Encoding.UTF8.GetBytes("Test string")));
    }
}