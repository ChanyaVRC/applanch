using applanch.Infrastructure.Storage;

namespace applanch.Events;

internal readonly record struct AppRefreshPayload(AppSettings PreviousSettings, AppSettings CurrentSettings);
