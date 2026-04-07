namespace applanch.Infrastructure.Utilities;

internal sealed class JsonPathResolutionException : InvalidOperationException
{
    internal JsonPathResolutionException(string message)
        : base(message)
    {
    }

    internal JsonPathResolutionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
