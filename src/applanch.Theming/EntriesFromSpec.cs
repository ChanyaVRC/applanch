using System.Text.Json.Serialization;

namespace applanch.Theming;

[JsonConverter(typeof(EntriesFromSpecJsonConverter))]
internal abstract record EntriesFromSpec;