using DWSIM.Automation;
using DWSIM.GlobalSettings;

namespace Enerflow.API.Services;

public interface IDWSIMService
{
    Automation2 GetAutomationManager();
    string GetDWSIMVersion();
}

// Add thread-safety synchronization and proper resource cleanup for singleton
// Automation2 instance.
// 
// Since DWSIMService is registered as a singleton, its shared Automation2 instance
// is accessed concurrently by FlowsheetService and HealthController. Address these issues:
// 
// TODO: Thread-safety: Automation2 is not thread-safe and not designed for concurrent
// multi-threaded use. Wrap calls to GetAutomationManager() with lock synchronization or serialize access to prevent race conditions.
// 
// TODO: Resource cleanup: Automation2 does not implement IDisposable, but it exposes
// a ReleaseResources() method that must be called to free internal resources.
// Consider implementing IDisposable on DWSIMService and calling
// _automationManager.ReleaseResources() in the Dispose() method, then ensure
// the singleton is properly disposed when the application shuts down.

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
        return Settings.CurrentVersion;
    }
}