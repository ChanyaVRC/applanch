using System.Windows;
using System.Windows.Media;

namespace applanch.Infrastructure.Theming;

internal static class WindowIconThemeHelper
{
    internal static readonly Color LightPaletteIconColor = (Color)ColorConverter.ConvertFromString("#0F172A")!;
    internal static readonly Color DarkPaletteIconColor = (Color)ColorConverter.ConvertFromString("#FFFFF0")!;

    private static readonly Geometry BrandGeometry = Geometry.Parse(
        "M248,8 L208.6,93.4 L128.9,173.1 L123.2,248 L107.9,194.2 L75.2,180.8 L61.8,148.1 L8,132.8 L82.9,127.1 L162.6,47.4 Z");

    private static readonly DrawingImage LightPaletteIcon = CreateIconSource(LightPaletteIconColor);
    private static readonly DrawingImage DarkPaletteIcon = CreateIconSource(DarkPaletteIconColor);

    internal static void Apply(Window window, ResourceDictionary resources)
    {
        window.Icon = ResolveIconVariant(resources);
    }

    internal static Color ResolveIconColor(ResourceDictionary resources)
    {
        if (resources["Brush.TextPrimary"] is SolidColorBrush textBrush && IsDarkColor(textBrush.Color))
        {
            return LightPaletteIconColor;
        }

        return DarkPaletteIconColor;
    }

    private static DrawingImage ResolveIconVariant(ResourceDictionary resources)
    {
        return ResolveIconColor(resources) == LightPaletteIconColor
            ? LightPaletteIcon
            : DarkPaletteIcon;
    }

    private static bool IsDarkColor(Color color)
    {
        var relativeLuminance = ((0.2126 * color.R) + (0.7152 * color.G) + (0.0722 * color.B)) / 255d;
        return relativeLuminance < 0.5d;
    }

    private static DrawingImage CreateIconSource(Color fillColor)
    {
        var brush = new SolidColorBrush(fillColor);
        brush.Freeze();

        var geometry = BrandGeometry.Clone();
        geometry.Transform = new ScaleTransform(1d / 8d, 1d / 8d);
        geometry.Freeze();

        var drawing = new GeometryDrawing(brush, null, geometry);
        drawing.Freeze();

        var image = new DrawingImage(drawing);
        image.Freeze();

        return image;
    }
}
