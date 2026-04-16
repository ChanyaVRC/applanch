using applanch.Infrastructure.Storage;

namespace applanch.Events;

public readonly record struct AppRefreshPayload(AppSettings PreviousSettings, AppSettings CurrentSettings);
