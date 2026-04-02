using Xunit;
using System.Windows;
using System.Globalization;
using System.Windows.Media;
using applanch.Infrastructure.Integration;
using applanch.Infrastructure.Resolution;
using applanch.Infrastructure.Storage;
using applanch.Tests.ViewModels.TestDoubles;
using applanch.ViewModels;

namespace applanch.Tests.ViewModels;

public class MainWindowViewModelTests
{
    [Fact]
    public void TryAddQuickItem_EmptyInput_ReturnsInformationFailure()
    {
        var vm = CreateViewModel();
        vm.QuickAddNameOrPath = "   ";

        var result = vm.TryAddQuickItem();

        Assert.False(result.IsSuccess);
        Assert.Equal(QuickAddMessageSeverity.Information, result.Severity);
        Assert.Empty(vm.LaunchItems);
    }

    [Fact]
    public void TryAddQuickItem_UnresolvedInput_ReturnsWarningFailure()
    {
        var resolver = new FakeResolver();
        var vm = CreateViewModel(resolver: resolver);
        vm.QuickAddNameOrPath = "unknown-app";

        var result = vm.TryAddQuickItem();

        Assert.False(result.IsSuccess);
        Assert.Equal(QuickAddMessageSeverity.Warning, result.Severity);
        Assert.Empty(vm.LaunchItems);
    }

    [Fact]
    public void TryAddQuickItem_DuplicatePath_ReturnsInformationFailure()
    {
        var existingPath = @"C:\\Tools\\App.exe";
        var store = new FakeStore(
        [
            new LauncherStore.LauncherEntry(existingPath, "Dev", string.Empty, "App")
        ]);

        var resolver = new FakeResolver
        {
            ShouldResolve = true,
            ResolvedApp = new ResolvedApp(existingPath, "App")
        };

        var vm = CreateViewModel(store, resolver);
        vm.QuickAddNameOrPath = "app";

        var result = vm.TryAddQuickItem();

        Assert.False(result.IsSuccess);
        Assert.Equal(QuickAddMessageSeverity.Information, result.Severity);
        Assert.Single(vm.LaunchItems);
    }

    [Fact]
    public void TryAddQuickItem_Success_AddsItemAndResetsFields()
    {
        var resolver = new FakeResolver
        {
            ShouldResolve = true,
            ResolvedApp = new ResolvedApp(@"C:\\Tools\\NewApp.exe", "NewApp")
        };

        var vm = CreateViewModel(resolver: resolver);
        vm.QuickAddNameOrPath = "newapp";
        vm.QuickAddCategory = "Utilities";
        vm.QuickAddArguments = "-v";

        var result = vm.TryAddQuickItem();

        Assert.True(result.IsSuccess);
        Assert.Single(vm.LaunchItems);
        Assert.Equal("NewApp", vm.LaunchItems[0].DisplayName);
        Assert.Equal(string.Empty, vm.QuickAddNameOrPath);
        Assert.Equal(string.Empty, vm.QuickAddArguments);
    }

    [Fact]
    public void PreviewMoveItem_DoesNotPersist_UntilCommit()
    {
        var store = new FakeStore(
        [
            new LauncherStore.LauncherEntry(@"C:\\Tools\\A.exe", "Dev", string.Empty, "A"),
            new LauncherStore.LauncherEntry(@"C:\\Tools\\B.exe", "Dev", string.Empty, "B")
        ]);

        var vm = CreateViewModel(store: store);

        vm.PreviewMoveItem(0, 1);

        Assert.Equal(0, store.SaveCallCount);
        Assert.Equal("B", vm.LaunchItems[0].DisplayName);
        Assert.Equal("A", vm.LaunchItems[1].DisplayName);

        vm.PersistOrderNow();

        Assert.Equal(1, store.SaveCallCount);
        Assert.Collection(
            store.LastSavedEntries,
            static entry => Assert.Equal("B", entry.DisplayName),
            static entry => Assert.Equal("A", entry.DisplayName));
    }

