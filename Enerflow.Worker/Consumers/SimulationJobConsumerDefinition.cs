using MassTransit;

namespace Enerflow.Worker.Consumers;

/// <summary>
/// Definition to enforce concurrency limits for the SimulationJobConsumer.
/// This ensures only one simulation runs at a time per worker instance, 
/// preventing race conditions in the non-thread-safe DWSIM automation engine.
/// </summary>
public class SimulationJobConsumerDefinition : ConsumerDefinition<SimulationJobConsumer>
{
    public SimulationJobConsumerDefinition()
    {
        // Limit the concurrent message processing to 1
        ConcurrentMessageLimit = 1;
    }

    protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator, IConsumerConfigurator<SimulationJobConsumer> consumerConfigurator, IRegistrationContext context)
    {
        // Add any specific endpoint configuration here if needed
        endpointConfigurator.UseMessageRetry(r => r.Interval(3, 1000));

        // This implicitly sets the prefetch count to maintain the concurrent message limit
        // ensuring we don't over-fetch 
    }
}
