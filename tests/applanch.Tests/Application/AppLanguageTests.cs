using System.Globalization;
using System.Reflection;
using System.Runtime.ExceptionServices;
using applanch.Infrastructure.Storage;
using Xunit;

namespace applanch.Tests.Application;

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
    public void ShouldReloadMainWindow_WhenLanguageChanged_ReturnsTrue()
    {
        var current = new AppSettings { Language = LanguageOption.English };
        var next = current with { Language = LanguageOption.Japanese };

        var result = App.ShouldReloadMainWindow(current, next);

        Assert.True(result);
    }

    [Fact]
    public void ShouldReloadMainWindow_WhenLanguageUnchanged_ReturnsFalse()
    {
        var current = new AppSettings { Language = LanguageOption.Japanese };
        var next = current with { ConfirmBeforeDelete = !current.ConfirmBeforeDelete };

        var result = App.ShouldReloadMainWindow(current, next);

        Assert.False(result);
    }

    [Fact]
    public void ApplyLanguage_English_UpdatesEmptyMessageResource_OnStaThread()
    {
        RunInSta(() =>
        {
            var method = GetApplyLanguageMethod();
            var previousUi = CultureInfo.CurrentUICulture;
            var previousCulture = CultureInfo.CurrentCulture;

            try
            {
                method.Invoke(null, [LanguageOption.English]);

                Assert.Equal("No items registered yet. Add from Explorer's right-click menu.", applanch.Properties.Resources.EmptyMessage);
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
        RunInSta(() =>
        {
            var method = GetApplyLanguageMethod();
            var previousUi = CultureInfo.CurrentUICulture;
            var previousCulture = CultureInfo.CurrentCulture;

            try
            {
                method.Invoke(null, [LanguageOption.Japanese]);

                Assert.Equal("登録項目がまだありません。エクスプローラーの右クリックから追加してください。", applanch.Properties.Resources.EmptyMessage);
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

    private static void RunInSta(Action action)
    {
        Exception? captured = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                captured = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (captured is not null)
        {
            ExceptionDispatchInfo.Capture(captured).Throw();
        }
    }

}
