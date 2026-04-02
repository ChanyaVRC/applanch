using Xunit;
using applanch.Infrastructure.Storage;
using applanch.ViewModels;

namespace applanch.Tests.ViewModels;

public class LaunchItemViewModelTests
{
    [Fact]
    public void Constructor_UsesPathFileName_WhenDisplayNameIsBlank()
    {
        var vm = new LaunchItemViewModel(
            fullPath: @"C:\\Tools\\MyApp.exe",
            category: "Dev",
            arguments: "--help",
            displayName: "   ");

        Assert.Equal("MyApp", vm.DisplayName);
    }

    [Fact]
    public void Constructor_NormalizesCategoryAndArguments()
    {
        var vm = new LaunchItemViewModel(
            fullPath: @"C:\\Tools\\MyApp.exe",
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
        var vm = new LaunchItemViewModel(
            fullPath: @"C:\\Tools\\MyApp.exe",
            category: "Dev",
            arguments: string.Empty,
            displayName: "App");

        vm.Category = "   ";

        Assert.Equal(LauncherStore.LauncherEntry.DefaultCategory, vm.Category);
    }

    [Fact]
    public void Arguments_SetWhitespace_BecomesEmptyString()
    {
        var vm = new LaunchItemViewModel(
            fullPath: @"C:\\Tools\\MyApp.exe",
            category: "Dev",
            arguments: "abc",
            displayName: "App");

        vm.Arguments = "   ";

        Assert.Equal(string.Empty, vm.Arguments);
    }

    [Fact]
    public void PropertyChanged_RaisesOnlyOnEffectiveValueChange()
    {
        var vm = new LaunchItemViewModel(
            fullPath: @"C:\\Tools\\MyApp.exe",
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
        var vm = new LaunchItemViewModel(
            fullPath: @"C:\\Tools\\Tool.exe",
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
        var vm = new LaunchItemViewModel(
            fullPath: @"C:\\Tools\\Tool.exe",
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
        var vm = new LaunchItemViewModel(
            fullPath: @"C:\\Tools\\Tool.exe",
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
        var vm = new LaunchItemViewModel(
            fullPath: missingPath,
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
            var vm = new LaunchItemViewModel(
                fullPath: existingPath,
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
}


