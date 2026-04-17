using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows;
using System.Globalization;
using applanch.Events;
using applanch.Settings;
using applanch.Theming;
using applanch.Tests.TestSupport;
using applanch.ViewModels;
using Xunit;

namespace applanch.Tests.UI;

[Collection("WpfTests")]
public sealed class SettingsThemeSelectionUiTests
{
    [Fact]
    public void ThemeComboBox_KeepsSelection_AfterLanguageChange()
    {
        WpfTestHost.RunInSta(() =>
        {
            var appEvent = AppEventFactory.Create();
            appEvent.Subscribe(AppEvents.Commit, payload =>
            {
                var settings = Assert.IsType<AppSettings>(payload);
                var cultureName = settings.Language == LanguageOption.Japanese ? "ja-JP" : "en-US";
                var culture = new CultureInfo(cultureName);
                CultureInfo.CurrentUICulture = culture;
                CultureInfo.CurrentCulture = culture;
            });

            using var cultureScope = new CultureScope("en-US");

            IReadOnlyDictionary<string, ThemeOption> ThemeOptionsProvider()
            {
                var options = new[]
                {
                    new ThemeOption(
                        ThemePaletteConfigurationLoader.SystemThemeId,
                        new LocalizedText(
                            "System",
                            new Dictionary<LanguageOption, string>
                            {
                                [LanguageOption.Japanese] = "システム"
                            }),
                        IsSystemOption: true),
                    new ThemeOption(
                        ThemePaletteConfigurationLoader.LightThemeId,
                        new LocalizedText(
                            "Light",
                            new Dictionary<LanguageOption, string>
                            {
                                [LanguageOption.Japanese] = "ライト"
                            }))
                };
                return options.ToDictionary(x => x.ThemeId);
            }

            var vm = new SettingsWindowViewModel(
                new AppSettings { Language = LanguageOption.English, ThemeId = ThemePaletteConfigurationLoader.SystemThemeId },
                appEvent,
                ThemeOptionsProvider);

            var comboBox = new ComboBox
            {
                SelectedValuePath = nameof(ThemeOption.ThemeId),
                IsEditable = false,
                DataContext = vm,
            };
            TextSearch.SetTextPath(comboBox, nameof(ThemeOption.DisplayName));
            comboBox.ItemTemplate = CreateDisplayNameItemTemplate();
            comboBox.SetBinding(ItemsControl.ItemsSourceProperty, new Binding(nameof(SettingsWindowViewModel.ThemeOptions)));
            comboBox.SetBinding(Selector.SelectedValueProperty, new Binding(nameof(SettingsWindowViewModel.SelectedThemeId))
            {
                Mode = BindingMode.TwoWay,
            });
            comboBox.SetBinding(ComboBox.TextProperty, new Binding(nameof(SettingsWindowViewModel.SelectedThemeDisplayName))
            {
                Mode = BindingMode.OneWay,
            });

            WpfTestHost.DoEvents();
            Assert.Equal(0, comboBox.SelectedIndex);
            Assert.False(comboBox.IsEditable);

            vm.SelectedLanguage = LanguageOption.Japanese;

            WpfTestHost.DoEvents();
            Assert.Equal(2, comboBox.Items.Count);
            Assert.Equal(ThemePaletteConfigurationLoader.SystemThemeId, comboBox.SelectedValue);
            Assert.Equal("システム", comboBox.Text);
        });
    }

    private static DataTemplate CreateDisplayNameItemTemplate()
    {
        var textBlock = new FrameworkElementFactory(typeof(TextBlock));
        textBlock.SetBinding(TextBlock.TextProperty, new Binding(nameof(ThemeOption.DisplayName)));

        return new DataTemplate
        {
            VisualTree = textBlock,
        };
    }
}
