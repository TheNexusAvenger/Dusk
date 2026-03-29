using System.CommandLine;
using Dusk.Client;
using Dusk.Clipboard;
using Dusk.Diagnostic;
using Dusk.Network;
using Dusk.Network.Packet;
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
            (await ClientConnection.ConnectAsync()).Start();
        });
        
        // Create the command for sending the current clipboard to the server.
        var sendClipboardConnectionIdArgument = new Argument<string>("connectionId")
        {
            Description = "Id of the connection sending the clipboard data.",
            Arity = ArgumentArity.ExactlyOne,
        };
        
        var sendClipboardCommand = new Command("send-clipboard", description: "Sends the current clipboard to the server.");
        sendClipboardCommand.Arguments.Add(sendClipboardConnectionIdArgument);
        sendClipboardCommand.SetAction(async parseResult =>
        {
            // Open the connection.
            var client = await ClientConnection.ConnectAsync(PacketData.PacketType.AuthenticationShortLived);
            
            // Send the clipboard.
            var clipboard = IClipboard.GetClipboard();
            var clipboardData = await clipboard.ReadClipboardAsync();
            if (clipboardData == null)
            {
                Logger.Error("Clipboard read failed.");
                return;
            }
            Logger.Info($"Sending clipboard with MIME type {clipboardData.MimeType}.");
            await client.TrySendPacketAsync(new UpdateClipboardPacket()
            {
                SourceConnectionId = parseResult.GetValue(sendClipboardConnectionIdArgument)!,
                MimeType = clipboardData.MimeType,
                Data = clipboardData.Data,
            }.ToPacketData());
            
            // Keep the connection alive until complete.
            await client.Stream.ReceiveAsync();
            client.Close();
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