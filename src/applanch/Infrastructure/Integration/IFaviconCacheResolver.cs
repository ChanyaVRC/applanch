using System.Windows.Media.Imaging;

namespace applanch.Infrastructure.Integration;

internal interface IFaviconCacheResolver
{
    BitmapFrame? TryLoad(Uri faviconUri, bool acceptExpired);

    void TryWrite(Uri faviconUri, byte[] payload);
}
