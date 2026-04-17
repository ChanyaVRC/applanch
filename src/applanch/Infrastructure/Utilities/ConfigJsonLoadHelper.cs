using System.IO;
using System.Text.Json;
using applanch.Core.Configuration;
using applanch.Core.Utilities;
using applanch.Serialization;

namespace applanch.Infrastructure.Utilities;

internal static class ConfigJsonLoadHelper
{
    internal static JsonSerializerOptions SerializerOptions => JsonConfigLoader.DefaultSerializerOptions;

    internal static JsonDocumentOptions DocumentOptions => JsonConfigLoader.DefaultDocumentOptions;

    internal static T Load<T>(ConfigJsonPathCandidate candidate, string configDescription)
    {
        return Load(
            candidate,
            configDescription,
            static path => DeserializeFile<T>(path, SerializerOptions));
    }

    internal static void LoadAndMerge<T>(
        IEnumerable<ConfigJsonPathCandidate> candidates,
        string configDescription,
        Func<string, T> loader,
        Action<T> merge)
    {
        foreach (var candidate in candidates)
        {
            try
            {
                var loaded = Load(candidate, configDescription, loader);
                merge(loaded);
            }
            catch (Exception)
            {
            }
        }
    }

    internal static T Load<T>(
        ConfigJsonPathCandidate candidate,
        string configDescription,
        Func<string, T> loader)
    {
        try
        {
            var loaded = loader(candidate.Path);
            AppLogger.Instance.Info($"Loaded {configDescription}: {candidate.Path}");
            return loaded;
        }
        catch (Exception exception)
        {
            LogLoadFailure(configDescription, candidate.Path, exception, candidate.IsBundled);
            if (candidate.IsBundled)
            {
                ReportBundledFailure(candidate.Path, exception);
            }

            throw;
        }
    }

    internal static T DeserializeFile<T>(string path, JsonSerializerOptions options)
    {
        return JsonConfigLoader.DeserializeFile<T>(path, options);
    }

    internal static void LogLoadFailure(string configDescription, string path, Exception exception, bool isBundled)
    {
        var message = $"Failed to load {configDescription} '{path}'";

        if (isBundled)
        {
            AppLogger.Instance.Error(exception, message);
            return;
        }

        AppLogger.Instance.Warn($"{message}: {exception.Message}");
    }

    internal static void ReportBundledFailure(string path, Exception exception)
    {
        if (exception is FileNotFoundException)
        {
            BundledConfigLoadNotificationCenter.ReportMissing(path);
            return;
        }

        BundledConfigLoadNotificationCenter.ReportInvalidFormat(path);
    }
}
