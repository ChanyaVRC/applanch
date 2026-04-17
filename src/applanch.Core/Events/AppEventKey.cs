namespace applanch.Events;

public sealed class AppEventKey
{
    internal AppEventKey(int id, string? name = null)
    {
        Id = id;
        Name = name;
    }

    internal int Id { get; }

    internal string? Name { get; }
}

public sealed class AppEventKey<TPayload>
{
    internal AppEventKey(int id, string? name = null)
    {
        Id = id;
        Name = name;
    }

    internal int Id { get; }

    internal string? Name { get; }

    public string PayloadName { get; } = typeof(TPayload).Name;
}
