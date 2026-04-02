using Xunit;
using applanch.Infrastructure.Storage;
using applanch.Infrastructure.Integration;
using applanch.Infrastructure.Utilities;
using applanch.ViewModels;
using System.Windows.Media;
using System.Windows.Threading;

namespace applanch.Tests.ViewModels;

public class LaunchItemViewModelTests
{
    [Fact]
    public void Constructor_UsesPathFileName_WhenDisplayNameIsBlank()
    {
        var vm = new LaunchItemViewModel(new applanch.Infrastructure.Utilities.LaunchPath(fullPath: @"C:\\Tools\\MyApp.exe"),
            category: "Dev",
            arguments: "--help",
            displayName: "   ");

        Assert.Equal("MyApp", vm.DisplayName);
    }

    [Fact]
    public void Constructor_NormalizesCategoryAndArguments()
    {
        var vm = new LaunchItemViewModel(new applanch.Infrastructure.Utilities.LaunchPath(fullPath: @"C:\\Tools\\MyApp.exe"),
            category: "  Utilities  ",
            arguments: "  -v  ",
            displayName: "  Custom Name  ");

        Assert.Equal("Utilities", vm.Category);
        Assert.Equal("-v", vm.Arguments);
        Assert.Equal("Custom Name", vm.DisplayName);
    }

    [Fact]
    public void Category_SetWhitespace_FallsBackToDefaultCategory()
    {
        var vm = new LaunchItemViewModel(new applanch.Infrastructure.Utilities.LaunchPath(fullPath: @"C:\\Tools\\MyApp.exe"),
            category: "Dev",
            arguments: string.Empty,
            displayName: "App");

        vm.Category = "   ";

        Assert.Equal(LauncherStore.LauncherEntry.DefaultCategory, vm.Category);
    }

    [Fact]
    public void Arguments_SetWhitespace_BecomesEmptyString()
    {
        var vm = new LaunchItemViewModel(new applanch.Infrastructure.Utilities.LaunchPath(fullPath: @"C:\\Tools\\MyApp.exe"),
            category: "Dev",
            arguments: "abc",
            displayName: "App");

        vm.Arguments = "   ";

        Assert.Equal(string.Empty, vm.Arguments);
    }

    [Fact]
    public void PropertyChanged_RaisesOnlyOnEffectiveValueChange()
    {
        var vm = new LaunchItemViewModel(new applanch.Infrastructure.Utilities.LaunchPath(fullPath: @"C:\\Tools\\MyApp.exe"),
            category: "Dev",
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

        vm.Category = "  Dev  ";
        vm.Category = "Ops";
        vm.Arguments = " abc ";
        vm.Arguments = "--run";

        Assert.Equal(new[] { nameof(LaunchItemViewModel.Category), nameof(LaunchItemViewModel.Arguments) }, changed);
    }

    [Fact]
    public void DisplayName_SetWhitespace_FallsBackToFileName_AndRaisesChanged()
    {
        var vm = new LaunchItemViewModel(new applanch.Infrastructure.Utilities.LaunchPath(fullPath: @"C:\\Tools\\Tool.exe"),
            category: "Dev",
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
        var vm = new LaunchItemViewModel(new applanch.Infrastructure.Utilities.LaunchPath(fullPath: @"C:\\Tools\\Tool.exe"),
            category: "Dev",
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
        var vm = new LaunchItemViewModel(new applanch.Infrastructure.Utilities.LaunchPath(fullPath: @"C:\\Tools\\Tool.exe"),
            category: "Dev",
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
        var vm = new LaunchItemViewModel(new applanch.Infrastructure.Utilities.LaunchPath(fullPath: missingPath),
            category: "Dev",
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
            var vm = new LaunchItemViewModel(new applanch.Infrastructure.Utilities.LaunchPath(fullPath: existingPath),
                category: "Dev",
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
    public void Constructor_UrlItem_UpdatesIconSourceWhenDeferredIconArrives()
    {
        RunInSta(() =>
        {
            var initialIcon = CreateDrawingImage();
            var deferredIcon = CreateDrawingImage();
            var provider = new DeferredIconProvider(initialIcon);
            var vm = new LaunchItemViewModel(new applanch.Infrastructure.Utilities.LaunchPath(fullPath: "https://example.com"),
                category: "Web",
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
            WaitUntil(() => ReferenceEquals(vm.IconSource, deferredIcon));

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

    private static void RunInSta(Action action)
    {
        Exception? captured = null;
        var completed = new ManualResetEventSlim(false);
        var thread = new Thread(() =>
        {
            try
            {
                action();
                DrainDispatcher();
            }
            catch (Exception ex)
            {
                captured = ex;
            }
            finally
            {
                completed.Set();
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        completed.Wait();

        if (captured is not null)
        {
            throw new Xunit.Sdk.XunitException($"STA test failed: {captured}");
        }
    }

    private static void WaitUntil(Func<bool> condition)
    {
        var timeoutAt = DateTime.UtcNow.AddSeconds(2);
        while (!condition() && DateTime.UtcNow < timeoutAt)
        {
            DrainDispatcher();
            Thread.Sleep(10);
        }

        Assert.True(condition());
    }

    private static void DrainDispatcher()
    {
        var frame = new DispatcherFrame();
        Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => frame.Continue = false));
        Dispatcher.PushFrame(frame);
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


