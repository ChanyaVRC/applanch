using System.Globalization;
using System.Reflection;
using applanch.Infrastructure.Storage;
using applanch.Tests.TestSupport;
using Xunit;

namespace applanch.Tests.Application;

[Collection("WpfTests")]
public class AppLanguageTests
{
    [Fact]
    public void ApplyLanguage_English_SetsCurrentUiCultureToEnglish()
    {
        var method = GetApplyLanguageMethod();
        var previousUi = CultureInfo.CurrentUICulture;
        var previousCulture = CultureInfo.CurrentCulture;

        try
        {
            method.Invoke(null, [LanguageOption.English]);

            Assert.Equal("en", CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);
            Assert.Equal("en", CultureInfo.CurrentCulture.TwoLetterISOLanguageName);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previousUi;
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.DefaultThreadCurrentUICulture = previousUi;
            CultureInfo.DefaultThreadCurrentCulture = previousCulture;
        }
    }

    [Fact]
    public void ApplyLanguage_Japanese_SetsCurrentUiCultureToJapanese()
    {
        var method = GetApplyLanguageMethod();
        var previousUi = CultureInfo.CurrentUICulture;
        var previousCulture = CultureInfo.CurrentCulture;

        try
        {
            method.Invoke(null, [LanguageOption.Japanese]);

            Assert.Equal("ja", CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);
            Assert.Equal("ja", CultureInfo.CurrentCulture.TwoLetterISOLanguageName);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previousUi;
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.DefaultThreadCurrentUICulture = previousUi;
            CultureInfo.DefaultThreadCurrentCulture = previousCulture;
        }
    }

    private static MethodInfo GetApplyLanguageMethod()
    {
        var method = typeof(App).GetMethod("ApplyLanguage", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);
        return method!;
    }

    [Fact]
    public void ApplyLanguage_English_UpdatesEmptyMessageResource_OnStaThread()
    {
        WpfTestHost.RunInSta(() =>
        {
            var method = GetApplyLanguageMethod();
            var previousUi = CultureInfo.CurrentUICulture;
            var previousCulture = CultureInfo.CurrentCulture;

            try
            {
                method.Invoke(null, [LanguageOption.English]);

                Assert.Equal("No items registered yet. Add from Explorer's right-click menu.", AppResources.EmptyMessage);
            }
            finally
            {
                CultureInfo.CurrentUICulture = previousUi;
                CultureInfo.CurrentCulture = previousCulture;
                CultureInfo.DefaultThreadCurrentUICulture = previousUi;
                CultureInfo.DefaultThreadCurrentCulture = previousCulture;
            }
        });
    }

    [Fact]
    public void ApplyLanguage_Japanese_UpdatesEmptyMessageResource_OnStaThread()
    {
        WpfTestHost.RunInSta(() =>
        {
            var method = GetApplyLanguageMethod();
            var previousUi = CultureInfo.CurrentUICulture;
            var previousCulture = CultureInfo.CurrentCulture;

            try
            {
                method.Invoke(null, [LanguageOption.Japanese]);

                Assert.Equal("登録項目がまだありません。エクスプローラーの右クリックから追加してください。", AppResources.EmptyMessage);
            }
            finally
            {
                CultureInfo.CurrentUICulture = previousUi;
                CultureInfo.CurrentCulture = previousCulture;
                CultureInfo.DefaultThreadCurrentUICulture = previousUi;
                CultureInfo.DefaultThreadCurrentCulture = previousCulture;
            }
        });
    }

}
