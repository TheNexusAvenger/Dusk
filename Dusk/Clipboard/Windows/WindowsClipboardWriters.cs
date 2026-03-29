using System.Text;

namespace Dusk.Clipboard.Windows;

public class WindowsClipboardWriters
{
    /// <summary>
    /// Reads MIME types and converts to Windows clipboard format.
    /// </summary>
    public static readonly List<WindowsClipboardEntry> ClipboardWriters = new List<WindowsClipboardEntry>()
    {
        new WindowsClipboardEntry()
        {
            MimeType = "text/plain;charset=utf-8",
            ClipboardFormat = "CF_UNICODETEXT",
            Convert = (data) => Encoding.Unicode.GetBytes(Encoding.UTF8.GetString(data)).Concat(new byte[1]).ToArray(),
        },
        new WindowsClipboardEntry()
        {
            MimeType = "text/plain",
            ClipboardFormat = "CF_TEXT",
        },
    };
}