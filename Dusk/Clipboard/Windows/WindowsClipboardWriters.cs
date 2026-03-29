using System.Net.Mime;
using System.Text;
using Dusk.Diagnostic;

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
                // Since the source will use UTF-16be but won't state it, it must be converted to UTF-16be (instead of UTF-16le).
                var encoding = Encoding.GetEncoding(contentType.CharSet ?? "utf-8");
                string inputString = null!;
                if (encoding.EncodingName == "Unicode" && data[0] == 0xFE && data[1] == 0xFF)
                {
                    Logger.Debug("Using UTF-16le instead of detected UTF-16be.");
                    inputString = Encoding.BigEndianUnicode.GetString(data, 2, data.Length - 2);
                }
                else
                {
                    inputString = encoding.GetString(data);
                }
                
                // Convert from the source encoding and to UTF-16le.
                return Encoding.Unicode.GetBytes(inputString).Concat(new byte[2]).ToArray();
            },
        },
    };
}