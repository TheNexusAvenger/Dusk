using System.CommandLine;
using Dusk.Client;
using Dusk.Diagnostic;
using Dusk.Server;
using Microsoft.Extensions.Logging;

namespace Dusk;

public class Program
{
    /// <summary>
    /// Command option for debug logging.
    /// </summary>
    public static readonly Option<bool> DebugOutputOption = new Option<bool>("--debug")
    {
        Description = "Enables debug logging.",
    };
    
    /// <summary>
    /// Runs the program.
    /// </summary>
    /// <param name="args">Arguments from the command line.</param>
    public static void Main(string[] args)
    {
        // Create the command for running the server.
        var serveCommand = new Command("serve", description: "Runs the server.");
        serveCommand.Options.Add(DebugOutputOption);
        serveCommand.SetAction(parseResult =>
        {
            new SocketServer().StartAsync().Wait();
        });
        
        // Create the command for running the client.
        var runCommand = new Command("run", description: "Runs the client.");
        runCommand.Options.Add(DebugOutputOption);
        runCommand.SetAction(parseResult =>
        {
            ClientConnection.StartAsync().Wait();
        });
        
        // Create the command for sending the current clipboard to the server.
        var sendClipboardCommand = new Command("send-clipboard", description: "Sends the current clipboard to the server.");
        sendClipboardCommand.Options.Add(DebugOutputOption);
        sendClipboardCommand.SetAction(parseResult =>
        {
            // TODO
        });
        
        // Create the root command.
        var rootCommand = new RootCommand(description: "Synchronizes remote systems.");
        rootCommand.Subcommands.Add(serveCommand);
        rootCommand.Subcommands.Add(runCommand);
        rootCommand.Subcommands.Add(sendClipboardCommand);
        
        // Parse and run the root command.
        var rootCommandParse = rootCommand.Parse(args);
        if (rootCommandParse.GetValue(DebugOutputOption))
        {
            Logger.SetMinimumLogLevel(LogLevel.Debug);
        }
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