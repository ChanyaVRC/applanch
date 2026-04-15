using System.Windows;
using applanch.Events;

namespace applanch;

public sealed partial class MainWindow
{
    private void CategorySidebarPinToggleButton_Click(object sender, RoutedEventArgs e)
    {
        UpdateCategorySidebarPinned(CategorySidebar.IsPinned);
    }

    private void ApplyCategorySidebarPinnedSetting(bool isPinned, bool animate)
    {
        CategorySidebar.SetPinned(isPinned, animate);
    }

    private void UpdateCategorySidebarPinned(bool isPinned)
    {
        CategorySidebar.SetPinned(isPinned, animate: true);

        if (_settings.CategorySidebarPinned == isPinned)
        {
            return;
        }

        var updatedSettings = SetCategorySidebarPinned(_settings, isPinned);

        if (_appEvent is not null)
        {
            _appEvent.Invoke(AppEvents.Commit, updatedSettings);
            return;
        }

        updatedSettings.Save();
        ApplySettingsFromAppRefresh(updatedSettings);
    }
}
