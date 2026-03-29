using System.Diagnostics;
using System.Net.Mime;
using System.Text;
using Dusk.Client;
using Dusk.Diagnostic;
using Microsoft.Extensions.Logging;

namespace Dusk.Clipboard;

public class LinuxClipboard : IClipboard
{
    /// <summary>
    /// MIME types by priority.
    /// </summary>
    public static readonly List<string> PriorityMimeTypes = new List<string>()
    {
        "text/plain", // Priority over text/html
    };
    
    /// <summary>
    /// Path to the wl-copy command.
    /// </summary>
    private readonly string? _wlCopyPath = FindInPath("wl-copy");
    
    /// <summary>
    /// Path to the wl-paste command.
    /// </summary>
    private readonly string? _wlPastePath = FindInPath("wl-paste");

    /// <summary>
    /// Creates a Linux clipboard.
    /// </summary>
    public LinuxClipboard()
    {
        // Throw an exception if wl-copy or wl-paste is missing.
        if (_wlCopyPath == null)
        {
            Logger.Error("wl-copy not found in PATH.");
            throw new InvalidOperationException("wl-copy not found in PATH.");
        }
        if (_wlPastePath == null)
        {
            Logger.Error("wl-paste not found in PATH.");
            throw new InvalidOperationException("wl-paste not found in PATH.");
        }
    }
    
    /// <summary>
    /// Attempts to find a file in the PATH.
    /// Only works on Linux.
    /// </summary>
    /// <param name="fileName">Name of the file to find in the PATH.</param>
    /// <returns>Path of the file, if it exists.</returns>
    public static string? FindInPath(string fileName)
    {
        return Environment.GetEnvironmentVariable("PATH")!.Split(":")
            .Select(directory => Path.Combine(directory, fileName))
            .FirstOrDefault(File.Exists);
    }

    /// <summary>
    /// Reads the current clipboard.
    /// </summary>
    /// <param name="debugLogLevel">Optional log level for the debug log messages.</param>
    /// <returns>Contents of the clipboard.</returns>
    public async Task<ClipboardData?> ReadClipboardAsync(LogLevel debugLogLevel = LogLevel.Debug)
    {
        // List the types of the clipboard.
        // Return if nothing is copied.
        var mimeTypesOutput = Encoding.UTF8.GetString(await RunWlPaste("--list-types")).Trim();
        if (mimeTypesOutput == "Nothing is copied")
        {
            Logger.Info("Clipboard is empty.");
            return null;
        }
        
        // Read the MIME types and pick the first one with a slash.
        var mimeTypes = mimeTypesOutput.Split("\n").Select(x => x.Trim()).ToArray();
        Logger.Log(debugLogLevel, $"Reading clipboard data with MIME type: {string.Join(", ", mimeTypes)}");
        var mimeType = mimeTypes.FirstOrDefault(mimeType => mimeType.Contains('/'));
        if (mimeType == null)
        {
            Logger.Warn($"Attempted to read clipboard but no MIME type was found: {string.Join(", ", mimeTypes)}");
            return null;
        }
        
        // Set the priority MIME type if one exists.
        foreach (var priorityMimeType in PriorityMimeTypes)
        {
            foreach (var clipboardMimeType in mimeTypes)
            {
                try
                {
                    var contentType = new ContentType(clipboardMimeType);
                    if (contentType.MediaType != priorityMimeType) continue;
                    Logger.Log(debugLogLevel, $"Using priority MIME type: {clipboardMimeType}");
                    mimeType = clipboardMimeType;
                    break;
                }
                catch (FormatException)
                {
                    // MIME type invalid.
                }
            }
        }
        
        // Read and return the clipboard data.
        Logger.Log(debugLogLevel, $"Reading clipboard with MIME type: {mimeType}");
        var data = await RunWlPaste($"--no-newline --type {mimeType}");
        return new ClipboardData()
        {
            MimeType = mimeType,
            Data = data,
        };
    }

    /// <summary>
    /// Writes the current clipboard.
    /// </summary>
    /// <param name="data">Contents of the clipboard.</param>
    public async Task WriteClipboardAsync(ClipboardData data)
    {
        // Start the wl-copy command.
        Logger.Debug($"Writing clipboard data with MIME type: {data.MimeType}");
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = _wlCopyPath!,
            ArgumentList = { "--type", data.MimeType },
            RedirectStandardInput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        process.Start();

        // Send the input.        
        await process.StandardInput.BaseStream.WriteAsync(data.Data);
        process.StandardInput.Close();

        // Wait for the command to finish.
        await process.WaitForExitAsync();
        if (process.ExitCode != 0)
        {
            Logger.Warn($"wl-copy exited with code {process.ExitCode}.");
        }
    }

    /// <summary>
    /// Listens for clipboard changes.
    /// </summary>
    /// <param name="clientConnection">Client connection to send clipboard updates for.</param>
    public async Task MonitorClipboardChangesAsync(ClientConnection clientConnection)
    {
        // Create the paste process.
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = _wlPastePath,
            Arguments = $"--watch \"{Environment.ProcessPath}\" send-clipboard",
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        process.Start();
        
        // Wait for the process to complete.
        await process.WaitForExitAsync();
    }

    /// <summary>
    /// Runs the wl-paste command.
    /// </summary>
    /// <param name="arguments">Arguments to pass to wl-paste.</param>
    /// <returns>The contents of the stdout.</returns>
    private async Task<byte[]> RunWlPaste(string arguments)
    {
        // Create the paste process.
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = _wlPastePath,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        process.Start();

        // Copy the output to a memory stream.
        // This must be copied out to avoid the output stream getting full and pausing the application.
        using var outputMemoryStream = new MemoryStream();
        using var errorMemoryStream = new MemoryStream();
        await process.StandardOutput.BaseStream.CopyToAsync(outputMemoryStream);
        await process.StandardError.BaseStream.CopyToAsync(errorMemoryStream);

        // Wait for the paste process to exist.
        await process.WaitForExitAsync();
        if (process.ExitCode != 0 && !arguments.Contains("--list-types"))
        {
            Logger.Warn($"wl-paste exited with code {process.ExitCode}.");
        }
        
        // Return the output.
        return outputMemoryStream.ToArray().Concat(errorMemoryStream.ToArray()).ToArray();
    }
}