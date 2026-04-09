using System.IO.Pipes;
using Dusk.Diagnostic;

namespace Dusk.Client;

public class ClientPipeServer
{
    /// <summary>
    /// Current connection to send clipboard requests to.
    /// </summary>
    public ClientConnection? CurrentConnection { get; set; }
    
    /// <summary>
    /// Runs the pipe server for inter-process communication (mainly for Linux).
    /// </summary>
    public async Task RunPipeServerAsync()
    {
        // Listen for requests to send the clipboard.
        while (true)
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
                    if (this.CurrentConnection != null && this.CurrentConnection.IsActive())
                    {
                        await this.CurrentConnection.SendClipboardAsync();
                    }
                    else
                    {
                        Logger.Warn("Unable to send clipboard due to no open connection.");
                    }
                }
                else
                {
                    // Warn if there is no handler.
                    Logger.Warn($"No pipe handler for \"{request}\".");
                }
            }
            catch (IOException)
            {
                // Pipe closed.
            }
        }
    }
}