using Dusk.Clipboard;
using Dusk.Server.Network;

namespace Dusk.Server.Model;

public class ServerDomain
{
    /// <summary>
    /// Name of the server domain.
    /// </summary>
    public string Name { get; set; } = null!;
    
    /// <summary>
    /// Last clipboard data that was replicated.
    /// </summary>
    public ClipboardData? LastClipboardData { get; set; }
    
    /// <summary>
    /// Connections under the server domain.
    /// </summary>
    public readonly Dictionary<string, ServerDomainConnection> Connections = new Dictionary<string, ServerDomainConnection>();
}