using System.Reflection;

namespace applanch.Infrastructure.Launch.AppIdResolvers;

/// <summary>
/// Factory for creating IAppIdResolver instances based on source configuration.
/// </summary>
internal static class AppIdResolverFactory
{
    private static readonly Dictionary<string, Func<IAppIdResolver>> ExactResolverFactories;
    private static readonly Dictionary<string, Func<string, IAppIdResolver>> PrefixResolverFactories;

    static AppIdResolverFactory()
    {
        ExactResolverFactories = [];
        PrefixResolverFactories = [];

        foreach (var type in typeof(AppIdResolverFactory).Assembly.GetTypes())
        {
            if (!typeof(IAppIdResolver).IsAssignableFrom(type) || type.IsAbstract || type.IsInterface)
            {
                continue;
            }

            foreach (var attribute in type.GetCustomAttributesData())
            {
                TryRegisterExactResolver(type, attribute);
                TryRegisterPrefixResolver(type, attribute);
            }
        }
    }

    private static void TryRegisterExactResolver(Type type, CustomAttributeData attribute)
    {
        if (attribute.AttributeType != typeof(AppIdSourceAttribute)
            || attribute.ConstructorArguments.Count != 1
            || attribute.ConstructorArguments[0].Value is not string source)
        {
            return;
        }

        var constructor = type.GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null, [], modifiers: null);
        if (constructor is not null)
            ExactResolverFactories[source] = () => (IAppIdResolver)constructor.Invoke(null);
    }

    private static void TryRegisterPrefixResolver(Type type, CustomAttributeData attribute)
    {
        if (attribute.AttributeType != typeof(AppIdSourcePrefixAttribute)
            || attribute.ConstructorArguments.Count != 1
            || attribute.ConstructorArguments[0].Value is not string prefix)
        {
            return;
        }

        var constructor = type.GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null, [typeof(string)], modifiers: null);
        if (constructor is not null)
            PrefixResolverFactories[prefix.ToLowerInvariant()] = s => (IAppIdResolver)constructor.Invoke([s]);
    }

    /// <summary>
    /// Creates a resolver for the given source.
    /// Supported formats:
    /// - "static:VALUE" - Static app ID value
    /// - "steam-manifest" - Resolve from Steam manifest files
    /// - "registry:HIVE:KeyPath:ValueName" - Resolve from Windows Registry
    /// </summary>
    internal static IAppIdResolver? CreateResolver(string? source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return null;
        }

        var trimmedSource = source.Trim();

        if (ExactResolverFactories.TryGetValue(trimmedSource, out var exactFactory))
        {
            return exactFactory();
        }

        var separatorIndex = trimmedSource.IndexOf(':');
        if (separatorIndex <= 0)
        {
            return null;
        }

        var prefix = trimmedSource[..separatorIndex];
        if (PrefixResolverFactories.TryGetValue(prefix, out var prefixFactory))
        {
            var normalizedSource = trimmedSource[(separatorIndex + 1)..];
            return prefixFactory(normalizedSource);
        }

        return null;
    }

}
