using DWSIM.Automation;

namespace Enerflow.API.Services;

public interface IDWSIMService
{
    Automation2 GetAutomationManager();
    string GetDWSIMVersion();
}

public class DWSIMService : IDWSIMService
{
    private readonly Automation2 _automationManager;

    public DWSIMService()
    {
        // Initializing the DWSIM Automation manager
        _automationManager = new Automation2();
    }

    public Automation2 GetAutomationManager()
    {
        return _automationManager;
    }

    public string GetDWSIMVersion()
    {
        return "9.0.5";
    }
}
