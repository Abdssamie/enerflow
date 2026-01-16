using Enerflow.Domain.DTOs;

namespace Enerflow.Domain.Interfaces;

/// <summary>
/// Service for publishing simulation jobs to the message queue.
/// </summary>
public interface IJobProducer
{
    /// <summary>
    /// Publishes a simulation job to the message queue for processing by workers.
    /// </summary>
    /// <param name="job">The simulation job to publish</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task PublishJobAsync(SimulationJob job, CancellationToken cancellationToken = default);
}
