using Dusk.Network;

namespace Dusk.Test.Network;

public class PacketStreamTest
{
    private MemoryStream _stream;
    private PacketStream _packetStream;

    [SetUp]
    public void SetUp()
    {
        this._stream = new MemoryStream();
        this._packetStream = new PacketStream(this._stream);
    }

    [Test]
    public void TestReceiveAsync()
    {
        this._packetStream.SendAsync(new PacketData(PacketData.PacketType.PingSend, "TestData")).Wait();
        this._stream.Position = 0;
        
        var response = this._packetStream.ReceiveAsync().Result;
        Assert.That(response.Type, Is.EqualTo(PacketData.PacketType.PingSend));
        Assert.That(response.Payload, Is.EqualTo("TestData"));
    }

    [Test]
    public void TestReceiveAsyncFixedLength()
    {
        this._packetStream.SendAsync(new PacketData(PacketData.PacketType.PingSend, "TestData")).Wait();
        this._stream.Position = 0;
        
        var response = this._packetStream.ReceiveAsync(4).Result;
        Assert.That(response.Type, Is.EqualTo(PacketData.PacketType.PingSend));
        Assert.That(response.Payload, Is.EqualTo("Test"));
    }
}