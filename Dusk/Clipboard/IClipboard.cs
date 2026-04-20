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
    /// Static clipboard instance.
    /// </summary>
    private static IClipboard? _clipboard;

    /// <summary>
    /// Returns the clipboard for the current environment.
    /// </summary>
    /// <returns>The clipboard for the current environment.</returns>
    public static IClipboard GetClipboard()
    {
        // Create the clipboard if it doesn't exist.
        if (_clipboard == null)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _clipboard = new WindowsClipboard();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                _clipboard = new LinuxClipboard();
            }
            else
            {
                throw new PlatformNotSupportedException("Clipboard implementation not found for current platform.");
            }
        }
        
        // Return the clipboard.
        return _clipboard;
    }
}