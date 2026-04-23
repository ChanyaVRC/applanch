using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Globalization;
using applanch.Localization;
using applanch.Theming;

namespace applanch.ThemeCreator;

public partial class ThemeCreatorWindow : Window
{
    private const string DefaultEditableBaseThemeId = "light";
    private const string FallbackEditableHex = "#808080";
    private const string PreviewThemeId = "themecreator-preview";
    private const string PreviewThemeFileName = "themecreator-preview.theme-palette.json";
    private const string WorkspaceMarkerFileName = "applanch.slnx";

    private readonly ThemeCreatorService _service = new();
    private readonly ObservableCollection<ThemeCreatorEditableEntry> _editableEntries = [];
    private readonly ObservableCollection<ThemeCreatorLocalizedNameEntry> _localizedNameEntries = [];
    private readonly ObservableCollection<ThemeCreatorBaseThemeChoice> _baseThemeChoices = [];

    internal static string ResolvePreviewBaseThemeId(string? selectedBaseThemeId, string? currentAppThemeId)
    {
        if (!string.IsNullOrWhiteSpace(selectedBaseThemeId))
        {
            return selectedBaseThemeId.Trim();
        }

        if (!string.IsNullOrWhiteSpace(currentAppThemeId))
        {
            var normalized = currentAppThemeId.Trim();
            return string.Equals(normalized, ThemePaletteConfigurationLoader.SystemThemeId, StringComparison.OrdinalIgnoreCase)
                ? ThemePaletteConfigurationLoader.LightThemeId
                : normalized;
        }

        return ThemePaletteConfigurationLoader.LightThemeId;
    }

