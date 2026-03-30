namespace applanch.ResourceGenerator;

internal sealed class ResxFile
{
    public ResxFile(string path, string? content)
    {
        Path = path;
        Content = content;
    }

    public string Path { get; }

    public string? Content { get; }
}
