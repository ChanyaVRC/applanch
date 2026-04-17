namespace applanch.Infrastructure.Integration;

internal interface IIconPathResolver
{
    string Resolve(string launchPath);
}
