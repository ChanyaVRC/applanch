using System.IO;
using applanch.Localization;

namespace applanch.ThemeCreator;

internal static class ThemeCreatorApp
{
    private static readonly HashSet<string> KnownOptions =
    [
        "--id",
        "--output",
        "--source",
        "--base",
    ];

    internal static int Run(string[] args, TextWriter output, TextWriter error)
    {
        ThemeCreatorLogger.Info($"Theme creator started. Mode={(args.Length == 0 ? "GUI" : "CLI")}");

        if (args.Length == 0)
        {
            var app = new System.Windows.Application();
            app.DispatcherUnhandledException += (_, eventArgs) => ThemeCreatorLogger.Error(eventArgs.Exception, "Unhandled UI exception");

            ThemeCreatorLogger.Info("Launching GUI window.");
            var exitCode = app.Run(new ThemeCreatorWindow());
            ThemeCreatorLogger.Info($"GUI window closed. ExitCode={exitCode}");
            return exitCode;
        }

        if (!TryParseArgs(args, out var options, out var parseError))
        {
            ThemeCreatorLogger.Warn($"CLI argument parsing failed. Error={parseError}");
            error.WriteLine(parseError);
            WriteUsage(error);
            return 1;
        }

        try
        {
            ThemeCreatorLogger.Info(
                $"Creating theme from CLI. ThemeId={options!.ThemeId}, BaseThemeId={options.BaseThemeId}, IncludeEntries={options.IncludeEntries}, OutputPath={options.OutputPath}");
            var service = new ThemeCreatorService();
            service.CreateThemeFile(options);
            ThemeCreatorLogger.Info($"CLI theme created successfully. OutputPath={options.OutputPath}");
            output.WriteLine(string.Format(AppResources.CliCreatedMessageFormat, options.OutputPath));
            return 0;
        }
        catch (Exception ex)
        {
            ThemeCreatorLogger.Error(ex, "CLI theme creation failed");
            error.WriteLine(string.Format(AppResources.CliFailedMessageFormat, ex.Message));
            return 1;
        }
    }

    private static bool TryParseArgs(string[] args, out ThemeCreatorOptions? options, out string error)
    {
        options = null;
        error = string.Empty;

        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var includeEntries = true;

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (string.Equals(arg, "--no-entries", StringComparison.OrdinalIgnoreCase))
            {
                includeEntries = false;
                continue;
            }

            if (!arg.StartsWith("--", StringComparison.Ordinal))
            {
                error = string.Format(AppResources.CliUnknownArgumentFormat, arg);
                return false;
            }

            if (!KnownOptions.Contains(arg) &&
                !arg.StartsWith("--name-", StringComparison.OrdinalIgnoreCase))
            {
                error = string.Format(AppResources.CliUnknownArgumentFormat, arg);
                return false;
            }

            if (i + 1 >= args.Length)
            {
                error = string.Format(AppResources.CliMissingArgumentValueFormat, arg);
                return false;
            }

            values[arg] = args[++i];
        }

        if (!values.TryGetValue("--id", out var themeId) || string.IsNullOrWhiteSpace(themeId))
        {
            error = AppResources.CliThemeIdRequired;
            return false;
        }

        if (!values.TryGetValue("--output", out var outputPath) || string.IsNullOrWhiteSpace(outputPath))
        {
            error = AppResources.CliOutputRequired;
            return false;
        }

        var sourcePalettePath = values.TryGetValue("--source", out var source)
            ? source
            : ThemeCreatorPathResolver.ResolveDefaultSourcePath();

        var baseThemeId = values.TryGetValue("--base", out var baseTheme)
            ? NormalizeBaseThemeId(baseTheme)
            : "light";

        if (!TryBuildDisplayNames(values, out var displayNames, out error))
        {
            return false;
        }

        options = new ThemeCreatorOptions(
            themeId.Trim(),
            outputPath,
            baseThemeId,
            sourcePalettePath,
            displayNames,
            includeEntries);

        return true;
    }

    private static bool TryBuildDisplayNames(
        IReadOnlyDictionary<string, string> values,
        out Dictionary<LanguageOption, string>? displayNames,
        out string error)
    {
        displayNames = null;
        error = string.Empty;

        foreach (var (key, value) in values)
        {
            if (!key.StartsWith("--name-", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var code = key["--name-".Length..];
            if (!LanguageOption.TryFromCode(code, out var language) ||
                language == LanguageOption.System)
            {
                error = string.Format(AppResources.CliUnknownArgumentFormat, key);
                return false;
            }

            var trimmed = value.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                continue;
            }

            displayNames ??= [];
            displayNames[language] = trimmed;
        }

        return true;
    }

    private static string? NormalizeBaseThemeId(string? value)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return null;
        }

        return string.Equals(trimmed, "none", StringComparison.OrdinalIgnoreCase)
            ? null
            : trimmed;
    }

    private static void WriteUsage(TextWriter writer)
    {
        writer.WriteLine(AppResources.CliUsageHeader);
        writer.WriteLine(AppResources.CliUsageCommand);
        writer.WriteLine();
        writer.WriteLine(AppResources.CliOptionsHeader);
        writer.WriteLine(AppResources.CliOptionBase);
        writer.WriteLine(AppResources.CliOptionSource);
        writer.WriteLine(AppResources.CliOptionNameGeneric);
        writer.WriteLine(AppResources.CliOptionNameEn);
        writer.WriteLine(AppResources.CliOptionNameJa);
        writer.WriteLine(AppResources.CliOptionNoEntries);
    }
}

