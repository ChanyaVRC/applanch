using System.Buffers;

namespace applanch.Utilities;

public static class AppDataPaths
{
    public const string ProductDirectoryName = "applanch";

    public static string LocalApplicationDataDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), ProductDirectoryName);

    public static string GetUnderLocalApplicationData(params string[] relativeSegments)
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
