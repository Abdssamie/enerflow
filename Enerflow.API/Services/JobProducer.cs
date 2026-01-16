using Enerflow.Domain.DTOs;
using Enerflow.Domain.Interfaces;
using MassTransit;

namespace Enerflow.API.Services;

/// <summary>
/// Implementation of job producer that publishes simulation jobs to MassTransit message queue.
/// </summary>
public class JobProducer : IJobProducer
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<JobProducer> _logger;

    public JobProducer(IPublishEndpoint publishEndpoint, ILogger<JobProducer> logger)
    {
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task PublishJobAsync(SimulationJob job, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Publishing simulation job {JobId} for simulation {SimulationId}",
            job.JobId, job.SimulationId);

        await _publishEndpoint.Publish(job, cancellationToken);

        _logger.LogInformation(
            "Successfully published simulation job {JobId}",
            job.JobId);
    }
}
