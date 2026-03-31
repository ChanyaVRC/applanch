using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using applanch.Infrastructure.Integration;
using applanch.Infrastructure.Storage;

namespace applanch;

public sealed class LaunchItemViewModel : INotifyPropertyChanged
{
    private string _displayName;
    private string _category;
    private string _arguments;
    private bool _isRenaming;
    private string _editingName = string.Empty;

    public LaunchItemViewModel(string fullPath, string category, string arguments, string displayName)
    {
        FullPath = fullPath;
        _displayName = LaunchItemNormalization.NormalizeDisplayName(displayName, fullPath);
        IconSource = GetIcon(fullPath);
        _category = LaunchItemNormalization.NormalizeCategory(category);
        _arguments = LaunchItemNormalization.NormalizeArguments(arguments);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string DisplayName
    {
        get => _displayName;
        set
        {
            var normalized = LaunchItemNormalization.NormalizeDisplayName(value, FullPath);
            if (string.Equals(_displayName, normalized, StringComparison.Ordinal))
            {
                return;
            }

            _displayName = normalized;
            OnPropertyChanged(nameof(DisplayName));
        }
    }

    public bool IsRenaming
    {
        get => _isRenaming;
        set
        {
            if (_isRenaming == value)
            {
                return;
            }

            _isRenaming = value;
            OnPropertyChanged(nameof(IsRenaming));
        }
    }

    public string EditingName
    {
        get => _editingName;
        set
        {
            if (string.Equals(_editingName, value, StringComparison.Ordinal))
            {
                return;
            }

            _editingName = value;
            OnPropertyChanged(nameof(EditingName));
        }
    }

    public string FullPath { get; }
    public BitmapSource? IconSource { get; }
    public bool IsPathMissing => !Path.Exists(FullPath);

    public string Arguments
    {
        get => _arguments;
        set
        {
            var normalized = LaunchItemNormalization.NormalizeArguments(value);
            if (string.Equals(_arguments, normalized, StringComparison.Ordinal))
            {
                return;
            }

            _arguments = normalized;
            OnPropertyChanged(nameof(Arguments));
        }
    }

    public string Category
    {
        get => _category;
        set
        {
            var normalized = LaunchItemNormalization.NormalizeCategory(value);
            if (string.Equals(_category, normalized, StringComparison.Ordinal))
            {
                return;
            }

            _category = normalized;
            OnPropertyChanged(nameof(Category));
        }
    }

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private static BitmapSource? GetIcon(string fullPath)
    {
        var shfi = new NativeMethods.SHFILEINFO();
        var result = NativeMethods.SHGetFileInfo(fullPath, 0, ref shfi,
            (uint)Marshal.SizeOf<NativeMethods.SHFILEINFO>(),
            NativeMethods.SHGFI_ICON | NativeMethods.SHGFI_LARGEICON);

        if (result == IntPtr.Zero || shfi.hIcon == IntPtr.Zero)
            return null;

        try
        {
            return Imaging.CreateBitmapSourceFromHIcon(
                shfi.hIcon,
                Int32Rect.Empty,
                BitmapSizeOptions.FromWidthAndHeight(32, 32));
        }
        finally
        {
            NativeMethods.DestroyIcon(shfi.hIcon);
        }
    }
}
