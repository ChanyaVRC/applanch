namespace applanch;

internal sealed class AppStartupArguments
{
    internal const string RegisterArgument = "--register";
    internal const string UnregisterContextMenuArgument = "--unregister-context-menu";
    internal const string ThemeArgument = "--theme";

    private readonly Dictionary<string, string> _arguments;

    private AppStartupArguments(Dictionary<string, string> arguments)
    {
        _arguments = arguments;
    }

    internal static AppStartupArguments Parse(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (!arg.StartsWith("--", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.OrdinalIgnoreCase))
            {
                result[arg] = args[i + 1];
                i++;
            }
            else
            {
                result[arg] = string.Empty;
            }
        }

        return new AppStartupArguments(result);
    }

    internal string? ThemeOverrideId
    {
        get
        {
            if (!_arguments.TryGetValue(ThemeArgument, out var themeId))
            {
                return null;
            }

            return themeId;
        }
    }

    internal bool IsContextMenuUnregisterRequested => _arguments.ContainsKey(UnregisterContextMenuArgument);

    internal string? RegisterPath
    {
        get
        {
            if (!_arguments.TryGetValue(RegisterArgument, out var value))
            {
                return null;
            }

            return value;
        }
    }

    internal bool Contains(string argument)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(argument);
        return _arguments.ContainsKey(argument);
    }

    internal bool TryGetValue(string argument, out string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(argument);
        return _arguments.TryGetValue(argument, out value!);
    }
}