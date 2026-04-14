namespace applanch.Infrastructure.Launch.AppIdResolvers;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
internal sealed class AppIdSourcePrefixAttribute(string prefix) : Attribute
{
    internal string Prefix { get; } = prefix;
}
