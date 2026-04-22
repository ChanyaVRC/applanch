using System.Windows;
using applanch.Tests.ThemeCreator.TestSupport;
using applanch.ThemeCreator;
using Xunit;

namespace applanch.Tests.ThemeCreator;

[Collection("WpfTests")]
public sealed class ColorPickerWindowTests
{
    [Fact]
    public void Constructor_AndShowOffscreen_DoNotThrow()
    {
        WpfTestHost.RunInSta(() =>
        {
            WpfTestHost.EnsureApplication();

            var window = new ColorPickerWindow("#336699");
            WpfTestHost.ShowOffscreen(window);

            Assert.Equal("#336699", window.SelectedHex, ignoreCase: true);

            window.Close();
        });
    }

    [Fact]
    public void FocusingHexTextBox_DoesNotShiftControlsBelow()
    {
        WpfTestHost.RunInSta(() =>
        {
            WpfTestHost.EnsureApplication();

            var window = new ColorPickerWindow("#336699");
            WpfTestHost.ShowOffscreen(window);

            var initialHexHeight = window.HexTextBox.ActualHeight;
            var initialRedSliderTop = window.RedSlider.TranslatePoint(new Point(0, 0), window).Y;

            window.HexTextBox.Focus();
            WpfTestHost.DoEvents();

            Assert.True(window.HexTextBox.IsKeyboardFocusWithin);
            Assert.Equal(initialHexHeight, window.HexTextBox.ActualHeight, 3);
            Assert.Equal(initialRedSliderTop, window.RedSlider.TranslatePoint(new Point(0, 0), window).Y, 3);

            window.Close();
        });
    }
}
