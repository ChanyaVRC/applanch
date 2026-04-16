using System.Collections;
using System.Windows;
using System.Windows.Data;
using applanch.Controls;
using applanch.Infrastructure.Storage;
using applanch.Settings;
using applanch.Tests.TestSupport;
using applanch.Tests.ViewModels.TestDoubles;
using applanch.ViewModels;
using Xunit;

namespace applanch.Tests.UI;

[Collection("WpfTests")]
public sealed class MainWindowQuickAddCategoryUiTests
{
    [Fact]
    public void QuickAddCategorySuggestions_UpdateDisplayedLabels_AfterLanguageChange()
    {
        WpfTestHost.RunInSta(() =>
        {
            WpfTestHost.EnsureAppResources();

            using var japaneseCulture = new CultureScope("ja");
            var japaneseDefaultLabel = AppResources.DefaultCategory;

            var viewModel = CreateViewModel(new AppSettings { Language = LanguageOption.Japanese });
            viewModel.QuickAddCategory = Category.Default;

            var control = new SuggestionInputControl
            {
                DataContext = viewModel,
            };
            control.SetBinding(
                SuggestionInputControl.SuggestionsProperty,
                new Binding(nameof(MainWindowViewModel.CategoryNames))
                {
                    Converter = new CategoryDisplayConverter(),
                });
            control.SetBinding(
                SuggestionInputControl.TextProperty,
                new Binding(nameof(MainWindowViewModel.QuickAddCategory))
                {
                    Converter = new CategoryDisplayConverter(),
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                });

            var hostWindow = new Window
            {
                Content = control,
                Width = 300,
                Height = 120,
            };

            WpfTestHost.ShowOffscreen(hostWindow);

            try
            {
                WpfTestHost.DoEvents();

                var initialSuggestions = Assert.IsAssignableFrom<IEnumerable>(control.Suggestions).Cast<string>().ToArray();
                Assert.Contains(japaneseDefaultLabel, initialSuggestions);
                Assert.Equal(japaneseDefaultLabel, control.Text);

                using var englishCulture = new CultureScope("en");
                var englishDefaultLabel = AppResources.DefaultCategory;

                viewModel.ApplySettings(new AppSettings { Language = LanguageOption.English });
                WpfTestHost.DoEvents();

                var updatedSuggestions = Assert.IsAssignableFrom<IEnumerable>(control.Suggestions).Cast<string>().ToArray();
                Assert.Contains(englishDefaultLabel, updatedSuggestions);
                Assert.DoesNotContain(japaneseDefaultLabel, updatedSuggestions);
                Assert.Equal(englishDefaultLabel, control.Text);
            }
            finally
            {
                hostWindow.Close();
                WpfTestHost.DoEvents();
            }
        });
    }

    private static MainWindowViewModel CreateViewModel(AppSettings settings)
    {
        var entries = new[]
        {
            new LauncherEntry(new LaunchPath(@"C:\Tools\App.exe"), Category.FromInput("Utilities"), string.Empty, "App")
        };
        return new MainWindowViewModel(
            new FakeResolver(),
            new FakeStore(entries),
            settings);
    }
}
