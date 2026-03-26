using Dusk.Server.Network;

namespace Dusk.Server.Model;

public class ServerDomain
{
    /// <summary>
    /// Connections under the server domain.
    /// </summary>
    public readonly Dictionary<string, ServerDomainConnection> Connections = new Dictionary<string, ServerDomainConnection>();
}