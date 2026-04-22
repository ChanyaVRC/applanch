using applanch.Theming;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Media;
using MediaColor = System.Windows.Media.Color;
using WpfKeyEventArgs = System.Windows.Input.KeyEventArgs;
using WpfMouseEventArgs = System.Windows.Input.MouseEventArgs;
using WpfPoint = System.Windows.Point;

namespace applanch.ThemeCreator;

public partial class ColorPickerWindow : Window
{
    private const double SvWidth = 258.0;
    private const double SvHeight = 218.0;
    private const double HueWidth = 258.0;

    private double _hue;
    private double _sat;
    private double _val;

    private bool _updatingUi;
    private bool _isDraggingSv;
    private bool _isDraggingHue;

    public string SelectedHex { get; private set; }

    public ColorPickerWindow(string initialHex)
    {
        InitializeComponent();

        SelectedHex = initialHex;
        _hue = 0;
        _sat = 0;
        _val = 0.5;

        if (ThemeColor.TryParse(initialHex, out var color))
        {
            var brush = new SolidColorBrush(color.ToMediaColor());
            RgbToHsv(color.R, color.G, color.B, out _hue, out _sat, out _val);
            BeforePreview.Background = brush;
        }
        else
        {
            BeforePreview.Background = new SolidColorBrush(MediaColor.FromRgb(128, 128, 128));
        }

        Loaded += (_, _) => UpdateAllUi();
    }

    private void UpdateAllUi()
    {
        if (_updatingUi)
        {
            return;
        }

        _updatingUi = true;
        try
        {
            var color = HsvToColor(_hue, _sat, _val);
            SelectedHex = $"#{color.R:X2}{color.G:X2}{color.B:X2}";

            AfterPreview.Background = new SolidColorBrush(color);
            HueBackground.Fill = new SolidColorBrush(HsvToColor(_hue, 1.0, 1.0));

            Canvas.SetLeft(SvCursor, _sat * SvWidth - 7);
            Canvas.SetTop(SvCursor, (1.0 - _val) * SvHeight - 7);
            Canvas.SetLeft(HueCursor, _hue / 360.0 * HueWidth - 2);

            HexTextBox.Text = SelectedHex;
            RedSlider.Value = color.R;
            GreenSlider.Value = color.G;
            BlueSlider.Value = color.B;
            RedTextBox.Text = color.R.ToString(CultureInfo.InvariantCulture);
            GreenTextBox.Text = color.G.ToString(CultureInfo.InvariantCulture);
            BlueTextBox.Text = color.B.ToString(CultureInfo.InvariantCulture);
        }
        finally
        {
            _updatingUi = false;
        }
    }

    private void UpdateAllUiExceptHex()
    {
        var color = HsvToColor(_hue, _sat, _val);
        SelectedHex = $"#{color.R:X2}{color.G:X2}{color.B:X2}";

        AfterPreview.Background = new SolidColorBrush(color);
        HueBackground.Fill = new SolidColorBrush(HsvToColor(_hue, 1.0, 1.0));

        Canvas.SetLeft(SvCursor, _sat * SvWidth - 7);
        Canvas.SetTop(SvCursor, (1.0 - _val) * SvHeight - 7);
        Canvas.SetLeft(HueCursor, _hue / 360.0 * HueWidth - 2);

        RedSlider.Value = color.R;
        GreenSlider.Value = color.G;
        BlueSlider.Value = color.B;
        RedTextBox.Text = color.R.ToString(CultureInfo.InvariantCulture);
        GreenTextBox.Text = color.G.ToString(CultureInfo.InvariantCulture);
        BlueTextBox.Text = color.B.ToString(CultureInfo.InvariantCulture);
    }

    // ── SV picker ────────────────────────────────────────────────────

    private void SvCanvas_MouseDown(object sender, MouseButtonEventArgs e)
    {
        _isDraggingSv = true;
        SvCanvas.CaptureMouse();
        SetSvFromMouse(e.GetPosition(SvCanvas));
        e.Handled = true;
    }

    private void SvCanvas_MouseMove(object sender, WpfMouseEventArgs e)
    {
        if (_isDraggingSv)
        {
            SetSvFromMouse(e.GetPosition(SvCanvas));
        }
    }

    private void SvCanvas_MouseUp(object sender, MouseButtonEventArgs e)
    {
        _isDraggingSv = false;
        SvCanvas.ReleaseMouseCapture();
    }

    private void SetSvFromMouse(WpfPoint p)
    {
        _sat = Math.Clamp(p.X / SvWidth, 0.0, 1.0);
        _val = Math.Clamp(1.0 - p.Y / SvHeight, 0.0, 1.0);
        UpdateAllUi();
    }

