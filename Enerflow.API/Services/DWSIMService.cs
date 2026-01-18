using DWSIM.Automation;
using DWSIM.GlobalSettings;

namespace Enerflow.API.Services;

public class DWSIMService : IDWSIMService, IDisposable
{
    private readonly Automation2 _automationManager;
    private readonly object _lock = new();
    private bool _disposed;

    public DWSIMService()
    {
        // CRITICAL: Enable AutomationMode to prevent DWSIM from trying to access UI components (Headless mode)
        Settings.AutomationMode = true;

        // Initializing the DWSIM Automation manager
        _automationManager = new Automation2();
    }

    public void Execute(Action<Automation2> action)
    {
        lock (_lock)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(DWSIMService));
            action(_automationManager);
        }
    }

    public T Execute<T>(Func<Automation2, T> action)
    {
        lock (_lock)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(DWSIMService));
            return action(_automationManager);
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        lock (_lock)
        {
            if (_disposed) return;

            if (disposing)
            {
                // Automation2 exposes ReleaseResources() to free internal resources.
                _automationManager.ReleaseResources();
            }

            _disposed = true;
        }
    }
}
