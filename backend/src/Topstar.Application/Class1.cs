namespace Topstar.Application;

public interface IPlatformInfoService
{
    PlatformInfo GetInfo();
}

public sealed record PlatformInfo(string ProjectName, string Environment, DateTime UtcNow);

public sealed class PlatformInfoService : IPlatformInfoService
{
    public PlatformInfo GetInfo()
    {
        return new PlatformInfo("Topstar", "SharedBusinessLogic", DateTime.UtcNow);
    }
}