    // ── Hue bar ───────────────────────────────────────────────────────

    private void HueCanvas_MouseDown(object sender, MouseButtonEventArgs e)
    {
        _isDraggingHue = true;
        HueCanvas.CaptureMouse();
        SetHueFromMouse(e.GetPosition(HueCanvas));
        e.Handled = true;
    }

    private void HueCanvas_MouseMove(object sender, WpfMouseEventArgs e)
    {
        if (_isDraggingHue)
        {
            SetHueFromMouse(e.GetPosition(HueCanvas));
        }
    }

    private void HueCanvas_MouseUp(object sender, MouseButtonEventArgs e)
    {
        _isDraggingHue = false;
        HueCanvas.ReleaseMouseCapture();
    }

    private void SetHueFromMouse(WpfPoint p)
    {
        _hue = Math.Clamp(p.X / HueWidth, 0.0, 1.0) * 360.0;
        UpdateAllUi();
    }

    // ── Hex TextBox ───────────────────────────────────────────────────

    private void HexTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_updatingUi)
        {
            return;
        }

        var text = HexTextBox.Text.Trim();
        if (!text.StartsWith('#'))
        {
            text = "#" + text;
        }

        if (ThemeColor.TryParse(text, out var color))
        {
            RgbToHsv(color.R, color.G, color.B, out _hue, out _sat, out _val);
            _updatingUi = true;
            try
            {
                UpdateAllUiExceptHex();
            }
            finally
            {
                _updatingUi = false;
            }
        }
    }

    // ── RGB sliders ───────────────────────────────────────────────────

    private void RgbSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_updatingUi)
        {
            return;
        }

        RgbToHsv((byte)RedSlider.Value, (byte)GreenSlider.Value, (byte)BlueSlider.Value,
            out _hue, out _sat, out _val);
        UpdateAllUi();
    }

    // ── RGB TextBoxes ─────────────────────────────────────────────────

    private void RgbTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_updatingUi)
        {
            return;
        }

        if (!byte.TryParse(RedTextBox.Text, out var r))
        {
            return;
        }

        if (!byte.TryParse(GreenTextBox.Text, out var g))
        {
            return;
        }

        if (!byte.TryParse(BlueTextBox.Text, out var b))
        {
            return;
        }

        RgbToHsv(r, g, b, out _hue, out _sat, out _val);
        UpdateAllUi();
    }

    // ── OK / Cancel ───────────────────────────────────────────────────

    private void Ok_Click(object sender, RoutedEventArgs e) => DialogResult = true;

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private void Window_KeyDown(object sender, WpfKeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            DialogResult = false;
            e.Handled = true;
        }
        else if (e.Key == Key.Enter
            && !HexTextBox.IsFocused
            && !RedTextBox.IsFocused
            && !GreenTextBox.IsFocused
            && !BlueTextBox.IsFocused)
        {
            DialogResult = true;
            e.Handled = true;
        }
    }

    // ── Color math ────────────────────────────────────────────────────

    private static MediaColor HsvToColor(double h, double s, double v)
    {
        if (s == 0)
        {
            var gray = (byte)(v * 255);
            return MediaColor.FromRgb(gray, gray, gray);
        }

        double hh = (h % 360.0) / 60.0;
        int i = (int)hh;
        double f = hh - i;
        double p = v * (1 - s);
        double q = v * (1 - s * f);
        double t = v * (1 - s * (1 - f));

        var (r, g, b) = i switch
        {
            0 => (v, t, p),
            1 => (q, v, p),
            2 => (p, v, t),
            3 => (p, q, v),
            4 => (t, p, v),
            _ => (v, p, q),
        };

        return MediaColor.FromRgb((byte)(r * 255), (byte)(g * 255), (byte)(b * 255));
    }

    private static void RgbToHsv(byte r, byte g, byte b, out double h, out double s, out double v)
    {
        double rf = r / 255.0;
        double gf = g / 255.0;
        double bf = b / 255.0;

        double max = Math.Max(rf, Math.Max(gf, bf));
        double min = Math.Min(rf, Math.Min(gf, bf));
        double delta = max - min;

        v = max;
        s = max == 0 ? 0 : delta / max;
        h = 0;

        if (delta != 0)
        {
            if (max == rf)
            {
                h = 60.0 * (((gf - bf) / delta) % 6);
            }
            else if (max == gf)
            {
                h = 60.0 * ((bf - rf) / delta + 2);
            }
            else
            {
                h = 60.0 * ((rf - gf) / delta + 4);
            }
        }

        if (h < 0)
        {
            h += 360;
        }
    }
}
