using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows;
using applanch.Events;
using applanch.Infrastructure.Storage;
using applanch.Infrastructure.Theming;
using applanch.Tests.TestSupport;
using applanch.ViewModels;
using Xunit;

namespace applanch.Tests.UI;

public sealed class SettingsThemeSelectionUiTests
{
    [Fact]
    public void ThemeComboBox_KeepsSelection_AfterLanguageChange()
    {
        WpfTestHost.RunInSta(() =>
        {
            var appEvent = new AppEvent();
            var culturePhase = "before";
            appEvent.Register(AppEvents.Commit, _ => culturePhase = "after");

            IReadOnlyList<ThemeOption> ThemeOptionsProvider()
            {
                return culturePhase == "before"
                    ?
                    [
                        new ThemeOption(ThemePaletteConfigurationLoader.SystemThemeId, "System", IsSystemOption: true),
                        new ThemeOption(ThemePaletteConfigurationLoader.LightThemeId, "Light")
                    ]
                    :
                    [
                        new ThemeOption(ThemePaletteConfigurationLoader.SystemThemeId, "システム", IsSystemOption: true),
                        new ThemeOption(ThemePaletteConfigurationLoader.LightThemeId, "ライト")
                    ];
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
            comboBox.ItemTemplate = (DataTemplate)System.Windows.Markup.XamlReader.Parse(
                "<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><TextBlock Text='{Binding DisplayName}'/></DataTemplate>");
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
}