    internal static string ResolvePreviewBaseThemeId(
        string? selectedBaseThemeId,
        string? currentAppThemeId,
        IReadOnlyCollection<string> availableBaseThemeIds)
    {
        ArgumentNullException.ThrowIfNull(availableBaseThemeIds);

        static bool ContainsThemeId(IReadOnlyCollection<string> themeIds, string themeId)
            => themeIds.Any(id => string.Equals(id, themeId, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(selectedBaseThemeId))
        {
            var selected = selectedBaseThemeId.Trim();
            return ContainsThemeId(availableBaseThemeIds, selected)
                ? selected
                : ThemePaletteConfigurationLoader.LightThemeId;
        }

        if (!string.IsNullOrWhiteSpace(currentAppThemeId))
        {
            var current = currentAppThemeId.Trim();
            if (string.Equals(current, ThemePaletteConfigurationLoader.SystemThemeId, StringComparison.OrdinalIgnoreCase))
            {
                return ThemePaletteConfigurationLoader.LightThemeId;
            }

            return ContainsThemeId(availableBaseThemeIds, current)
                ? current
                : ThemePaletteConfigurationLoader.LightThemeId;
        }

        return ThemePaletteConfigurationLoader.LightThemeId;
    }

    private static bool TryResolveWorkspaceRoot([NotNullWhen(true)] out string? workspaceRoot)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, WorkspaceMarkerFileName)))
            {
                workspaceRoot = directory.FullName;
                return true;
            }

            directory = directory.Parent;
        }

        workspaceRoot = null;
        return false;
    }

    private static string ReadCurrentAppThemeId()
    {
        var settingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "applanch",
            "settings.json");

        if (!File.Exists(settingsPath))
        {
            return ThemePaletteConfigurationLoader.LightThemeId;
        }

        try
        {
            using var stream = File.OpenRead(settingsPath);
            using var json = JsonDocument.Parse(stream);

            if (json.RootElement.TryGetProperty("ThemeId", out var themeIdValue) &&
                themeIdValue.ValueKind == JsonValueKind.String)
            {
                var themeId = themeIdValue.GetString();
                if (!string.IsNullOrWhiteSpace(themeId))
                {
                    return themeId.Trim();
                }
            }
        }
        catch
        {
        }

        return ThemePaletteConfigurationLoader.LightThemeId;
    }

    internal static string ResolveAppPreviewThemeDirectory(string workspaceRoot)
    {
#if DEBUG
        var frameworkName = GetCurrentTargetFrameworkMoniker();
        return Path.Combine(
            workspaceRoot,
            "src",
            "applanch",
            "bin",
            "Debug",
            frameworkName,
            "Config",
            "UserDefined",
            "theme-palette");
#else
        return Path.Combine(
            AppContext.BaseDirectory,
            "Config",
            "UserDefined",
            "theme-palette");
#endif
    }

    private static string GetCurrentTargetFrameworkMoniker()
    {
        var targetFramework = typeof(ThemeCreatorWindow).Assembly
            .GetCustomAttribute<TargetFrameworkAttribute>()
            ?.FrameworkName;

        if (string.IsNullOrWhiteSpace(targetFramework))
        {
            throw new InvalidOperationException(AppResources.GuiFrameworkResolutionFailed);
        }

        return targetFramework.Trim();
    }

    private List<ThemeEntryDto>? CollectPreviewEntries()
    {
        List<ThemeEntryDto>? entries = null;
        foreach (var editableEntry in _editableEntries)
        {
            if (string.IsNullOrWhiteSpace(editableEntry.Hex))
            {
                continue;
            }

            if (!ThemeColor.TryParse(editableEntry.Hex, out var _color))
            {
                throw new InvalidOperationException(string.Format(AppResources.GuiInvalidHexFormat, editableEntry.Key, editableEntry.Hex));
            }

            entries ??= [];
            entries.Add(new ThemeEntryDto(editableEntry.Key, ThemeColor.Parse(_color.Hex)));
        }

        return entries;
    }

    private static Process? LaunchApplanch(string workspaceRoot, string themeId)
    {
#if DEBUG
        // In dev mode, build and run from source
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project src/applanch/applanch.csproj -- --theme {themeId}",
            WorkingDirectory = workspaceRoot,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

#else
        // In release builds, applanch.exe is in the same directory as ThemeCreator
        var releaseExePath = Path.Combine(AppContext.BaseDirectory, "applanch.exe");
        var startInfo = new ProcessStartInfo
        {
            FileName = releaseExePath,
            Arguments = $"--theme {themeId}",
            UseShellExecute = false,
            CreateNoWindow = true,
        };

#endif
        return Process.Start(startInfo);
    }
    private bool _isUpdatingOutputPath;
    private bool _isOutputPathCustomized;
    private string? _lastSuggestedOutputPath;
    private string? _defaultEditableEntriesSourcePath;
    private Dictionary<string, string>? _defaultEditableHexByKey;
    private ThemePreviewWindow? _previewWindow;

    public ThemeCreatorWindow()
    {
        InitializeComponent();
        EntriesDataGrid.ItemsSource = _editableEntries;
        LocalizedNamesItemsControl.ItemsSource = _localizedNameEntries;
        BaseThemeComboBox.ItemsSource = _baseThemeChoices;
        InitializeLocalizedNameEntries();
        SourcePathTextBox.Text = ThemeCreatorPathResolver.ResolveDefaultSourcePath();
        IncludeEntriesCheckBox.IsChecked = true;
        Loaded += OnLoaded;
        Closed += OnClosed;
        UpdateDefaultOutputPath();
        ThemeCreatorLogger.Info($"Theme creator window initialized. SourcePath={SourcePathTextBox.Text}");
        ReloadThemes();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ThemeCreatorLogger.Info("Theme creator window loaded.");
        UpdateEntryEditorState();
        UpdateSectionStates();
        RefreshPreview();
    }

    private void CreationModeChanged(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded)
        {
            return;
        }

        InvalidateDefaultEditableEntriesCache();

        if (!_isOutputPathCustomized)
        {
            UpdateDefaultOutputPath();
        }

        ReloadThemes();
        UpdateSectionStates();
        RefreshPreview();
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        if (_previewWindow is not null)
        {
            _previewWindow.Close();
            _previewWindow = null;
        }

        ThemeCreatorLogger.Info("Theme creator window closed.");
    }

    private void SourcePathChanged(object sender, TextChangedEventArgs e)
    {
        if (!IsLoaded)
        {
            return;
        }

        InvalidateDefaultEditableEntriesCache();

        if (!_isOutputPathCustomized)
        {
            UpdateDefaultOutputPath();
        }

        UpdateSectionStates();
        RefreshPreview();
    }

    private void BaseThemeSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded)
        {
            return;
        }

        ReloadEditableEntries();
        UpdateSectionStates();
        RefreshPreview();
    }

    private void ThemeIdChanged(object sender, TextChangedEventArgs e)
    {
        if (!IsLoaded)
        {
            return;
        }

        if (!_isOutputPathCustomized)
        {
            UpdateDefaultOutputPath();
        }

        UpdateSectionStates();
        RefreshPreview();
    }

    private void MetadataChanged(object sender, RoutedEventArgs e)
    {
        UpdateEntryEditorState();
        UpdateSectionStates();
        RefreshPreview();
    }

    private void NameEntryChanged(object sender, TextChangedEventArgs e)
        => MetadataChanged(sender, e);

    private void OutputPathChanged(object sender, TextChangedEventArgs e)
    {
        TrackOutputPathCustomization();
        RefreshPreview();
    }

    private void BrowseSource_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = AppResources.SourceFileFilter,
            FileName = SourcePathTextBox.Text
        };

        if (dialog.ShowDialog(this) == true)
        {
            SourcePathTextBox.Text = dialog.FileName;
            _isOutputPathCustomized = false;
            UpdateDefaultOutputPath();
            ThemeCreatorLogger.Info($"Source palette selected. Path={dialog.FileName}");
            ReloadThemes();
        }
    }

    private void BrowseOutput_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = AppResources.OutputFileFilter,
            FileName = string.IsNullOrWhiteSpace(OutputPathTextBox.Text) ? "theme.json" : OutputPathTextBox.Text,
            DefaultExt = ".json"
        };

        if (dialog.ShowDialog(this) == true)
        {
            OutputPathTextBox.Text = dialog.FileName;
            _isOutputPathCustomized = true;
            ThemeCreatorLogger.Info($"Output path selected. Path={dialog.FileName}");
        }
    }

    private void ReloadSource_Click(object sender, RoutedEventArgs e)
        => ReloadThemes();

    private void OpenPreview_Click(object sender, RoutedEventArgs e)
    {
        if (_previewWindow is null)
        {
            _previewWindow = new ThemePreviewWindow
            {
                Owner = this
            };
            _previewWindow.Closed += PreviewWindowClosed;
        }

        if (!_previewWindow.IsVisible)
        {
            _previewWindow.Show();
        }

        _previewWindow.Activate();
        RefreshPreview();
    }

    private void OpenAppPreview_Click(object sender, RoutedEventArgs e)
    {
        if (!TryBuildOptions(out var options, out var errorMessage))
        {
            ShowError(errorMessage);
            return;
        }

        if (!TryResolveWorkspaceRoot(out var workspaceRoot))
        {
            ShowError(AppResources.AppPreviewWorkspaceNotFoundError);
            return;
        }

        try
        {
            var sourcePalettePath = ThemeCreatorPathResolver.ResolveDefaultSourcePath();
            if (string.IsNullOrWhiteSpace(sourcePalettePath) || !File.Exists(sourcePalettePath))
            {
                ShowError(string.Format(CultureInfo.CurrentCulture, AppResources.GuiSourceNotFoundFormat, sourcePalettePath ?? string.Empty));
                return;
            }

            var availableThemeIds = _service.GetThemeIdsFromFile(sourcePalettePath);

            var previewThemePath = Path.Combine(
                ResolveAppPreviewThemeDirectory(workspaceRoot),
                PreviewThemeFileName);
            Directory.CreateDirectory(Path.GetDirectoryName(previewThemePath)!);

            var previewEntries = CollectPreviewEntries();
            var currentThemeId = ReadCurrentAppThemeId();
            var previewBaseThemeId = ResolvePreviewBaseThemeId(options.BaseThemeId, currentThemeId, availableThemeIds);

            var previewOptions = options with
            {
                ThemeId = PreviewThemeId,
                SourcePalettePath = sourcePalettePath,
                OutputPath = previewThemePath,
                BaseThemeId = previewBaseThemeId,
                IncludeEntries = true,
                Entries = previewEntries,
            };

            _service.CreateThemeFile(previewOptions);
            var process = LaunchApplanch(workspaceRoot, PreviewThemeId);

            // Monitor process exit (no theme restoration needed with command-line args)
            if (process is not null)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await process.WaitForExitAsync();
                    }
                    catch
                    {
                    }
                });
            }

            StatusTextBlock.Text = string.Format(
                CultureInfo.CurrentCulture,
                AppResources.AppPreviewStarted,
                PreviewThemeId,
                previewBaseThemeId);
            StatusTextBlock.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            ShowError(string.Format(CultureInfo.CurrentCulture, AppResources.AppPreviewLaunchFailed, ex.Message));
        }
    }

    private void PreviewWindowClosed(object? sender, EventArgs e)
        => _previewWindow = null;

    private void Create_Click(object sender, RoutedEventArgs e)
    {
        if (!TryBuildOptions(out var options, out var errorMessage))
        {
            ThemeCreatorLogger.Warn($"Create requested with invalid options. Error={errorMessage}");
            ShowError(errorMessage);
            return;
        }

        try
        {
            ThemeCreatorLogger.Info(
                $"Creating theme from GUI. ThemeId={options.ThemeId}, BaseThemeId={options.BaseThemeId}, IncludeEntries={options.IncludeEntries}, OutputPath={options.OutputPath}");
            _service.CreateThemeFile(options);
            StatusTextBlock.Text = string.Format(AppResources.GuiSuccessMessageFormat, options.OutputPath);
            ThemeCreatorLogger.Info($"GUI theme created successfully. OutputPath={options.OutputPath}");
            System.Windows.MessageBox.Show(this, StatusTextBlock.Text, AppResources.SuccessCaption, MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            ThemeCreatorLogger.Error(ex, "GUI theme creation failed");
            ShowError(ex.Message);
        }
    }

    private void ReloadThemes()
    {
        if (!IsSourcePaletteMode())
        {
            PopulateBaseThemeChoices([]);
            BaseThemeComboBox.SelectedItem = _baseThemeChoices.FirstOrDefault();
            PopulateDefaultEditableEntries();
            StatusTextBlock.Text = string.Empty;
            ThemeCreatorLogger.Info("Theme list reset for no-source creation mode.");
            UpdateEntryEditorState();
            RefreshPreview();
            return;
        }

        try
        {
            var selectedBaseThemeId = (BaseThemeComboBox.SelectedItem as ThemeCreatorBaseThemeChoice)?.Id;
            var themeIds = _service.GetThemeIdsFromFile(SourcePathTextBox.Text);
            PopulateBaseThemeChoices(themeIds);

            BaseThemeComboBox.SelectedItem = _baseThemeChoices.FirstOrDefault(choice =>
                string.Equals(choice.Id, selectedBaseThemeId, StringComparison.OrdinalIgnoreCase))
                ?? _baseThemeChoices.FirstOrDefault();
            StatusTextBlock.Text = string.Empty;
            ThemeCreatorLogger.Info($"Theme list reloaded successfully. SourcePath={SourcePathTextBox.Text}, ThemeCount={themeIds.Count}");
            ReloadEditableEntries();
        }
        catch (Exception ex)
        {
            _baseThemeChoices.Clear();
            BaseThemeComboBox.SelectedItem = null;
            StatusTextBlock.Text = ex.Message;
            _editableEntries.Clear();
            ThemeCreatorLogger.Warn(ex, $"Theme list reload failed. SourcePath={SourcePathTextBox.Text}");
        }

        RefreshPreview();
    }

    private void ReloadEditableEntries()
    {
        var baseThemeId = (BaseThemeComboBox.SelectedItem as ThemeCreatorBaseThemeChoice)?.Id;

        if (!IsSourcePaletteMode())
        {
            PopulateDefaultEditableEntries();
            UpdateEntryEditorState();
            return;
        }

        _editableEntries.Clear();

        if (string.IsNullOrWhiteSpace(SourcePathTextBox.Text))
        {
            UpdateEntryEditorState();
            return;
        }

        if (string.IsNullOrWhiteSpace(baseThemeId))
        {
            PopulateDefaultEditableEntries();
            StatusTextBlock.Text = string.Empty;
            UpdateEntryEditorState();
            return;
        }

        try
        {
            foreach (var entry in _service.GetEntriesFromFile(SourcePathTextBox.Text, baseThemeId))
            {
                if (string.IsNullOrWhiteSpace(entry.Key))
                {
                    continue;
                }

                _editableEntries.Add(new ThemeCreatorEditableEntry(
                    entry.Key,
                    ThemeBrushReferenceCatalog.GetDescription(entry.Key),
                    entry.Hex.Hex));
            }

            if (_editableEntries.Count == 0 && IncludeEntriesCheckBox.IsChecked != false)
            {
                StatusTextBlock.Text = AppResources.GuiNoEditableEntries;
            }
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = ex.Message;
            ThemeCreatorLogger.Warn(ex, $"Editable entries reload failed. BaseThemeId={BaseThemeComboBox.SelectedItem}");
        }

        UpdateEntryEditorState();
    }

    private void RefreshPreview()
    {
        if (!IsLoaded)
        {
            return;
        }

        if (!TryBuildOptions(out var options, out var errorMessage))
        {
            UpdatePreviewText(errorMessage);
            StatusTextBlock.Text = errorMessage;
            return;
        }

        try
        {
            UpdatePreviewText(_service.CreateThemeJsonFromFile(options));
            if (!string.IsNullOrEmpty(StatusTextBlock.Text))
            {
                StatusTextBlock.Text = string.Empty;
            }
        }
        catch (Exception ex)
        {
            UpdatePreviewText(ex.Message);
            StatusTextBlock.Text = ex.Message;
            ThemeCreatorLogger.Warn(ex, $"Preview refresh failed. ThemeId={options.ThemeId}, OutputPath={options.OutputPath}");
        }
    }

    private void EntryHexChanged(object sender, TextChangedEventArgs e)
        => RefreshPreview();

    private void PickEntryColor_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement element || element.DataContext is not ThemeCreatorEditableEntry entry)
        {
            return;
        }

        var picker = new ColorPickerWindow(entry.Hex) { Owner = this };
        if (picker.ShowDialog() == true)
        {
            entry.Hex = picker.SelectedHex;
            RefreshPreview();
        }
    }

    private bool TryBuildOptions(out ThemeCreatorOptions options, out string errorMessage)
    {
        var sourcePath = SourcePathTextBox.Text.Trim();
        var themeId = ThemeIdTextBox.Text.Trim();
        var outputPath = OutputPathTextBox.Text.Trim();
        var baseThemeId = (BaseThemeComboBox.SelectedItem as ThemeCreatorBaseThemeChoice)?.Id;
        var includeEntries = IncludeEntriesCheckBox.IsChecked != false;
        var useSourcePalette = IsSourcePaletteMode();

        if (useSourcePalette && (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath)))
        {
            errorMessage = string.Format(AppResources.GuiSourceNotFoundFormat, sourcePath);
            options = null!;
            return false;
        }

        if (string.IsNullOrWhiteSpace(themeId))
        {
            errorMessage = AppResources.GuiThemeIdRequired;
            options = null!;
            return false;
        }

        if (string.IsNullOrWhiteSpace(outputPath))
        {
            errorMessage = AppResources.GuiOutputRequired;
            options = null!;
            return false;
        }

        List<ThemeEntryDto>? entries = null;
        if (includeEntries)
        {
            entries = [];
            foreach (var editableEntry in _editableEntries)
            {
                if (string.IsNullOrWhiteSpace(editableEntry.Hex))
                {
                    continue;
                }

                if (!ThemeColor.TryParse(editableEntry.Hex, out var _color))
                {
                    errorMessage = string.Format(AppResources.GuiInvalidHexFormat, editableEntry.Key, editableEntry.Hex);
                    options = null!;
                    return false;
                }

                entries.Add(new ThemeEntryDto(editableEntry.Key, ThemeColor.Parse(_color.Hex)));
            }

            if (entries.Count == 0)
            {
                entries = null;
            }
        }

        options = new ThemeCreatorOptions(
            themeId,
            outputPath,
            baseThemeId,
            useSourcePalette ? sourcePath : null,
            BuildDisplayNames(),
            includeEntries,
            entries);
        errorMessage = string.Empty;
        return true;
    }

    private Dictionary<LanguageOption, string>? BuildDisplayNames()
    {
        var displayNames = new Dictionary<LanguageOption, string>();
        foreach (var entry in _localizedNameEntries)
        {
            var trimmedValue = entry.Value.Trim();
            if (string.IsNullOrWhiteSpace(trimmedValue))
            {
                continue;
            }

            displayNames[entry.Language] = trimmedValue;
        }

        return displayNames.Count == 0
            ? null
            : displayNames;
    }

    private void InitializeLocalizedNameEntries()
    {
        _localizedNameEntries.Clear();
        var seenLanguageCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var culture in LanguageOption.EnumerateSupportedCultures(includeInvariantCulture: false))
        {
            if (!LanguageOption.TryMapFromCultureCode(culture.TwoLetterISOLanguageName, out var language))
            {
                continue;
            }

            if (!seenLanguageCodes.Add(language.Code))
            {
                continue;
            }

            _localizedNameEntries.Add(new ThemeCreatorLocalizedNameEntry(language));
        }
    }

    private void UpdatePreviewText(string value)
    {
        _previewWindow?.SetPreviewText(value);
    }

    private void UpdateDefaultOutputPath()
    {
        var suggestedPath = ThemeCreatorPathResolver.ResolveDefaultOutputPath(GetEffectiveSourcePathForOutput(), ThemeIdTextBox.Text.Trim());
        _lastSuggestedOutputPath = suggestedPath;

        if (_isOutputPathCustomized)
        {
            return;
        }

        _isUpdatingOutputPath = true;
        OutputPathTextBox.Text = suggestedPath;
        _isUpdatingOutputPath = false;
    }

    private void TrackOutputPathCustomization()
    {
        if (_isUpdatingOutputPath)
        {
            return;
        }

        _isOutputPathCustomized = !string.IsNullOrWhiteSpace(OutputPathTextBox.Text) &&
            !string.Equals(OutputPathTextBox.Text, _lastSuggestedOutputPath, StringComparison.OrdinalIgnoreCase);
    }

    private void UpdateEntryEditorState()
    {
        if (EntriesDataGrid is null || IncludeEntriesCheckBox is null)
        {
            return;
        }

        var hasEntries = _editableEntries.Count > 0;

        IncludeEntriesCheckBox.IsEnabled = hasEntries;
        if (!hasEntries)
        {
            IncludeEntriesCheckBox.IsChecked = false;
        }

        EntriesDataGrid.IsEnabled = hasEntries && IncludeEntriesCheckBox.IsChecked != false;
    }

    private void PopulateBaseThemeChoices(IReadOnlyList<string> themeIds)
    {
        _baseThemeChoices.Clear();
        _baseThemeChoices.Add(new ThemeCreatorBaseThemeChoice(null, AppResources.BaseThemeNoneOption));
        foreach (var themeId in themeIds)
        {
            _baseThemeChoices.Add(new ThemeCreatorBaseThemeChoice(themeId, themeId));
        }
    }

    private void PopulateDefaultEditableEntries()
    {
        var defaultHexByKey = ResolveDefaultEditableHexByKey();

        _editableEntries.Clear();
        foreach (var key in ThemeBrushReferenceCatalog.Keys)
        {
            _editableEntries.Add(new ThemeCreatorEditableEntry(
                key,
                ThemeBrushReferenceCatalog.GetDescription(key),
                defaultHexByKey.TryGetValue(key, out var hex)
                    ? hex
                    : FallbackEditableHex));
        }
    }

    private Dictionary<string, string> ResolveDefaultEditableHexByKey()
    {
        var sourcePalettePath = ResolveSourcePalettePathForDefaultEntries();

        if (string.IsNullOrWhiteSpace(sourcePalettePath) || !File.Exists(sourcePalettePath))
        {
            return [];
        }

        if (string.Equals(_defaultEditableEntriesSourcePath, sourcePalettePath, StringComparison.OrdinalIgnoreCase) &&
            _defaultEditableHexByKey is not null)
        {
            return _defaultEditableHexByKey;
        }

        try
        {
            var themeIds = _service.GetThemeIdsFromFile(sourcePalettePath);
            var preferredThemeId = themeIds.FirstOrDefault(themeId =>
                                     string.Equals(themeId, DefaultEditableBaseThemeId, StringComparison.OrdinalIgnoreCase))
                                 ?? themeIds.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(preferredThemeId))
            {
                return [];
            }

            var entries = _service.GetEntriesFromFile(sourcePalettePath, preferredThemeId);
            _defaultEditableEntriesSourcePath = sourcePalettePath;
            _defaultEditableHexByKey = entries
                .Where(static entry => !string.IsNullOrWhiteSpace(entry.Key))
                .GroupBy(static entry => entry.Key, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    static group => group.Key,
                    static group => group.First().Hex.Hex,
                    StringComparer.OrdinalIgnoreCase);

            return _defaultEditableHexByKey;
        }
        catch (Exception ex)
        {
            ThemeCreatorLogger.Warn(ex, $"Resolving default editable entries failed. SourcePath={sourcePalettePath}");
            return [];
        }
    }

    private string ResolveSourcePalettePathForDefaultEntries()
    {
        if (IsSourcePaletteMode())
        {
            return SourcePathTextBox.Text.Trim();
        }

        return ThemeCreatorPathResolver.ResolveDefaultSourcePath();
    }

    private void InvalidateDefaultEditableEntriesCache()
    {
        _defaultEditableEntriesSourcePath = null;
        _defaultEditableHexByKey = null;
    }

    private void UpdateSectionStates()
    {
        if (!IsLoaded)
        {
            return;
        }

        var sourceMode = IsSourcePaletteMode();
        var sourceExists = !string.IsNullOrWhiteSpace(SourcePathTextBox.Text) && File.Exists(SourcePathTextBox.Text);
        var sourceReady = !sourceMode || sourceExists;
        var themeIdEntered = !string.IsNullOrWhiteSpace(ThemeIdTextBox.Text);
        var outputPathEntered = !string.IsNullOrWhiteSpace(OutputPathTextBox.Text);

        SourcePanel.Visibility = sourceMode ? Visibility.Visible : Visibility.Collapsed;
        BaseThemeComboBox.IsEnabled = sourceReady;
        CreateButton.IsEnabled = sourceReady && themeIdEntered && outputPathEntered;
        OpenAppPreviewButton.IsEnabled = sourceReady && themeIdEntered;
    }

    private bool IsSourcePaletteMode()
        => UseSourcePaletteRadioButton.IsChecked != false;

    private string GetEffectiveSourcePathForOutput()
    {
        if (IsSourcePaletteMode())
        {
            return SourcePathTextBox.Text.Trim();
        }

        return ThemeCreatorPathResolver.ResolveDefaultSourcePath();
    }

    private void ShowError(string message)
    {
        StatusTextBlock.Text = message;
        System.Windows.MessageBox.Show(this, message, AppResources.ErrorCaption, MessageBoxButton.OK, MessageBoxImage.Error);
    }
}


