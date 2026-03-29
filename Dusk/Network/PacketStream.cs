namespace Dusk.Network;

public class PacketStream
{
    /// <summary>
    /// Stream to read and write data to.
    /// </summary>
    private readonly Stream _stream;
    
    /// <summary>
    /// Semaphore for the stream to prevent threads writing to the stream concurrently.
    /// </summary>
    private readonly SemaphoreSlim _streamSemaphore = new SemaphoreSlim(1, 1);
    
    /// <summary>
    /// Creates the packet stream.
    /// </summary>
    /// <param name="stream">Base stream to read data from, such as a network stream.</param>
    public PacketStream(Stream stream)
    {
        this._stream = stream;
    }
    
    /// <summary>
    /// Sends a message.
    /// </summary>
    /// <param name="message">Message packet to send.</param>
    public async Task SendAsync(PacketData message)
    {
        await this._streamSemaphore.WaitAsync();
        await this._stream.WriteAsync(BitConverter.GetBytes((uint) message.Payload.Length + 1));
        await this._stream.WriteAsync(new [] {(byte) message.Type});
        await this._stream.WriteAsync(message.Payload);
        await this._stream.FlushAsync();
        this._streamSemaphore.Release();
    }
    
    /// <summary>
    /// Receives a message.
    /// </summary>
    /// <returns>Message that was received.</returns>
    public async Task<PacketData> ReceiveAsync(int? maxLength = null)
    {
        // Get the packet length and throw an exception if the connection closed.
        var packetLenBuffer = new byte[4];
        var lengthBytesRead = await this._stream.ReadAsync(packetLenBuffer);
        if (lengthBytesRead == 0)
        {
            throw new InvalidOperationException("Connection closed.");
        }
        
        // Determine the packet size.
        var packetSize = BitConverter.ToInt32(packetLenBuffer) - 1;
        if (maxLength != null)
        {
            packetSize = Math.Min(packetSize, maxLength.Value);
        }
        
        // Read and return the data.
        var packetType = new byte[1];
        await this._stream.ReadAsync(packetType);
        var packetBuffer = new byte[packetSize];
        if (packetBuffer.Length != 0)
        {
            var packetBytesRead = 0;
            while (packetBytesRead < packetBuffer.Length)
            {
                var currentBytesRead = await this._stream.ReadAsync(packetBuffer, packetBytesRead, packetBuffer.Length - packetBytesRead);
                if (currentBytesRead == 0)
                {
                    throw new InvalidOperationException("Connection closed.");
                }
                packetBytesRead += currentBytesRead;
            }
        }
        return new PacketData((PacketData.PacketType) packetType[0], packetBuffer);
    }

    /// <summary>
    /// Closes the stream.
    /// </summary>
    public void Close()
    {
        this._stream.Close();
    }
}