using System.CommandLine;
using System.IO.Pipes;
using Dusk.Client;
using Dusk.Clipboard;
using Dusk.Diagnostic;
using Dusk.Server;

namespace Dusk;

public class Program
{
    /// <summary>
    /// Runs the program.
    /// </summary>
    /// <param name="args">Arguments from the command line.</param>
    public static void Main(string[] args)
    {
        // Create the command for running the server.
        var serveCommand = new Command("serve", description: "Runs the server.");
        serveCommand.SetAction(async parseResult =>
        {
            await new SocketServer().StartAsync();
        });
        
        // Create the command for running the client.
        var runCommand = new Command("run", description: "Runs the client.");
        runCommand.SetAction(async parseResult =>
        {
            while (true)
            {
                // Run the connection.
                try
                {
                    var connection = await ClientConnection.ConnectAsync();
                    var clipboardTask = IClipboard.GetClipboard().MonitorClipboardChangesAsync(connection);
                    var connectionTask = connection.StartAsync();
                    var namedPipeServerTask = connection.RunPipeServerAsync();
                    await Task.WhenAny(clipboardTask, connectionTask, namedPipeServerTask);
                }
                catch (Exception e)
                {
                    Logger.Error($"Connection closed: {e}");
                }
                
                // Automatically reconnect after 5 seconds.
                Logger.Warn("Connection closed. Attempting to reconnect in 5 seconds.");
                await Task.Delay(TimeSpan.FromSeconds(5));
            }
        });
        
        // Create the command for sending the current clipboard to the server.
        var sendClipboardCommand = new Command("send-clipboard", description: "Sends the current clipboard to the server.");
        sendClipboardCommand.SetAction(async parseResult =>
        {
            // Send the update clipboard request to the socket.
            await using var pipeClient = new NamedPipeClientStream(".", "DuskClient", PipeDirection.Out);
            await pipeClient.ConnectAsync();
            await using var streamWriter = new StreamWriter(pipeClient);
            await streamWriter.WriteLineAsync("UpdateClipboard");
            await streamWriter.FlushAsync();
        });
        
        // Create the root command.
        var rootCommand = new RootCommand(description: "Synchronizes remote systems.");
        rootCommand.Subcommands.Add(serveCommand);
        rootCommand.Subcommands.Add(runCommand);
        rootCommand.Subcommands.Add(sendClipboardCommand);
        
        // Parse and run the root command.
        var rootCommandParse = rootCommand.Parse(args);
        try
        {
            rootCommandParse.Invoke();
        }
        finally
        {
            Logger.WaitForCompletionAsync().Wait();
        }
    }
}