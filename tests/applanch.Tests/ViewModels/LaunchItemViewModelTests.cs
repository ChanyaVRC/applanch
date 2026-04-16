using Xunit;
using applanch.Infrastructure.Storage;
using applanch.Infrastructure.Integration;
using applanch.Tests.TestSupport;
using applanch.ViewModels;
using System.Windows.Media;

namespace applanch.Tests.ViewModels;

[Collection("WpfTests")]
public class LaunchItemViewModelTests
{
    [Fact]
    public void Constructor_UsesPathFileName_WhenDisplayNameIsBlank()
    {
        var vm = new LaunchItemViewModel(new applanch.Core.Infrastructure.Utilities.LaunchPath(fullPath: @"C:\Tools\MyApp.exe"),
            category: Category.FromInput("Dev"),
            arguments: "--help",
            displayName: "   ");

        Assert.Equal("MyApp", vm.DisplayName);
    }

    [Fact]
    public void Constructor_NormalizesCategoryAndArguments()
    {
        var vm = new LaunchItemViewModel(new applanch.Core.Infrastructure.Utilities.LaunchPath(fullPath: @"C:\Tools\MyApp.exe"),
            category: Category.FromInput("  Utilities  "),
            arguments: "  -v  ",
            displayName: "  Custom Name  ");

        Assert.Equal("Utilities", vm.Category.Value);
        Assert.Equal("-v", vm.Arguments);
        Assert.Equal("Custom Name", vm.DisplayName);
    }

    [Fact]
    public void Category_SetWhitespace_FallsBackToDefaultCategory()
    {
        var vm = new LaunchItemViewModel(new applanch.Core.Infrastructure.Utilities.LaunchPath(fullPath: @"C:\Tools\MyApp.exe"),
            category: Category.FromInput("Dev"),
            arguments: string.Empty,
            displayName: "App");

        vm.Category = Category.FromInput("   ");

        Assert.Equal(LauncherEntry.DefaultCategory, vm.Category.Value);
    }

    [Fact]
    public void Arguments_SetWhitespace_BecomesEmptyString()
    {
        var vm = new LaunchItemViewModel(new applanch.Core.Infrastructure.Utilities.LaunchPath(fullPath: @"C:\Tools\MyApp.exe"),
            category: Category.FromInput("Dev"),
            arguments: "abc",
            displayName: "App");

        vm.Arguments = "   ";

        Assert.Equal(string.Empty, vm.Arguments);
    }

    [Fact]
    public void PropertyChanged_RaisesOnlyOnEffectiveValueChange()
    {
        var vm = new LaunchItemViewModel(new applanch.Core.Infrastructure.Utilities.LaunchPath(fullPath: @"C:\Tools\MyApp.exe"),
            category: Category.FromInput("Dev"),
            arguments: "abc",
            displayName: "App");

        var changed = new List<string>();
        vm.PropertyChanged += (_, e) =>
        {
            if (!string.IsNullOrWhiteSpace(e.PropertyName))
            {
                changed.Add(e.PropertyName!);
            }
        };

        vm.Category = Category.FromInput("  Dev  ");
        vm.Category = Category.FromInput("Ops");
        vm.Arguments = " abc ";
        vm.Arguments = "--run";

        Assert.Equal(new[] { nameof(LaunchItemViewModel.Category), nameof(LaunchItemViewModel.Arguments) }, changed);
    }

    [Fact]
    public void DisplayName_SetWhitespace_FallsBackToFileName_AndRaisesChanged()
    {
        var vm = new LaunchItemViewModel(new applanch.Core.Infrastructure.Utilities.LaunchPath(fullPath: @"C:\Tools\Tool.exe"),
            category: Category.FromInput("Dev"),
            arguments: string.Empty,
            displayName: "Original");

        var changed = new List<string>();
        vm.PropertyChanged += (_, e) =>
        {
            if (!string.IsNullOrWhiteSpace(e.PropertyName))
            {
                changed.Add(e.PropertyName!);
            }
        };

        vm.DisplayName = "   ";

        Assert.Equal("Tool", vm.DisplayName);
        Assert.Single(changed);
        Assert.Equal(nameof(LaunchItemViewModel.DisplayName), changed[0]);
    }

    [Fact]
    public void IsRenaming_RaisesOnlyOnEffectiveValueChange()
    {
        var vm = new LaunchItemViewModel(new applanch.Core.Infrastructure.Utilities.LaunchPath(fullPath: @"C:\Tools\Tool.exe"),
            category: Category.FromInput("Dev"),
            arguments: string.Empty,
            displayName: "Tool");

        var changed = new List<string>();
        vm.PropertyChanged += (_, e) =>
        {
            if (!string.IsNullOrWhiteSpace(e.PropertyName))
            {
                changed.Add(e.PropertyName!);
            }
        };

        vm.IsRenaming = true;
        vm.IsRenaming = true;
        vm.IsRenaming = false;

        Assert.Equal(new[] { nameof(LaunchItemViewModel.IsRenaming), nameof(LaunchItemViewModel.IsRenaming) }, changed);
    }

