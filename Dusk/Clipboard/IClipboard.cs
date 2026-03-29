using System.Runtime.InteropServices;
using Dusk.Client;
using Dusk.Clipboard.Windows;
using Microsoft.Extensions.Logging;

namespace Dusk.Clipboard;

public interface IClipboard
{
    /// <summary>
    /// Reads the current clipboard.
    /// </summary>
    /// <param name="debugLogLevel">Optional log level for the debug log messages.</param>
    /// <returns>Contents of the clipboard.</returns>
    public Task<ClipboardData?> ReadClipboardAsync(LogLevel debugLogLevel = LogLevel.Debug);
    
    /// <summary>
    /// Writes the current clipboard.
    /// </summary>
    /// <param name="data">Contents of the clipboard.</param>
    public Task WriteClipboardAsync(ClipboardData data);

    /// <summary>
    /// Listens for clipboard changes.
    /// </summary>
    /// <param name="clientConnection">Client connection to send clipboard updates for.</param>
    public Task MonitorClipboardChangesAsync(ClientConnection clientConnection);

    /// <summary>
    /// Returns the clipboard for the current environment.
    /// </summary>
    /// <returns>The clipboard for the current environment.</returns>
    public static IClipboard GetClipboard()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return new WindowsClipboard();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return new LinuxClipboard();
        }
        throw new PlatformNotSupportedException("Clipboard implementation not found for current platform.");
    }
}