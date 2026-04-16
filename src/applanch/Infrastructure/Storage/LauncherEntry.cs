using System.Text.Json.Serialization;
using applanch.Core.Utilities;

namespace applanch.Infrastructure.Storage;

internal sealed record LauncherEntry(
    [property: JsonConverter(typeof(LaunchPathJsonConverter))] LaunchPath Path,
    [property: JsonConverter(typeof(CategoryJsonConverter))] Category Category,
    string Arguments,
    string DisplayName)
{
    public static string DefaultCategory => string.Empty;
    public static string AllCategories => "*";

    internal LauncherEntry(string path, Category category, string arguments, string displayName)
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