using applanch.Settings;

namespace applanch.Events;

public readonly record struct AppRefreshPayload(AppSettings PreviousSettings, AppSettings CurrentSettings);
