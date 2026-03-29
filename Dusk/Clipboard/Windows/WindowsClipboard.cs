using System.ComponentModel;
using System.Net.Mime;
using System.Runtime.InteropServices;
using System.Text;
using Dusk.Client;
using Dusk.Diagnostic;
using Dusk.Network.Packet;
using Microsoft.Extensions.Logging;

namespace Dusk.Clipboard.Windows;

public class WindowsClipboard : IClipboard
{
    /// <summary>
    /// Windows system clipboard formats.
    /// </summary>
    private static readonly Dictionary<uint, string> SystemClipboardFormats = new Dictionary<uint, string>()
    {
        { 1, "CF_TEXT" },
        { 2, "CF_BITMAP" },
        { 3, "CF_METAFILEPICT" },
        { 4, "CF_SYLK" },
        { 5, "CF_DIF" },
        { 6, "CF_TIFF" },
        { 7, "CF_OEMTEXT" },
        { 8, "CF_DIB" },
        { 9, "CF_PALETTE" },
        { 10, "CF_PENDATA" },
        { 11, "CF_RIFF" },
        { 12, "CF_WAVE" },
        { 13, "CF_UNICODETEXT" },
        { 14, "CF_ENHMETAFILE" },
        { 15, "CF_HDROP" },
        { 16, "CF_LOCALE" },
        { 17, "CF_DIBV5" },
    };
    
    /// <summary>
    /// Reads the current clipboard.
    /// </summary>
    /// <param name="debugLogLevel">Optional log level for the debug log messages.</param>
    /// <returns>Contents of the clipboard.</returns>
    public async Task<ClipboardData?> ReadClipboardAsync(LogLevel debugLogLevel = LogLevel.Debug)
    {
        // Open the clipboard.
        await TryOpenClipboardAsync();
        
        // Read the clipboard.
        var readHandle = IntPtr.Zero;
        var lockPointer = IntPtr.Zero;
        try
        {
            // Get the clipboard formats.
            var clipboardFormats = GetClipboardFormats();
            var clipboardFormatsString = string.Join(", ", clipboardFormats.Select(entry => $"{entry.Value} ({entry.Key})").ToList());
            Logger.Log(debugLogLevel, $"Reading clipboard with formats: {clipboardFormatsString}");
            
            // Get the clipboard reader.
            var clipboardReader = WindowsClipboardReaders.ClipboardReaders.FirstOrDefault(reader =>
                clipboardFormats.ContainsValue(reader.ClipboardFormat));
            if (clipboardReader == null)
            {
                if (debugLogLevel == LogLevel.Debug)
                {
                    // Log a warning when in debug mode (not trace from the WindowsClipboard monitoring).
                    Logger.Warn($"No clipboard reader found for formats: {clipboardFormatsString}");
                }
                return new ClipboardData()
                {
                    MimeType = "text/plain;charset=utf-8",
                    Data = Encoding.UTF8.GetBytes($"Unsupported formats: {clipboardFormatsString}"),
                };
            }
            
            // Get the clipboard handle and return null if it can't be obtained.
            readHandle = GetClipboardData(clipboardFormats.First(entry => entry.Value == clipboardReader.ClipboardFormat).Key);
            if (readHandle == IntPtr.Zero)
            {
                return null;
            }

            // Get the global lock and return null if it can't be obtained.
            lockPointer = GlobalLock(readHandle);
            if (lockPointer == IntPtr.Zero)
            {
                Logger.Warn("Failed to get global lock for clipboard.");
                return null;
            }

            // Copy the clipboard to a buffer.
            var size = GlobalSize(readHandle);
            var buffer = new byte[size];
            Marshal.Copy(lockPointer, buffer, 0, size);
            if (clipboardReader.Convert != null)
            {
                buffer = clipboardReader.Convert(buffer);
            }
            
            // Return the clipboard data.
            return new ClipboardData()
            {
                MimeType = clipboardReader.MimeType,
                Data = buffer,
            };
        }
        finally
        {
            // Unlock and close the clipboard.
            if (lockPointer != IntPtr.Zero)
            {
                GlobalUnlock(readHandle);
            }
            CloseClipboard();
        }
    }

