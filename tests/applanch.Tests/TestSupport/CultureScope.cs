using System.Globalization;

namespace applanch.Tests.TestSupport;

internal sealed class CultureScope : IDisposable
{
    private readonly CultureInfo _originalUiCulture;
    private readonly CultureInfo _originalCulture;

    public CultureScope(string cultureName)
    {
        _originalUiCulture = CultureInfo.CurrentUICulture;
        _originalCulture = CultureInfo.CurrentCulture;

        var culture = CultureInfo.GetCultureInfo(cultureName);
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
    }

    public void Dispose()
    {
        CultureInfo.CurrentUICulture = _originalUiCulture;
        CultureInfo.CurrentCulture = _originalCulture;
    }
}
