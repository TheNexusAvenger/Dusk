namespace Dusk.Clipboard.Windows;

public class WindowsClipboardReaders
{
    /// <summary>
    /// Reads the Windows clipboard and converts to MIME types.
    /// This is run in order!
    /// Reference: https://learn.microsoft.com/en-us/windows/win32/dataxchg/standard-clipboard-formats
    /// </summary>
    public static readonly List<WindowsClipboardEntry> ClipboardReaders = new List<WindowsClipboardEntry>()
    {
        // CF_UNICODETEXT (13)
        // Must be before CF_TEXT (1), since text has both.
        new WindowsClipboardEntry()
        {
            MimeType = "text/plain;charset=utf-16le",
            ClipboardFormat = "CF_UNICODETEXT",
        },
        
        // CF_TEXT (1)
        new WindowsClipboardEntry()
        {
            MimeType = "text/plain",
            ClipboardFormat = "CF_TEXT",
        },
        
        // Unsupported:
        // CF_BITMAP (2)
        // CF_METAFILEPICT (3)
        // CF_SYLK (4)
        // CF_DIF (5)
        // CF_TIFF (6)
        // CF_OEMTEXT (7)
        // CF_DIB (8)
        // CF_PALETTE (9)
        // CF_PENDATA (10)
        // CF_RIFF (11)
        // CF_WAVE (12)
        // CF_ENHMETAFILE (14)
        // CF_HDROP (15)
        // CF_LOCALE (16)
        // CF_DIBV5 (17)
    };
}