    [Fact]
    public void PreviewMoveItem_InvalidIndices_AreIgnored()
    {
        var store = new FakeStore(
        [
            new LauncherStore.LauncherEntry(@"C:\\Tools\\A.exe", "Dev", string.Empty, "A"),
            new LauncherStore.LauncherEntry(@"C:\\Tools\\B.exe", "Dev", string.Empty, "B")
        ]);

        var vm = CreateViewModel(store: store);

        vm.PreviewMoveItem(-1, 0);
        vm.PreviewMoveItem(0, 99);

        Assert.Equal(0, store.SaveCallCount);
        Assert.Equal("A", vm.LaunchItems[0].DisplayName);
        Assert.Equal("B", vm.LaunchItems[1].DisplayName);
    }

    [Fact]
    public void RemoveItem_PersistsImmediately()
    {
        var store = new FakeStore(
        [
            new LauncherStore.LauncherEntry(@"C:\\Tools\\A.exe", "Dev", string.Empty, "A")
        ]);

        var vm = CreateViewModel(store: store);

        vm.RemoveItem(vm.LaunchItems[0]);

        Assert.Equal(1, store.SaveCallCount);
        Assert.Empty(store.LastSavedEntries);
    }

    [Fact]
    public void UpdateItemCategory_UpdatesAndPersists()
    {
        var store = new FakeStore(
        [
            new LauncherStore.LauncherEntry(@"C:\\Tools\\A.exe", "Dev", string.Empty, "A")
        ]);

        var vm = CreateViewModel(store: store);

        vm.UpdateItemCategory(vm.LaunchItems[0], "Ops");

        Assert.Equal("Ops", vm.LaunchItems[0].Category);
        Assert.Equal(1, store.SaveCallCount);
        Assert.Equal("Ops", store.LastSavedEntries[0].Category);
    }

    [Fact]
    public void UpdateItemArguments_UpdatesAndPersists()
    {
        var store = new FakeStore(
        [
            new LauncherStore.LauncherEntry(@"C:\\Tools\\A.exe", "Dev", "-a", "A")
        ]);

        var vm = CreateViewModel(store: store);

        vm.UpdateItemArguments(vm.LaunchItems[0], "-b");

        Assert.Equal("-b", vm.LaunchItems[0].Arguments);
        Assert.Equal(1, store.SaveCallCount);
        Assert.Equal("-b", store.LastSavedEntries[0].Arguments);
    }

    [Fact]
    public void UpdateItemArguments_DoesNotRebuildCategoryCollections()
    {
        var store = new FakeStore(
        [
            new LauncherStore.LauncherEntry(@"C:\\Tools\\A.exe", "Dev", "-a", "A"),
            new LauncherStore.LauncherEntry(@"C:\\Tools\\B.exe", "Ops", "-b", "B")
        ]);

        var vm = CreateViewModel(store: store);
        var categoryChanges = 0;
        var filterCategoryChanges = 0;
        vm.CategoryNames.CollectionChanged += (_, _) => categoryChanges++;
        vm.FilterCategoryNames.CollectionChanged += (_, _) => filterCategoryChanges++;

        vm.UpdateItemArguments(vm.LaunchItems[0], "--new");

        Assert.Equal(0, categoryChanges);
        Assert.Equal(0, filterCategoryChanges);
        Assert.Equal(1, store.SaveCallCount);
    }

    [Fact]
    public void UpdateItemDisplayName_DoesNotRebuildCategoryCollections()
    {
        var store = new FakeStore(
        [
            new LauncherStore.LauncherEntry(@"C:\\Tools\\A.exe", "Dev", "-a", "A"),
            new LauncherStore.LauncherEntry(@"C:\\Tools\\B.exe", "Ops", "-b", "B")
        ]);

        var vm = CreateViewModel(store: store);
        var categoryChanges = 0;
        var filterCategoryChanges = 0;
        vm.CategoryNames.CollectionChanged += (_, _) => categoryChanges++;
        vm.FilterCategoryNames.CollectionChanged += (_, _) => filterCategoryChanges++;

        vm.UpdateItemDisplayName(vm.LaunchItems[0], "A-Renamed");

        Assert.Equal(0, categoryChanges);
        Assert.Equal(0, filterCategoryChanges);
        Assert.Equal(1, store.SaveCallCount);
    }