    [Fact]
    public void EditingName_RaisesOnlyOnEffectiveValueChange()
    {
        var vm = new LaunchItemViewModel(new applanch.Core.Infrastructure.Utilities.LaunchPath(fullPath: @"C:\Tools\Tool.exe"),
            category: Category.FromInput("Dev"),
            arguments: string.Empty,
            displayName: "Tool");

        var changed = new List<string>();
        vm.PropertyChanged += (_, e) =>
        {
            if (!string.IsNullOrWhiteSpace(e.PropertyName))
            {
                changed.Add(e.PropertyName!);
            }
        };

        vm.EditingName = "Tool Temp";
        vm.EditingName = "Tool Temp";
        vm.EditingName = "Tool Final";

        Assert.Equal("Tool Final", vm.EditingName);
        Assert.Equal(new[] { nameof(LaunchItemViewModel.EditingName), nameof(LaunchItemViewModel.EditingName) }, changed);
    }

    [Fact]
    public void IsPathMissing_WhenPathDoesNotExist_ReturnsTrue()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), $"applanch-missing-{Guid.NewGuid():N}.exe");
        var vm = new LaunchItemViewModel(new applanch.Core.Infrastructure.Utilities.LaunchPath(fullPath: missingPath),
            category: Category.FromInput("Dev"),
            arguments: string.Empty,
            displayName: "Missing");

        Assert.True(vm.IsPathMissing);
    }

    [Fact]
    public void IsPathMissing_WhenFileExists_ReturnsFalse()
    {
        var existingPath = Path.GetTempFileName();
        try
        {
            var vm = new LaunchItemViewModel(new applanch.Core.Infrastructure.Utilities.LaunchPath(fullPath: existingPath),
                category: Category.FromInput("Dev"),
                arguments: string.Empty,
                displayName: "Existing");

            Assert.False(vm.IsPathMissing);
        }
        finally
        {
            File.Delete(existingPath);
        }
    }

    [Fact]
    public void RefreshIcon_WhenFileIsDeleted_UpdatesIsPathMissingAndRaisesPropertyChanged()
    {
        var existingPath = Path.GetTempFileName();
        try
        {
            var vm = new LaunchItemViewModel(new applanch.Core.Infrastructure.Utilities.LaunchPath(fullPath: existingPath),
                category: Category.FromInput("Dev"),
                arguments: string.Empty,
                displayName: "Existing");

            var changed = new List<string>();
            vm.PropertyChanged += (_, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.PropertyName))
                {
                    changed.Add(e.PropertyName!);
                }
            };

            File.Delete(existingPath);
            vm.RefreshIcon();

            Assert.True(vm.IsPathMissing);
            Assert.Contains(nameof(LaunchItemViewModel.IsPathMissing), changed);
        }
        finally
        {
            if (File.Exists(existingPath))
            {
                File.Delete(existingPath);
            }
        }
    }

    [Fact]
    public void Constructor_UrlItem_UpdatesIconSourceWhenDeferredIconArrives()
    {
        WpfTestHost.RunInStaAndDrain(() =>
        {
            var initialIcon = CreateDrawingImage();
            var deferredIcon = CreateDrawingImage();
            var provider = new DeferredIconProvider(initialIcon);
            var vm = new LaunchItemViewModel(new applanch.Core.Infrastructure.Utilities.LaunchPath(fullPath: "https://example.com"),
                category: Category.FromInput("Web"),
                arguments: string.Empty,
                displayName: "Example",
                iconProvider: provider);

            var changed = new List<string>();
            vm.PropertyChanged += (_, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.PropertyName))
                {
                    changed.Add(e.PropertyName!);
                }
            };

            Assert.Same(initialIcon, vm.IconSource);

            provider.Complete(deferredIcon);
            WaitUntil(() => ReferenceEquals(vm.IconSource, deferredIcon), TimeSpan.FromSeconds(5));

            Assert.Contains(nameof(LaunchItemViewModel.IconSource), changed);
        });
    }

    private static DrawingImage CreateDrawingImage()
    {
        var drawing = new GeometryDrawing(
            Brushes.CadetBlue,
            null,
            new RectangleGeometry(new System.Windows.Rect(0, 0, 10, 10)));
        drawing.Freeze();

        var image = new DrawingImage(drawing);
        image.Freeze();
        return image;
    }

    private static void WaitUntil(Func<bool> condition, TimeSpan? timeout = null)
    {
        var effectiveTimeout = timeout ?? TimeSpan.FromSeconds(2);
        var timeoutAt = DateTime.UtcNow.Add(effectiveTimeout);
        while (!condition() && DateTime.UtcNow < timeoutAt)
        {
            WpfTestHost.DoEvents();
            Thread.Sleep(10);
        }

        Assert.True(condition());
    }

    private sealed class DeferredIconProvider(ImageSource initialIcon) : ILaunchItemIconProvider
    {
        private readonly TaskCompletionSource<ImageSource?> _deferredIcon = new();

        public void ApplySettings(AppSettings settings)
        {
        }

        public ImageSource? GetInitialIcon(LaunchPath path) => initialIcon;

        public ValueTask<ImageSource?> GetDeferredIconAsync(LaunchPath path) => new(_deferredIcon.Task);

        internal void Complete(ImageSource icon)
        {
            _deferredIcon.TrySetResult(icon);
        }
    }
}


