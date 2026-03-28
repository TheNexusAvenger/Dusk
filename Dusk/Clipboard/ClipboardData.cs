namespace Dusk.Clipboard;

public class ClipboardData
{
    /// <summary>
    /// MIME type fo the clipboard.
    /// </summary>
    public string MimeType { get; set; } = null!;

    /// <summary>
    /// Data stored in the clipboard.
    /// </summary>
    public byte[] Data { get; set; } = null!;
}