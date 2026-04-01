using System.Globalization;
using Xunit;

namespace applanch.Tests.Helpers;

public class ResourcesSourceGenerationTests
{
    [Fact]
    public void Subtitle_InvariantCulture_ReturnsEnglishText()
    {
        var originalCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

            Assert.Equal(
                "Right-click to register items, then launch with one click. Drag & drop to reorder.",
                AppResources.Subtitle);
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }

    [Fact]
    public void Subtitle_JapaneseCulture_ReturnsJapaneseText()
    {
        var originalCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("ja-JP");

            Assert.Equal(
                "右クリックで登録した項目をワンクリックで起動。並び替えはドラッグ＆ドロップ。",
                AppResources.Subtitle);
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }
}
