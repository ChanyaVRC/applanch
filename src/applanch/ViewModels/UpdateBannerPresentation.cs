using System.Windows;

namespace applanch.ViewModels;

internal readonly record struct UpdateBannerPresentation(
    Visibility BannerVisibility,
    Visibility HeaderButtonVisibility,
    Visibility ActionButtonVisibility);
