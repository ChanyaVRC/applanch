using System.Resources;

namespace applanch.Infrastructure.Localization;

internal static class AppResourceManagerProvider
{
    public static ResourceManager Instance { get; } = new(typeof(AppResources));
}