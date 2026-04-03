using System.Text.Json.Serialization;
using applanch.Infrastructure.Utilities;

namespace applanch.Infrastructure.Storage;

internal sealed record LauncherEntry(
    [property: JsonConverter(typeof(LaunchPathJsonConverter))] LaunchPath Path,
    string Category,
    string Arguments,
    string DisplayName)
{
    public static string DefaultCategory => AppResources.DefaultCategory;

    internal LauncherEntry(string path, string category, string arguments, string displayName)
        : this(
            string.IsNullOrWhiteSpace(path)
                ? default
                : new LaunchPath(path),
            category,
            arguments,
            displayName)
    {
    }

    [JsonIgnore]
    public bool IsNormalized { get; init; }
}
