using System.Net.Mime;
using System.Text;

namespace Dusk.Clipboard.Windows;

public class WindowsClipboardWriters
{
    public class WindowsClipboardWriteEntry
    {
        /// <summary>
        /// MIME type of the clipboard type.
        /// </summary>
        public string MimeType { get; set; } = null!;
        
        /// <summary>
        /// MIME subtype of the clipboard type.
        /// </summary>
        public string? MimeSubtype { get; set; }
    
        /// <summary>
        /// Windows clipboard format of the clipboard type.
        /// </summary>
        public string ClipboardFormat { get; set; } = null!;

        /// <summary>
        /// Converts from one format to another.
        /// If not specified, the original data is used.
        /// </summary>
        public Func<byte[], ContentType, byte[]>? Convert { get; set; }
    }
    
    /// <summary>
    /// Reads MIME types and converts to Windows clipboard format.
    /// </summary>
    public static readonly List<WindowsClipboardWriteEntry> ClipboardWriters = new List<WindowsClipboardWriteEntry>()
    {
        new WindowsClipboardWriteEntry()
        {
            MimeType = "text",
            // Any subtype accepted.
            ClipboardFormat = "CF_UNICODETEXT",
            Convert = (data, contentType) =>
            {
                // Decode the string.
                var encoding = Encoding.GetEncoding(contentType.CharSet ?? "utf-8");
                using var stream = new MemoryStream(data);
                using var reader = new StreamReader(stream, encoding, detectEncodingFromByteOrderMarks: true);
                var inputString = reader.ReadToEnd();
                
                // Convert from the source encoding and to UTF-16le.
                return Encoding.Unicode.GetBytes(inputString).Concat(new byte[2]).ToArray();
            },
        },
    };
}