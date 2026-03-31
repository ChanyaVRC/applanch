namespace applanch.Infrastructure.Launch;

internal sealed class LaunchFallbackConfiguration
{
    public List<LaunchFallbackRuleConfiguration> Rules { get; init; } = [];
}
