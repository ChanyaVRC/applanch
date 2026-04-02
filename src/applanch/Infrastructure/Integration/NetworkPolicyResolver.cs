using System.Net;
using applanch.Infrastructure.Storage;
using applanch.Infrastructure.Utilities;

namespace applanch.Infrastructure.Integration;

internal sealed class NetworkPolicyResolver : INetworkPolicyResolver
{
    private static readonly IPNetwork[] PrivateNetworks =
    [
        IPNetwork.Parse("10.0.0.0/8"),
        IPNetwork.Parse("172.16.0.0/12"),
        IPNetwork.Parse("192.168.0.0/16"),
        IPNetwork.Parse("169.254.0.0/16"),
        IPNetwork.Parse("fc00::/7"),
        IPNetwork.Parse("fe80::/10"),
        IPNetwork.Parse("fec0::/10"),
    ];

    private readonly Func<string, CancellationToken, Task<IReadOnlyList<IPAddress>>> _hostAddressResolver;
    private AppSettings _settings = new();

    internal NetworkPolicyResolver(Func<string, CancellationToken, Task<IReadOnlyList<IPAddress>>>? hostAddressResolver = null)
    {
        _hostAddressResolver = hostAddressResolver ?? DefaultResolveHostAddressesAsync;
    }

    public void ApplySettings(AppSettings settings)
    {
        _settings = settings;
    }

    public bool ShouldRequestFavicon(Uri pageUri)
    {
        if (!_settings.FetchHttpIcons)
        {
            return false;
        }

        if (_settings.AllowPrivateNetworkHttpIconRequests)
        {
            return true;
        }

        return !IsLocalOrPrivateLiteral(pageUri.Host);
    }

    public async Task<bool> IsDestinationAllowedAsync(Uri uri)
    {
        if (_settings.AllowPrivateNetworkHttpIconRequests)
        {
            return true;
        }

        if (IsLocalOrPrivateLiteral(uri.Host))
        {
            return false;
        }

        try
        {
            var addresses = await _hostAddressResolver(uri.IdnHost, CancellationToken.None).ConfigureAwait(false);
            return addresses.Count == 0 || addresses.All(static address => !IsPrivateOrLoopbackAddress(address));
        }
        catch (Exception ex)
        {
            AppLogger.Instance.Warn($"Failed to resolve favicon host '{uri.Host}': {ex.Message}");
            return false;
        }
    }

    public async Task<Uri?> ResolveAllowedRedirectAsync(Uri originalUri, Uri currentUri, Uri? location)
    {
        var target = ResolveRedirectTarget(originalUri, currentUri, location);
        if (target is null)
        {
            return null;
        }

        return await IsDestinationAllowedAsync(target).ConfigureAwait(false)
            ? target
            : null;
    }

    internal static Uri? ResolveRedirectTarget(Uri originalUri, Uri currentUri, Uri? location)
    {
        if (location is null)
        {
            return null;
        }

        var target = location.IsAbsoluteUri ? location : new Uri(currentUri, location);
        if (target.Scheme is not ("http" or "https"))
        {
            return null;
        }

        return string.Equals(target.IdnHost, originalUri.IdnHost, StringComparison.OrdinalIgnoreCase)
            ? target
            : null;
    }

    private static bool IsLocalOrPrivateLiteral(string host)
    {
        if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return IPAddress.TryParse(host, out var address) && IsPrivateOrLoopbackAddress(address);
    }

    private static bool IsPrivateOrLoopbackAddress(IPAddress address)
    {
        if (IPAddress.IsLoopback(address))
        {
            return true;
        }

        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        return PrivateNetworks.Any(network => network.Contains(address));
    }

    private static async Task<IReadOnlyList<IPAddress>> DefaultResolveHostAddressesAsync(string host, CancellationToken cancellationToken)
    {
        return await Dns.GetHostAddressesAsync(host, cancellationToken).ConfigureAwait(false);
    }
}