    [Fact]
    public void UpdateItemCategory_RebuildsCategoryCollections()
    {
        var store = new FakeStore(
        [
            new LauncherStore.LauncherEntry(@"C:\\Tools\\A.exe", "Dev", "-a", "A"),
            new LauncherStore.LauncherEntry(@"C:\\Tools\\B.exe", "Ops", "-b", "B")
        ]);

        var vm = CreateViewModel(store: store);
        var categoryChanges = 0;
        var filterCategoryChanges = 0;
        vm.CategoryNames.CollectionChanged += (_, _) => categoryChanges++;
        vm.FilterCategoryNames.CollectionChanged += (_, _) => filterCategoryChanges++;

        vm.UpdateItemCategory(vm.LaunchItems[0], "QA");

        Assert.True(categoryChanges > 0);
        Assert.True(filterCategoryChanges > 0);
        Assert.Contains("QA", vm.CategoryNames);
        Assert.DoesNotContain("Dev", vm.CategoryNames);
        Assert.Equal(1, store.SaveCallCount);
    }

    [Fact]
    public void QuickAddNameOrPath_UpdatesSuggestions()
    {
        var vm = CreateViewModel();

        vm.QuickAddNameOrPath = "note";

        Assert.Equal(new[] { "note-s1", "note-s2" }, vm.QuickAddSuggestions);
    }

    [Fact]
    public void SelectedCategory_FiltersLaunchItems()
    {
        var store = new FakeStore(
        [
            new LauncherStore.LauncherEntry(@"C:\\Tools\\A.exe", "Dev", string.Empty, "A"),
            new LauncherStore.LauncherEntry(@"C:\\Tools\\B.exe", "Ops", string.Empty, "B")
        ]);

        var vm = CreateViewModel(store: store);
        vm.SelectedCategory = "Dev";

        var filtered = vm.FilteredLaunchItems.Cast<LaunchItemViewModel>().ToList();
        Assert.Single(filtered);
        Assert.Equal("A", filtered[0].DisplayName);
    }

    [Fact]
    public void RemoveItem_RemovesSelectedCategory_ResetsToAll()
    {
        var store = new FakeStore(
        [
            new LauncherStore.LauncherEntry(@"C:\\Tools\\A.exe", "Dev", string.Empty, "A"),
            new LauncherStore.LauncherEntry(@"C:\\Tools\\B.exe", "Ops", string.Empty, "B")
        ]);

        var vm = CreateViewModel(store: store);
        vm.SelectedCategory = "Ops";

        var opsItem = vm.LaunchItems.Single(item => item.Category == "Ops");
        vm.RemoveItem(opsItem);

        Assert.Equal(AppResources.AllCategories, vm.SelectedCategory);
    }

    [Fact]
    public void TryAddQuickItem_WhenCategoryFilterActive_ResetsQuickAddCategoryToSelectedCategory()
    {
        var store = new FakeStore(
        [
            new LauncherStore.LauncherEntry(@"C:\\Tools\\A.exe", "Dev", string.Empty, "A")
        ]);

        var resolver = new FakeResolver
        {
            ShouldResolve = true,
            ResolvedApp = new ResolvedApp(@"C:\\Tools\\B.exe", "B")
        };

        var vm = CreateViewModel(store, resolver);
        vm.SelectedCategory = "Dev";
        vm.QuickAddCategory = "Ops";
        vm.QuickAddNameOrPath = "b";

        var result = vm.TryAddQuickItem();

        Assert.True(result.IsSuccess);
        Assert.Equal("Dev", vm.QuickAddCategory);
        Assert.Equal(string.Empty, vm.QuickAddNameOrPath);
        Assert.Equal(string.Empty, vm.QuickAddArguments);
    }

    [Fact]
    public void ComplexFlow_AddUpdateMoveRemove_PersistsConsistentOrderAndMetadata()
    {
        var store = new FakeStore(
        [
            new LauncherStore.LauncherEntry(@"C:\\Tools\\A.exe", "Dev", "-a", "A"),
            new LauncherStore.LauncherEntry(@"C:\\Tools\\B.exe", "Ops", "-b", "B")
        ]);

        var resolver = new FakeResolver
        {
            ShouldResolve = true,
            ResolvedApp = new ResolvedApp(@"C:\\Tools\\C.exe", "C")
        };

        var vm = CreateViewModel(store, resolver);

        vm.QuickAddNameOrPath = "C";
        vm.QuickAddCategory = "QA";
        vm.QuickAddArguments = "-c";
        Assert.True(vm.TryAddQuickItem().IsSuccess);

        var added = vm.LaunchItems.Single(x => x.DisplayName == "C");
        vm.UpdateItemDisplayName(added, "C-App");
        vm.UpdateItemCategory(added, "Prod");
        vm.PreviewMoveItem(2, 0);
        vm.PersistOrderNow();
        vm.RemoveItem(vm.LaunchItems.Single(x => x.DisplayName == "B"));

        Assert.Equal("C-App", vm.LaunchItems[0].DisplayName);
        Assert.Equal("Prod", vm.LaunchItems[0].Category);
        Assert.Equal(2, vm.LaunchItems.Count);

        Assert.Equal(5, store.SaveCallCount);
        Assert.Collection(
            store.LastSavedEntries,
            e => Assert.Equal("C-App", e.DisplayName),
            e => Assert.Equal("A", e.DisplayName));
    }

