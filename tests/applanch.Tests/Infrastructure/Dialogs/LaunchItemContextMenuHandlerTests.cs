using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls;
using applanch.Infrastructure.Dialogs;
using applanch.ViewModels;
using Xunit;

namespace applanch.Tests.Infrastructure.Dialogs;

public class LaunchItemContextMenuHandlerTests
{
    [Fact]
    public void GetTargetItem_ReturnsDataContextItem()
    {
        RunInSta(() =>
        {
            var item = new LaunchItemViewModel("path", "Dev", string.Empty, "App");
            var sender = BuildSender(item);

            var result = LaunchItemContextMenuHandler.GetTargetItem(sender);

            Assert.Same(item, result);
        });
    }

    [Fact]
    public void EditCategory_AppliesPromptResult()
    {
        RunInSta(() =>
        {
            var item = new LaunchItemViewModel("path", "Dev", string.Empty, "App");
            var sender = BuildSender(item);
            var interaction = new FakeUserInteractionService
            {
                PromptWithSuggestionsResult = "Ops",
            };
            var owner = new Window();
            var sut = new LaunchItemContextMenuHandler(interaction, owner);

            string? applied = null;
            sut.EditCategory(
                sender,
                ["Dev", "Ops", "", "Ops"],
                "prompt",
                (_, value) => applied = value);

            Assert.Equal("Ops", applied);
            Assert.Equal("Dev", interaction.LastPromptWithSuggestionsInitialValue);
            Assert.Equal(new[] { "Dev", "Ops" }, interaction.LastSuggestions);
        });
    }

    [Fact]
    public void EditValue_WhenPromptReturnsNull_DoesNotApply()
    {
        RunInSta(() =>
        {
            var item = new LaunchItemViewModel("path", "Dev", "-a", "App");
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
        RunInSta(() =>
        {
            var item = new LaunchItemViewModel("path", "Dev", string.Empty, "App");
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
        RunInSta(() =>
        {
            var item = new LaunchItemViewModel("path", "Dev", string.Empty, "App");
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
        internal string? PromptWithSuggestionsResult { get; init; } = "value";
        internal string LastPromptWithSuggestionsInitialValue { get; private set; } = string.Empty;
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

        public string? PromptWithSuggestions(string title, string initialValue, IEnumerable<string> suggestions, Window owner)
        {
            LastPromptWithSuggestionsInitialValue = initialValue;
            LastSuggestions = suggestions.ToArray();
            return PromptWithSuggestionsResult;
        }
    }

    private static void RunInSta(Action action)
    {
        Exception? captured = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                captured = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (captured is not null)
        {
            ExceptionDispatchInfo.Capture(captured).Throw();
        }
    }
}
