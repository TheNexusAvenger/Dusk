using System.Runtime.InteropServices;
using Dusk.Clipboard.Windows;

namespace Dusk.Clipboard;

public interface IClipboard
{
    /// <summary>
    /// Reads the current clipboard.
    /// </summary>
    /// <returns>Contents of the clipboard.</returns>
    public Task<ClipboardData?> ReadClipboardAsync();
    
    /// <summary>
    /// Writes the current clipboard.
    /// </summary>
    /// <param name="data">Contents of the clipboard</param>
    public Task WriteClipboardAsync(ClipboardData data);

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
            // TODO: Create clipboard using wl-copy/wl-paste.
        }
        throw new PlatformNotSupportedException("Clipboard implementation not found for current platform.");
    }
}