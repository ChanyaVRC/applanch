using System.IO;
using System.Windows;
using applanch.Infrastructure.Integration;
using applanch.Infrastructure.Storage;
using applanch.Infrastructure.Utilities;
using System.Windows.Media;
using System.Windows.Threading;

namespace applanch.ViewModels;

public sealed class LaunchItemViewModel : ObservableObject
{
    private string _displayName;
    private string _category;
    private string _arguments;
    private bool _isRenaming;
    private string _editingName = string.Empty;
    private readonly Dispatcher _dispatcher;
    private readonly ILaunchItemIconProvider _iconProvider;
    private ImageSource? _iconSource;
    private int _iconRefreshVersion;

    public LaunchItemViewModel(LaunchPath fullPath, string category, string arguments, string displayName)
        : this(fullPath, category, arguments, displayName, null)
    {
    }

    internal LaunchItemViewModel(LaunchPath fullPath, string category, string arguments, string displayName, ILaunchItemIconProvider? iconProvider)
    {
        FullPath = fullPath;
        _dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
        _iconProvider = iconProvider ?? LaunchItemIconProvider.Shared;
        _displayName = NormalizeDisplayName(displayName);
        _category = NormalizeCategory(category);
        _arguments = NormalizeArguments(arguments);

        RefreshIcon();
    }

    public string DisplayName
    {
        get => _displayName;
        set => SetNormalizedString(ref _displayName, value, NormalizeDisplayName, nameof(DisplayName));
    }

    public bool IsRenaming
    {
        get => _isRenaming;
        set => SetField(ref _isRenaming, value);
    }

    public string EditingName
    {
        get => _editingName;
        set => SetField(ref _editingName, value);
    }

    public LaunchPath FullPath { get; }
    public ImageSource? IconSource
    {
        get => _iconSource;
        private set => SetField(ref _iconSource, value);
    }

    public bool IsPathMissing => !FullPath.IsUrl && !Path.Exists(FullPath.Value);

    public string Arguments
    {
        get => _arguments;
        set => SetNormalizedString(ref _arguments, value, NormalizeArguments, nameof(Arguments));
    }

    public string Category
    {
        get => _category;
        set => SetNormalizedString(ref _category, value, NormalizeCategory, nameof(Category));
    }

    private string NormalizeDisplayName(string value) =>
        LaunchItemNormalization.NormalizeDisplayName(value, FullPath.Value);

    private static string NormalizeCategory(string value) =>
        LaunchItemNormalization.NormalizeCategory(value);

    private static string NormalizeArguments(string value) =>
        LaunchItemNormalization.NormalizeArguments(value);

    private void SetNormalizedString(ref string field, string value, Func<string, string> normalize, string propertyName)
    {
        var normalized = normalize(value);
        if (field == normalized)
        {
            return;
        }

        field = normalized;
        OnPropertyChanged(propertyName);
    }

    internal void RefreshIcon()
    {
        var refreshVersion = Interlocked.Increment(ref _iconRefreshVersion);
        IconSource = _iconProvider.GetInitialIcon(FullPath);
        _ = LoadDeferredIconAsync(refreshVersion);
    }

    private async Task LoadDeferredIconAsync(int refreshVersion)
    {
        try
        {
            var deferredIcon = await _iconProvider.GetDeferredIconAsync(FullPath).ConfigureAwait(false);
            if (refreshVersion != Volatile.Read(ref _iconRefreshVersion) ||
                deferredIcon is null ||
                ReferenceEquals(IconSource, deferredIcon))
            {
                return;
            }

            if (_dispatcher.CheckAccess())
            {
                if (refreshVersion != Volatile.Read(ref _iconRefreshVersion))
                {
                    return;
                }

                IconSource = deferredIcon;
                return;
            }

            await _dispatcher.InvokeAsync(() =>
            {
                if (refreshVersion == Volatile.Read(ref _iconRefreshVersion))
                {
                    IconSource = deferredIcon;
                }
            });
        }
        catch (Exception ex)
        {
            AppLogger.Instance.Warn($"Failed to update icon for '{FullPath.Value}': {ex.Message}");
        }
    }
}