    [Fact]
    public void SelectedLaunchItem_WhenRemoved_ReassignsToFirstFilteredItem()
    {
        var store = new FakeStore(
        [
            new LauncherStore.LauncherEntry(@"C:\\Tools\\A.exe", "Dev", string.Empty, "A"),
            new LauncherStore.LauncherEntry(@"C:\\Tools\\B.exe", "Dev", string.Empty, "B"),
            new LauncherStore.LauncherEntry(@"C:\\Tools\\C.exe", "Ops", string.Empty, "C")
        ]);

        var vm = CreateViewModel(store: store);
        vm.SelectedCategory = "Dev";
        vm.SelectedLaunchItem = vm.LaunchItems.Single(x => x.DisplayName == "B");

        vm.RemoveItem(vm.SelectedLaunchItem!);

        Assert.NotNull(vm.SelectedLaunchItem);
        Assert.Equal("A", vm.SelectedLaunchItem!.DisplayName);
        Assert.Equal("Dev", vm.SelectedLaunchItem.Category);
    }

    [Fact]
    public void QuickAddNameOrPath_SameValue_DoesNotRefreshSuggestionsAgain()
    {
        var resolver = new FakeResolver();
        var vm = CreateViewModel(resolver: resolver);

        vm.QuickAddNameOrPath = "note";
        var firstCallCount = resolver.SuggestionsCallCount;

        vm.QuickAddNameOrPath = "note";

        Assert.Equal(firstCallCount, resolver.SuggestionsCallCount);
    }

    [Fact]
    public void LongFlow_AddFailureRecoveryThenMutationAndReorder_PreservesConsistency()
    {
        var store = new FakeStore(
        [
            new LauncherStore.LauncherEntry(@"C:\\Tools\\A.exe", "Dev", "-a", "A"),
            new LauncherStore.LauncherEntry(@"C:\\Tools\\B.exe", "Ops", "-b", "B"),
            new LauncherStore.LauncherEntry(@"C:\\Tools\\C.exe", "QA", "-c", "C")
        ]);

        var resolver = new FakeResolver();
        var vm = CreateViewModel(store, resolver);

        // Phase 1: unresolved add fails and must not persist.
        resolver.ShouldResolve = false;
        vm.QuickAddNameOrPath = "missing-app";
        var failResult = vm.TryAddQuickItem();
        Assert.False(failResult.IsSuccess);
        Assert.Equal(0, store.SaveCallCount);
        Assert.Equal(3, vm.LaunchItems.Count);

        // Phase 2: resolve succeeds and adds a new item.
        resolver.ShouldResolve = true;
        resolver.ResolvedApp = new ResolvedApp(@"C:\\Tools\\D.exe", "D");
        vm.QuickAddNameOrPath = "d";
        vm.QuickAddCategory = "Sandbox";
        vm.QuickAddArguments = "--initial";
        var addResult = vm.TryAddQuickItem();
        Assert.True(addResult.IsSuccess);
        Assert.Equal(1, store.SaveCallCount);
        Assert.Equal(4, vm.LaunchItems.Count);

        // Phase 3: duplicate add fails and does not persist.
        resolver.ResolvedApp = new ResolvedApp(@"C:\\Tools\\D.exe", "D");
        vm.QuickAddNameOrPath = "duplicate-d";
        var duplicateResult = vm.TryAddQuickItem();
        Assert.False(duplicateResult.IsSuccess);
        Assert.Equal(1, store.SaveCallCount);
        Assert.Equal(4, vm.LaunchItems.Count);

        // Phase 4: mutate display/category/arguments for the newly added item.
        var added = vm.LaunchItems.Single(x => x.FullPath.EndsWith("D.exe", StringComparison.OrdinalIgnoreCase));
        vm.UpdateItemDisplayName(added, "D-App");
        vm.UpdateItemCategory(added, "Dev");
        vm.UpdateItemArguments(added, "--updated");

        Assert.Equal("D-App", added.DisplayName);
        Assert.Equal("Dev", added.Category);
        Assert.Equal("--updated", added.Arguments);
        Assert.Equal(4, store.SaveCallCount);

        // Phase 5: preview reorder, filter, remove, and explicitly persist final order.
        vm.PreviewMoveItem(3, 1);
        Assert.Equal(4, store.SaveCallCount); // preview must not persist

        vm.SelectedCategory = "Dev";
        var devFiltered = vm.FilteredLaunchItems.Cast<LaunchItemViewModel>().Select(x => x.DisplayName).ToList();
        Assert.Equal(new[] { "A", "D-App" }, devFiltered);

        var itemB = vm.LaunchItems.Single(x => x.DisplayName == "B");
        vm.RemoveItem(itemB);
        Assert.Equal(5, store.SaveCallCount);

        vm.PersistOrderNow();
        Assert.Equal(6, store.SaveCallCount);

        Assert.Equal(3, store.LastSavedEntries.Count);
        Assert.Collection(
            store.LastSavedEntries,
            e => Assert.Equal("A", e.DisplayName),
            e => Assert.Equal("D-App", e.DisplayName),
            e => Assert.Equal("C", e.DisplayName));
    }

