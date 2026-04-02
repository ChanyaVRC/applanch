using applanch.Infrastructure.Storage;

namespace applanch.Infrastructure.Integration;

internal interface INetworkPolicyResolver
{
    void ApplySettings(AppSettings settings);

    bool ShouldRequestFavicon(Uri pageUri);

    Task<bool> IsDestinationAllowedAsync(Uri uri);

    Task<Uri?> ResolveAllowedRedirectAsync(Uri originalUri, Uri currentUri, Uri? location);
}
