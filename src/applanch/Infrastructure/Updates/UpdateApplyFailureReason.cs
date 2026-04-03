namespace applanch.Infrastructure.Updates;

internal enum UpdateApplyFailureReason
{
    None = 0,
    Unknown = 1,
    Network = 2,
    Io = 3,
    Permission = 4,
    InvalidPackage = 5,
}