    [Fact]
    public void LongFlow_CategoryTransitionsAndSelectionRecovery_RemainStable()
    {
        var store = new FakeStore(
        [
            new LauncherStore.LauncherEntry(@"C:\\Tools\\Alpha.exe", "Dev", string.Empty, "Alpha"),
            new LauncherStore.LauncherEntry(@"C:\\Tools\\Beta.exe", "Dev", string.Empty, "Beta"),
            new LauncherStore.LauncherEntry(@"C:\\Tools\\Gamma.exe", "Ops", string.Empty, "Gamma"),
            new LauncherStore.LauncherEntry(@"C:\\Tools\\Delta.exe", "Ops", string.Empty, "Delta")
        ]);

        var resolver = new FakeResolver
        {
            SuggestionsOverride = ["alpha", "beta", "gamma", "delta"]
        };

        var vm = CreateViewModel(store, resolver);

        // Phase 1: select Dev category and a concrete selected item.
        vm.SelectedCategory = "Dev";
        vm.SelectedLaunchItem = vm.LaunchItems.Single(x => x.DisplayName == "Beta");
        Assert.Equal("Beta", vm.SelectedLaunchItem.DisplayName);

        // Phase 2: remove currently selected item and ensure selection is reassigned.
        vm.RemoveItem(vm.SelectedLaunchItem);
        Assert.NotNull(vm.SelectedLaunchItem);
        Assert.Equal("Alpha", vm.SelectedLaunchItem!.DisplayName);
        Assert.Equal(1, store.SaveCallCount);

        // Phase 3: move remaining Dev item to Ops and verify category list/filter transitions.
        var alpha = vm.LaunchItems.Single(x => x.DisplayName == "Alpha");
        vm.UpdateItemCategory(alpha, "Ops");
        Assert.Equal(2, store.SaveCallCount);

        // Dev category should disappear, selected category should reset to all.
        Assert.Equal(AppResources.AllCategories, vm.SelectedCategory);
        Assert.DoesNotContain("Dev", vm.CategoryNames);
        Assert.Contains("Ops", vm.CategoryNames);

        // Phase 4: ensure suggestions refresh and same-value assignment does not over-refresh.
        vm.QuickAddNameOrPath = "tool";
        var afterFirst = resolver.SuggestionsCallCount;
        vm.QuickAddNameOrPath = "tool";
        Assert.Equal(afterFirst, resolver.SuggestionsCallCount);

        // Phase 5: set Ops filter and remove all Ops entries, expecting empty filtered view.
        vm.SelectedCategory = "Ops";
        foreach (var item in vm.LaunchItems.Where(x => x.Category == "Ops").ToList())
        {
            vm.RemoveItem(item);
        }

        Assert.True(vm.FilteredLaunchItems.IsEmpty);
        Assert.Equal(Visibility.Visible, vm.EmptyMessageVisibility);
        Assert.Equal(Visibility.Collapsed, vm.SelectedLaunchItemVisibility);
    }

