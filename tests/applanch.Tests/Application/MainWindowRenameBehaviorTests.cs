using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using applanch.Infrastructure.Dialogs;
using applanch.Infrastructure.Launch;
using applanch.Infrastructure.Resolution;
using applanch.Infrastructure.Storage;
using applanch.Infrastructure.Updates;
using applanch.Infrastructure.Utilities;
using applanch.Tests.TestSupport;
using applanch.ViewModels;
using Xunit;

namespace applanch.Tests.Application;

public sealed class MainWindowRenameBehaviorTests
{
    [Fact]
    public void ContextMenuRename_WhenIconModeEnabled_UsesPromptDialogAndUpdatesDisplayName()
    {
        WpfTestHost.RunInSta((Action)(() =>
        {
            WpfTestHost.EnsureAppResources();

            VerifyRenameBehavior(
                iconModeEnabled: true,
                expectedPromptCalls: 1,
                expectedDisplayName: "Renamed by dialog",
                expectedInlineRename: false,
                promptValue: "Renamed by dialog");
        }));
    }

    private static void VerifyRenameBehavior(
        bool iconModeEnabled,
        int expectedPromptCalls,
        string expectedDisplayName,
        bool expectedInlineRename,
        string promptValue)
    {
        var settings = new AppSettings
        {
            LaunchItemIconOnlyMode = iconModeEnabled,
            CheckForUpdatesOnStartup = false,
        };

        var viewModel = new MainWindowViewModel(
            new FakeResolver(),
            new FakeStore(),
            settings);

        var interaction = new FakeInteractionService(promptValue);

        var window = new MainWindow(
            viewModel,
            new FakeLaunchService(),
            interaction,
            static _ => new FakeUpdateService(),
            settings);

        try
        {
            var item = viewModel.LaunchItems[0];
            InvokeRenameMenu(window, item);

            Assert.Equal(expectedPromptCalls, interaction.PromptCallCount);
            Assert.Equal(expectedDisplayName, item.DisplayName);
            Assert.Equal(expectedInlineRename, item.IsRenaming);
            if (expectedInlineRename)
            {
                Assert.Equal(item.DisplayName, item.EditingName);
            }
        }
        finally
        {
            window.Close();
            WpfTestHost.DoEvents();
        }
    }

    private static void InvokeRenameMenu(MainWindow window, LaunchItemViewModel item)
    {
        var placementTarget = new Border { DataContext = item };
        var contextMenu = new ContextMenu { PlacementTarget = placementTarget };
        var menuItem = new MenuItem { Tag = LaunchItemContextMenuAction.Rename };
        _ = contextMenu.Items.Add(menuItem);

        var clickHandler = typeof(MainWindow).GetMethod(
            "ContextMenu_Item_Click",
            BindingFlags.Instance | BindingFlags.NonPublic);

        clickHandler!.Invoke(window, [menuItem, new RoutedEventArgs(MenuItem.ClickEvent)]);
    }

    private sealed class FakeStore : ILauncherStore
    {
        public IReadOnlyList<LauncherEntry> LoadAll()
        {
            return
            [
                new LauncherEntry(new LaunchPath(@"C:\Tools\App.exe"), LauncherEntry.DefaultCategory, string.Empty, "App")
            ];
        }

        public void SaveAll(IEnumerable<LauncherEntry> entries)
        {
        }
    }

    private sealed class FakeResolver : IAppResolver
    {
        public bool TryResolve(string input, out ResolvedApp resolved)
        {
            resolved = default!;
            return false;
        }

        public IReadOnlyList<string> GetSuggestions(string input, int maxResults = 8)
        {
            return [];
        }
    }

    private sealed class FakeLaunchService : IItemLaunchService
    {
        public LaunchExecutionResult TryLaunch(LaunchPath launchPath, string arguments, bool runAsAdministrator = false)
        {
            return LaunchExecutionResult.Success();
        }
    }

    private sealed class FakeInteractionService(string promptValue) : IUserInteractionService
    {
        public int PromptCallCount { get; private set; }

        public void Show(string message, string caption, MessageBoxImage icon)
        {
        }

        public bool Confirm(string message, string caption, Window owner)
        {
            return true;
        }

        public string? Prompt(string title, string initialValue, Window owner)
        {
            PromptCallCount++;
            return promptValue;
        }

        public string? PromptWithSuggestions(string title, string initialValue, IEnumerable<string> suggestions, Window owner)
        {
            return initialValue;
        }
    }

    private sealed class FakeUpdateService : IAppUpdateService
    {
        public Task<AppUpdateInfo?> CheckForUpdateAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<AppUpdateInfo?>(null);
        }

        public Task ApplyUpdateAsync(AppUpdateInfo update, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