    /// <summary>
    /// Writes the current clipboard.
    /// </summary>
    /// <param name="data">Contents of the clipboard.</param>
    public async Task WriteClipboardAsync(ClipboardData data)
    {
        // Open the clipboard.
        await TryOpenClipboardAsync();
        
        // Empty the clipboard.
        EmptyClipboard();
        
        // Try to set the clipboard.
        var dataGlobal = IntPtr.Zero;
        try
        {
            // Get the clipboard writer and replace it if the mimetype is not supported.
            Logger.Debug($"Writing clipboard data with MIME type: {data.MimeType}");
            var contentType = new ContentType(data.MimeType);
            WindowsClipboardWriters.WindowsClipboardWriteEntry? clipboardWriter = null;
            foreach (var writer in WindowsClipboardWriters.ClipboardWriters)
            {
                var mimeTypeParts = contentType.MediaType.Split("/", 2);
                if (writer.MimeType != mimeTypeParts[0]) continue;
                if (writer.MimeSubtype != null && mimeTypeParts[1] != writer.MimeSubtype) continue;
                clipboardWriter = writer;
                break;
            }
            if (clipboardWriter == null)
            {
                Logger.Warn($"Unsupported MIME type: {data.MimeType}");
                data.Data = Encoding.UTF8.GetBytes($"Unsupported MIME type: {data.MimeType}");
                clipboardWriter =
                    WindowsClipboardWriters.ClipboardWriters.FirstOrDefault(entry => entry.MimeType == "text/plain;charset=utf-8");
            }
            
            // Convert the data if the writer has a converter.
            if (clipboardWriter!.Convert != null)
            {
                data.Data = clipboardWriter.Convert(data.Data, contentType);
            }
            
            // Allocate unmanaged memory for the data.
            dataGlobal = Marshal.AllocHGlobal(data.Data.Length);
            if (dataGlobal == IntPtr.Zero)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            // Lock the clipboard.
            var lockPointer = GlobalLock(dataGlobal);
            if (lockPointer == IntPtr.Zero)
            {
                Logger.Warn("Failed to get global lock for clipboard.");
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            try
            {
                // Copy the clipboard data.
                Marshal.Copy(data.Data, 0, dataGlobal, data.Data.Length);
            }
            finally
            {
                // Unlock the clipboard.
                GlobalUnlock(lockPointer);
            }

            // Try to set the clipboard, and throw if it failed.
            if (SetClipboardData(GetClipboardFormatId(clipboardWriter.ClipboardFormat), dataGlobal) == IntPtr.Zero)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
            dataGlobal = IntPtr.Zero;
        }
        finally
        {
            // Unlock and close the clipboard.
            if (dataGlobal != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(dataGlobal);
            }
            CloseClipboard();
        }
    }

    /// <summary>
    /// Listens for clipboard changes.
    /// </summary>
    /// <param name="clientConnection">Client connection to send clipboard updates for.</param>
    public async Task MonitorClipboardChangesAsync(ClientConnection clientConnection)
    {
        // Get the initial clipboard.
        var lastClipboard = await this.ReadClipboardAsync(LogLevel.Trace);
        
        // Run a busy-wait loop to check the clipboard.
        // This isn't optimal, but avoids needing to create a window.
        while (clientConnection.IsActive())
        {
            // Wait to check again.
            await Task.Delay(100);
            
            // Try to read the clipboard.
            try
            {
                // Read the clipboard.
                var currentClipboard = await this.ReadClipboardAsync(LogLevel.Trace);
                if (currentClipboard == null) continue;
                if (lastClipboard != null && lastClipboard.MimeType == currentClipboard.MimeType && currentClipboard.Data.SequenceEqual(lastClipboard.Data)) continue;
                Logger.Debug($"Clipboard change detected. Sending clipboard contents with MIME type {currentClipboard.MimeType}.");
                lastClipboard = currentClipboard;

                // Send the updated clipboard.
                await clientConnection.SendClipboardAsync();
            }
            catch (Exception e)
            {
                Logger.Debug($"Error reading clipboard for changes: {e}");
            }
        }
    }
    
    /// <summary>
    /// Tries to open the clipboard.
    /// </summary>
    private static async Task TryOpenClipboardAsync()
    {
        // Try to open the clipboard with a retry.
        for (var i = 0; i < 10; i++)
        {
            // Return if the clipboard is opened.
            if (OpenClipboard(IntPtr.Zero))
            {
                return;
            }
            
            // Wait to try again.
            await Task.Delay(100);
        }
        
        // Throw the last exception if it was never opened.
        throw new Win32Exception(Marshal.GetLastWin32Error());
    }

    /// <summary>
    /// Returns the current clipboard formats.
    /// </summary>
    /// <returns>List of the current clipboard formats.</returns>
    private static Dictionary<uint, string> GetClipboardFormats()
    {
        var formats = new Dictionary<uint, string>();
        uint format = 0;
        while (true)
        {
            format = EnumClipboardFormats(format);
            if (format == 0) break;
            formats[format] = GetClipboardFormatName(format);
        }
        return formats;
    }

    /// <summary>
    /// Returns the clipboard format name for an id.
    /// </summary>
    /// <param name="format">Id of the format.</param>
    /// <returns>String name of the format.</returns>
    private static string GetClipboardFormatName(uint format)
    {
        // Return a stored format.
        if (SystemClipboardFormats.TryGetValue(format, out var formatName))
        {
            return formatName;
        }
        
        // Try to get the custom format name.
        var formatNameBuilder = new StringBuilder(256);
        var result = GetClipboardFormatName(format, formatNameBuilder, formatNameBuilder.Capacity);
        if (result > 0)
        {
            return formatNameBuilder.ToString();
        }
        
        // Return that the format is unknown.
        return $"Unknown ({format})";
    }
    
    /// <summary>
    /// Returns the clipboard format id for a name.
    /// </summary>
    /// <param name="formatName">Name of the format.</param>
    /// <returns>Id of the format.</returns>
    private static uint GetClipboardFormatId(string formatName)
    {
        foreach (var format in SystemClipboardFormats)
        {
            if (format.Value != formatName) continue;
            return format.Key;
        }
        return RegisterClipboardFormat(formatName);
    }

    [DllImport("User32.dll", SetLastError = true)]
    static extern IntPtr GetClipboardData(uint uFormat);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern IntPtr GlobalLock(IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool GlobalUnlock(IntPtr hMem);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool OpenClipboard(IntPtr hWndNewOwner);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool CloseClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    static extern IntPtr SetClipboardData(uint uFormat, IntPtr data);

    [DllImport("user32.dll")]
    static extern bool EmptyClipboard();

    [DllImport("Kernel32.dll", SetLastError = true)]
    static extern int GlobalSize(IntPtr hMem);
    
    [DllImport("user32.dll", SetLastError = true)]
    static extern uint EnumClipboardFormats(uint format);

    [DllImport("user32.dll", SetLastError = true)]
    static extern int GetClipboardFormatName(uint format, StringBuilder lpszFormatName, int cchMaxCount);
    
    [DllImport("user32.dll", SetLastError = true)]
    static extern uint RegisterClipboardFormat(string lpszFormat);
}