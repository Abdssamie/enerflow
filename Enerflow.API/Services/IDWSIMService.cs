using DWSIM.Automation;

namespace Enerflow.API.Services;

public interface IDWSIMService
{
    /// <summary>
    /// Executes an action using the shared Automation2 instance in a thread-safe manner.
    /// </summary>
    void Execute(Action<Automation2> action);

    /// <summary>
    /// Executes a function using the shared Automation2 instance in a thread-safe manner and returns a result.
    /// </summary>
    T Execute<T>(Func<Automation2, T> action);
}
