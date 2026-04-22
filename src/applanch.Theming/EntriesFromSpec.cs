using System.Text.Json.Serialization;

namespace applanch.Theming;

[JsonConverter(typeof(EntriesFromSpecJsonConverter))]
public abstract record EntriesFromSpec;