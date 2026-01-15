using DWSIM.Automation;
using DWSIM.GlobalSettings;

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
        
        // CRITICAL: Enable AutomationMode to prevent DWSIM from trying to access UI components (Headless mode)
        DWSIM.GlobalSettings.Settings.AutomationMode = true;
    }

    public Automation2 GetAutomationManager()
    {
        return _automationManager;
    }

    public string GetDWSIMVersion()
    {
        return Settings.CurrentVersion;
    }
}
