using System.Text.Json.Serialization;

namespace applanch.Infrastructure.Theming;

[JsonConverter(typeof(EntriesFromSpecJsonConverter))]
internal abstract record EntriesFromSpec;
