using System.Net;

namespace applanch.Infrastructure.Integration;

internal interface IHostAddressResolver
{
    Task<IReadOnlyList<IPAddress>> ResolveHostAddressesAsync(string host, CancellationToken cancellationToken);
}
