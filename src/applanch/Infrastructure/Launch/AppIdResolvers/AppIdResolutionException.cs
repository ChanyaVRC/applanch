namespace applanch.Infrastructure.Launch.AppIdResolvers;

internal sealed class AppIdResolutionException : Exception
{
    internal AppIdResolutionException(string message)
        : base(message)
    {
    }

    internal AppIdResolutionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}