    [Fact]
    public void ApplySettings_WhenCultureChangesFromJapaneseToEnglish_KeepsAllFilterAndItemsVisible()
    {
        var previousUi = CultureInfo.CurrentUICulture;
        var previousCulture = CultureInfo.CurrentCulture;

        try
        {
            var ja = new CultureInfo("ja");
            CultureInfo.CurrentUICulture = ja;
            CultureInfo.CurrentCulture = ja;

            var store = new FakeStore(
            [
                new LauncherStore.LauncherEntry(@"C:\\Tools\\A.exe", "Dev", string.Empty, "A")
            ]);

            var vm = CreateViewModel(store: store);

            var en = new CultureInfo("en");
            CultureInfo.CurrentUICulture = en;
            CultureInfo.CurrentCulture = en;

            vm.ApplySettings(new AppSettings { Language = LanguageOption.English });

            Assert.Equal(AppResources.AllCategories, vm.SelectedCategory);
            Assert.False(vm.FilteredLaunchItems.IsEmpty);
            Assert.Equal(Visibility.Collapsed, vm.EmptyMessageVisibility);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previousUi;
            CultureInfo.CurrentCulture = previousCulture;
        }
    }

    [Fact]
    public void ApplySettings_WhenCultureChangesFromJapaneseToEnglish_NormalizesDefaultCategoryLabels()
    {
        var previousUi = CultureInfo.CurrentUICulture;
        var previousCulture = CultureInfo.CurrentCulture;

        try
        {
            var ja = new CultureInfo("ja");
            CultureInfo.CurrentUICulture = ja;
            CultureInfo.CurrentCulture = ja;

            var store = new FakeStore(
            [
                new LauncherStore.LauncherEntry(@"C:\\Tools\\A.exe", AppResources.DefaultCategory, string.Empty, "A")
            ]);

            var vm = CreateViewModel(store: store);
            vm.QuickAddCategory = AppResources.DefaultCategory;

            var en = new CultureInfo("en");
            CultureInfo.CurrentUICulture = en;
            CultureInfo.CurrentCulture = en;

            vm.ApplySettings(new AppSettings { Language = LanguageOption.English });

            Assert.Equal(AppResources.DefaultCategory, vm.LaunchItems[0].Category);
            Assert.Equal(AppResources.DefaultCategory, vm.QuickAddCategory);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previousUi;
            CultureInfo.CurrentCulture = previousCulture;
        }
    }

    [Fact]
    public void TryAddQuickItem_EmptyInput_SetsInformationMessage()
    {
        var vm = CreateViewModel();
        vm.QuickAddNameOrPath = "   ";

        vm.TryAddQuickItem();

        Assert.NotEmpty(vm.QuickAddFeedback.Message);
        Assert.Equal(QuickAddMessageSeverity.Information, vm.QuickAddFeedback.Severity);
        Assert.Equal(Visibility.Visible, vm.QuickAddFeedback.MessageVisibility);
    }

    [Fact]
    public void TryAddQuickItem_UnresolvedInput_SetsWarningMessage()
    {
        var vm = CreateViewModel(resolver: new FakeResolver());
        vm.QuickAddNameOrPath = "unknown-app";

        vm.TryAddQuickItem();

        Assert.NotEmpty(vm.QuickAddFeedback.Message);
        Assert.Equal(QuickAddMessageSeverity.Warning, vm.QuickAddFeedback.Severity);
        Assert.Equal(Visibility.Visible, vm.QuickAddFeedback.MessageVisibility);
    }

