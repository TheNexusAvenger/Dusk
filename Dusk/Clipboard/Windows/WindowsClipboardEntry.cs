namespace Dusk.Clipboard.Windows;

public class WindowsClipboardEntry
{
    /// <summary>
    /// MIME type of the clipboard type.
    /// </summary>
    public string MimeType { get; set; } = null!;
    
    /// <summary>
    /// Windows clipboard format of the clipboard type.
    /// </summary>
    public string ClipboardFormat { get; set; } = null!;

    /// <summary>
    /// Converts from one format to another.
    /// If not specified, the original data is used.
    /// </summary>
    public Func<byte[], byte[]>? Convert { get; set; } = null!;
}