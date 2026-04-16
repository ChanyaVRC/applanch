namespace applanch.Serialization;

public sealed class JsonPathResolutionException : InvalidOperationException
{
    public JsonPathResolutionException(string message)
        : base(message)
    {
    }

    public JsonPathResolutionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
