using System.Windows;
using System.Windows.Controls;
using applanch.Infrastructure.Dialogs;
using applanch.Infrastructure.Storage;
using applanch.Tests.TestSupport;
using applanch.ViewModels;
using Xunit;

namespace applanch.Tests.Infrastructure.Dialogs;

[Collection("WpfTests")]
public class LaunchItemContextMenuHandlerTests
{
    [Fact]
    public void GetTargetItem_ReturnsDataContextItem()
    {
        WpfTestHost.RunInSta(() =>
        {
            var item = new LaunchItemViewModel(new applanch.Infrastructure.Utilities.LaunchPath("path"), "Dev", string.Empty, "App");
            var sender = BuildSender(item);

            var result = LaunchItemContextMenuHandler.GetTargetItem(sender);

            Assert.Same(item, result);
        });
    }

    [Fact]
    public void EditCategory_AppliesPromptResult()
    {
        WpfTestHost.RunInSta(() =>
        {
            var item = new LaunchItemViewModel(new applanch.Infrastructure.Utilities.LaunchPath("path"), "Dev", string.Empty, "App");
            var sender = BuildSender(item);
            var interaction = new FakeUserInteractionService
            {
                PromptWithSuggestionsResult = Category.FromInput("Ops"),
            };
            var owner = new Window();
            var sut = new LaunchItemContextMenuHandler(interaction, owner);

            Category? applied = null;
            sut.EditCategory(
                sender,
                new[] { Category.FromInput("Dev"), Category.FromInput("Ops"), Category.FromInput(""), Category.FromInput("Ops") },
                "prompt",
                (_, value) => applied = value);

            Assert.NotNull(applied);
            Assert.Equal("Ops", applied.Value.Value);
            Assert.Equal("Dev", interaction.LastPromptWithSuggestionsInitialValueText);
            Assert.Equal(new[] { "Dev", "Ops", AppResources.DefaultCategory }, interaction.LastSuggestions);
        });
    }

    [Fact]
    public void EditValue_WhenPromptReturnsNull_DoesNotApply()
    {
        WpfTestHost.RunInSta(() =>
        {
            var item = new LaunchItemViewModel(new applanch.Infrastructure.Utilities.LaunchPath("path"), "Dev", "-a", "App");
            var sender = BuildSender(item);
            var interaction = new FakeUserInteractionService
            {
                PromptResult = null,
            };
            var sut = new LaunchItemContextMenuHandler(interaction, new Window());

            var called = false;
            sut.EditValue(sender, "title", static x => x.Arguments, (_, _) => called = true);

            Assert.False(called);
        });
    }

    [Fact]
    public void BeginRename_SetsEditingState()
    {
        WpfTestHost.RunInSta(() =>
        {
            var item = new LaunchItemViewModel(new applanch.Infrastructure.Utilities.LaunchPath("path"), "Dev", string.Empty, "App");
            var sender = BuildSender(item);
            var sut = new LaunchItemContextMenuHandler(new FakeUserInteractionService(), new Window());

            sut.BeginRename(sender);

            Assert.True(item.IsRenaming);
            Assert.Equal("App", item.EditingName);
        });
    }

    [Fact]
    public void Delete_InvokesRemoveAction()
    {
        WpfTestHost.RunInSta(() =>
        {
            var item = new LaunchItemViewModel(new applanch.Infrastructure.Utilities.LaunchPath("path"), "Dev", string.Empty, "App");
            var sender = BuildSender(item);
            var sut = new LaunchItemContextMenuHandler(new FakeUserInteractionService(), new Window());

            LaunchItemViewModel? removed = null;
            sut.Delete(sender, x => removed = x);

            Assert.Same(item, removed);
        });
    }

    private static MenuItem BuildSender(LaunchItemViewModel item)
    {
        var menuItem = new MenuItem();
        var contextMenu = new ContextMenu
        {
            PlacementTarget = new Border { DataContext = item },
        };
        contextMenu.Items.Add(menuItem);
        return menuItem;
    }

    private sealed class FakeUserInteractionService : IUserInteractionService
    {
        internal string? PromptResult { get; init; } = "value";
        internal object? PromptWithSuggestionsResult { get; init; } = "value";
        internal string LastPromptWithSuggestionsInitialValueText { get; private set; } = string.Empty;
        internal string[] LastSuggestions { get; private set; } = [];

        public void Show(string message, string caption, MessageBoxImage icon)
        {
        }

        public bool Confirm(string message, string caption, Window owner)
        {
            return true;
        }

        public string? Prompt(string title, string initialValue, Window owner)
        {
            return PromptResult;
        }

        public PromptResult<string>? PromptWithSuggestions(string title, string initialValue, IEnumerable<string> suggestions, Window owner)
        {
            LastPromptWithSuggestionsInitialValueText = initialValue;
            LastSuggestions = suggestions
                .Where(static value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            if (PromptWithSuggestionsResult is not string stringResult)
            {
                return null;
            }

            var selectedSuggestion = LastSuggestions.FirstOrDefault(value => string.Equals(value, stringResult, StringComparison.Ordinal));
            return new PromptResult<string>(stringResult, selectedSuggestion);
        }

        public PromptResult<T?>? PromptWithSuggestions<T>(string title, T initialValue, IEnumerable<T> suggestions, Window owner)
        {
            LastPromptWithSuggestionsInitialValueText = initialValue?.ToString() ?? string.Empty;
            LastSuggestions = suggestions
                .Select(static value => value?.ToString() ?? string.Empty)
                .Where(static value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            if (PromptWithSuggestionsResult is null)
            {
                return null;
            }

            if (PromptWithSuggestionsResult is T typedResult)
            {
                return new PromptResult<T?>(typedResult?.ToString() ?? string.Empty, typedResult);
            }

            if (PromptWithSuggestionsResult is string stringResult)
            {
                var selectedSuggestion = LastSuggestions.FirstOrDefault(value => string.Equals(value, stringResult, StringComparison.Ordinal));
                var selectedItem = suggestions.FirstOrDefault(value => string.Equals(value?.ToString(), selectedSuggestion, StringComparison.Ordinal));
                return new PromptResult<T?>(stringResult, selectedItem);
            }

            throw new InvalidOperationException($"Unsupported prompt result type: {PromptWithSuggestionsResult.GetType().FullName}.");
        }
    }

}
