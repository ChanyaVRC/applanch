using System.Buffers;

namespace applanch.Infrastructure.Utilities;

internal static class AppDataPaths
{
    internal const string ProductDirectoryName = "applanch";

    internal static string LocalApplicationDataDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), ProductDirectoryName);

    internal static string GetUnderLocalApplicationData(params string[] relativeSegments)
    {
        if (relativeSegments.Length == 0)
        {
            return LocalApplicationDataDirectory;
        }

        var segmentCount = relativeSegments.Length + 1;
        var rented = ArrayPool<string>.Shared.Rent(segmentCount);

        try
        {
            rented[0] = LocalApplicationDataDirectory;
            relativeSegments.CopyTo(rented, 1);
            return Path.Combine(rented.AsSpan(0, segmentCount));
        }
        finally
        {
            ArrayPool<string>.Shared.Return(rented);
        }
    }
}