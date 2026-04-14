namespace applanch.Infrastructure.Launch.AppIdResolvers;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
internal sealed class AppIdSourceAttribute(string source) : Attribute
{
    internal string Source { get; } = source;
}