    [Fact]
    public void TryAddQuickItem_Success_ClearsMessage()
    {
        var resolver = new FakeResolver
        {
            ShouldResolve = true,
            ResolvedApp = new ResolvedApp(@"C:\\Tools\\NewApp.exe", "NewApp")
        };
        var vm = CreateViewModel(resolver: resolver);
        vm.QuickAddNameOrPath = "newapp";
        // Produce a prior failure message via an empty input, then succeed.
        var vmPrime = CreateViewModel(resolver: resolver);
        vmPrime.QuickAddNameOrPath = string.Empty;
        vmPrime.TryAddQuickItem(); // sets QuickAddFeedback.Message
        Assert.NotEmpty(vmPrime.QuickAddFeedback.Message);
        vmPrime.QuickAddNameOrPath = "newapp";

        vmPrime.TryAddQuickItem();

        Assert.Empty(vmPrime.QuickAddFeedback.Message);
        Assert.Equal(Visibility.Collapsed, vmPrime.QuickAddFeedback.MessageVisibility);
    }

    [Fact]
    public void InsertItem_InsertsAtSpecifiedIndex_AndPersistsImmediately()
    {
        var store = new FakeStore(
        [
            new LauncherStore.LauncherEntry(@"C:\\Tools\\A.exe", "Dev", string.Empty, "A"),
            new LauncherStore.LauncherEntry(@"C:\\Tools\\C.exe", "Dev", string.Empty, "C")
        ]);

        var vm = CreateViewModel(store: store);
        var itemB = new LaunchItemViewModel(@"C:\\Tools\\B.exe", "B", "Dev", string.Empty);

        vm.InsertItem(itemB, 1);

        Assert.Equal(1, store.SaveCallCount);
        Assert.Equal(["A", "B", "C"], vm.LaunchItems.Select(x => x.DisplayName));
        Assert.Equal(["A", "B", "C"], store.LastSavedEntries.Select(x => x.DisplayName));
    }

    [Fact]
    public void InsertItem_OutOfRangeIndex_IsClamped()
    {
        var store = new FakeStore(
        [
            new LauncherStore.LauncherEntry(@"C:\\Tools\\A.exe", "Dev", string.Empty, "A")
        ]);

        var vm = CreateViewModel(store: store);
        var itemHead = new LaunchItemViewModel(@"C:\\Tools\\Head.exe", "Head", "Dev", string.Empty);
        var itemTail = new LaunchItemViewModel(@"C:\\Tools\\Tail.exe", "Tail", "Dev", string.Empty);

        vm.InsertItem(itemHead, -100);
        vm.InsertItem(itemTail, 999);

        Assert.Equal(2, store.SaveCallCount);
        Assert.Equal(["Head", "A", "Tail"], vm.LaunchItems.Select(x => x.DisplayName));
    }

    [Fact]
    public void CategorySortMode_AsAdded_PreservesFirstAppearanceOrder()
    {
        var store = new FakeStore(
        [
            new LauncherStore.LauncherEntry(@"C:\\Tools\\A.exe", "Ops", string.Empty, "A"),
            new LauncherStore.LauncherEntry(@"C:\\Tools\\B.exe", "Dev", string.Empty, "B"),
            new LauncherStore.LauncherEntry(@"C:\\Tools\\C.exe", "Neko", string.Empty, "C")
        ]);

        var vm = CreateViewModel(store: store, settings: new AppSettings { CategorySortMode = CategorySortMode.AsAdded });

        Assert.Equal(["Ops", "Dev", "Neko"], vm.CategoryNames);
    }

    [Fact]
    public void CategorySortMode_AsAdded_DefaultCategoryPinnedLast()
    {
        var defaultCategory = LauncherStore.LauncherEntry.DefaultCategory;
        var store = new FakeStore(
        [
            new LauncherStore.LauncherEntry(@"C:\\Tools\\A.exe", defaultCategory, string.Empty, "A"),
            new LauncherStore.LauncherEntry(@"C:\\Tools\\B.exe", "Dev", string.Empty, "B"),
            new LauncherStore.LauncherEntry(@"C:\\Tools\\C.exe", "Ops", string.Empty, "C")
        ]);

        var vm = CreateViewModel(store: store, settings: new AppSettings { CategorySortMode = CategorySortMode.AsAdded });

        Assert.Equal(defaultCategory, vm.CategoryNames.Last());
    }

    [Fact]
    public void CategorySortMode_Alphabetical_DefaultCategoryPinnedLast()
    {
        var defaultCategory = LauncherStore.LauncherEntry.DefaultCategory;
        var store = new FakeStore(
        [
            new LauncherStore.LauncherEntry(@"C:\\Tools\\A.exe", defaultCategory, string.Empty, "A"),
            new LauncherStore.LauncherEntry(@"C:\\Tools\\B.exe", "Dev", string.Empty, "B"),
            new LauncherStore.LauncherEntry(@"C:\\Tools\\C.exe", "Ops", string.Empty, "C")
        ]);

        var vm = CreateViewModel(store: store, settings: new AppSettings { CategorySortMode = CategorySortMode.Alphabetical });

        Assert.Equal(defaultCategory, vm.CategoryNames.Last());
    }

    [Fact]
    public void AppListSortMode_Name_SortsFilteredViewByDisplayName()
    {
        var store = new FakeStore(
        [
            new LauncherStore.LauncherEntry(@"C:\\Tools\\B.exe", "Ops", string.Empty, "Zeta"),
            new LauncherStore.LauncherEntry(@"C:\\Tools\\A.exe", "Dev", string.Empty, "Alpha"),
            new LauncherStore.LauncherEntry(@"C:\\Tools\\C.exe", "Dev", string.Empty, "Kappa")
        ]);

        var vm = CreateViewModel(store: store, settings: new AppSettings { AppListSortMode = AppListSortMode.Name });

        Assert.Equal(["Alpha", "Kappa", "Zeta"], vm.FilteredLaunchItems.Cast<LaunchItemViewModel>().Select(x => x.DisplayName));
    }

    [Fact]
    public void ApplySettings_CategorySortModeSwitch_RebuildsCategoryOrder()
    {
        var store = new FakeStore(
        [
            new LauncherStore.LauncherEntry(@"C:\\Tools\\A.exe", "Ops", string.Empty, "A"),
            new LauncherStore.LauncherEntry(@"C:\\Tools\\B.exe", "Dev", string.Empty, "B")
        ]);

        var vm = CreateViewModel(store: store, settings: new AppSettings { CategorySortMode = CategorySortMode.AsAdded });
        Assert.Equal(["Ops", "Dev"], vm.CategoryNames);

        vm.ApplySettings(new AppSettings { CategorySortMode = CategorySortMode.Alphabetical });

        Assert.Equal(["Dev", "Ops"], vm.CategoryNames);
    }

    [Fact]
    public void ApplySettings_HttpIconPolicyChange_RefreshesOnlyHttpItems()
    {
        var store = new FakeStore(
        [
            new LauncherStore.LauncherEntry("https://example.com", "Web", string.Empty, "Example"),
            new LauncherStore.LauncherEntry(@"C:\\Tools\\A.exe", "Dev", string.Empty, "A")
        ]);
        var iconProvider = new TrackingIconProvider();
        var vm = CreateViewModel(store: store, iconProvider: iconProvider);

        iconProvider.Reset();
        vm.ApplySettings(new AppSettings { FetchHttpIcons = false });

        Assert.Equal(1, iconProvider.ApplySettingsCallCount);
        Assert.Equal(1, iconProvider.GetInitialIconCalls.Count(static path => path.StartsWith("https://", StringComparison.OrdinalIgnoreCase)));
        Assert.DoesNotContain(iconProvider.GetInitialIconCalls, static path => path.StartsWith(@"C:\", StringComparison.OrdinalIgnoreCase));
    }

    private static MainWindowViewModel CreateViewModel(FakeStore? store = null, FakeResolver? resolver = null, AppSettings? settings = null, ILaunchItemIconProvider? iconProvider = null)
    {
        return new MainWindowViewModel(resolver ?? new FakeResolver(), store ?? new FakeStore([]), settings ?? new AppSettings(), iconProvider);
    }

    private sealed class TrackingIconProvider : ILaunchItemIconProvider
    {
        internal List<string> GetInitialIconCalls { get; } = [];
        internal int ApplySettingsCallCount { get; private set; }

        public void ApplySettings(AppSettings settings)
        {
            ApplySettingsCallCount++;
        }

        public ImageSource? GetInitialIcon(string fullPath)
        {
            GetInitialIconCalls.Add(fullPath);
            return null;
        }

        public ValueTask<ImageSource?> GetDeferredIconAsync(string fullPath)
        {
            return ValueTask.FromResult<ImageSource?>(null);
        }

        internal void Reset()
        {
            GetInitialIconCalls.Clear();
            ApplySettingsCallCount = 0;
        }
    }
}

