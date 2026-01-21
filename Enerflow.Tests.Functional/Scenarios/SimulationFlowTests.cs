using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Enerflow.Domain.DTOs;
using Enerflow.Domain.Enums;
using FluentAssertions;
using Xunit;

// TODO: TROUBLESHOOTING PAUSED (2026-01-17)
// Current Status:
// 1. System.Drawing.Common crash (GDI+) appears resolved by adding v6.0.0 package and runtime config.
// 2. System.Configuration.ConfigurationManager FileNotFoundException resolved by adding package.
// 3. NEW BLOCKER: MassTransit Consumer Loop Fault - "Connection refused" to Postgres.
//    - Error: Npgsql.NpgsqlException (0x80004005): Failed to connect to [Host:Port]
//    - Context: The Worker running inside IntegrationTestWebAppFactory cannot reach the Testcontainer Postgres.
//    - Hypotheses:
//      a) Connection string not propagating correctly to Worker services.
//      b) MassTransit Transport configuration in TestFactory isn't overriding the Worker's default config effectively.
//      c) Docker networking issue (though unlikely for Host port mapping).
// Next Steps:
// 1. Verify IntegrationTestWebAppFactory.ConfigureWebHost correctly overrides "ConnectionStrings:DefaultConnection".
// 2. Debug the actual connection string being used by the SimulationJobConsumer/MassTransit at runtime.
// 3. Ensure the Worker service collection actually uses the Testcontainer connection string.

namespace Enerflow.Tests.Functional.Scenarios;

public class SimulationFlowTests : BaseIntegrationTest
{
    public SimulationFlowTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Can_Run_Simple_Mixer_Simulation()
    {
        // 1. Create Simulation
        var createRequest = new CreateSimulationRequest
        {
            Name = "Mixer E2E Test",
            ThermoPackage = "PengRobinson",
            FlashAlgorithm = "NestedLoops",
            SystemOfUnits = "SI"
        };
        var response = await HttpClient.PostAsJsonAsync("/api/v1/simulations", createRequest);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to create simulation. Status: {response.StatusCode}, Body: {errorBody}");
        }
        var simData = await response.Content.ReadFromJsonAsync<JsonElement>();
        Guid simId = simData.GetProperty("id").GetGuid();

        // 2. Add Compound
        var addCompoundResponse = await HttpClient.PostAsJsonAsync($"/api/v1/simulations/{simId}/compounds", new { name = "Water" });
        addCompoundResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // 3. Add Streams
        var inlet1Request = new AddStreamRequest
        {
            Name = "Inlet1",
            Temperature = 300, // K
            Pressure = 101325, // Pa
            MassFlow = 1.0,    // kg/s
            MolarCompositions = new Dictionary<string, double> { { "Water", 1.0 } }
        };
        var inlet1Res = await HttpClient.PostAsJsonAsync($"/api/v1/simulations/{simId}/streams", inlet1Request);
        if (!inlet1Res.IsSuccessStatusCode)
        {
            var errorBody = await inlet1Res.Content.ReadAsStringAsync();
            throw new Exception($"Failed to add stream. Status: {inlet1Res.StatusCode}, Body: {errorBody}");
        }
        var inlet1Data = await inlet1Res.Content.ReadFromJsonAsync<JsonElement>();
        Guid inlet1Id = inlet1Data.GetProperty("streamId").GetGuid();

        var inlet2Request = new AddStreamRequest
        {
            Name = "Inlet2",
            Temperature = 300,
            Pressure = 101325,
            MassFlow = 2.0,
            MolarCompositions = new Dictionary<string, double> { { "Water", 1.0 } }
        };
        var inlet2Res = await HttpClient.PostAsJsonAsync($"/api/v1/simulations/{simId}/streams", inlet2Request);
        var inlet2Data = await inlet2Res.Content.ReadFromJsonAsync<JsonElement>();
        Guid inlet2Id = inlet2Data.GetProperty("streamId").GetGuid();

        var outletRequest = new AddStreamRequest
        {
            Name = "Outlet",
            Temperature = 300,
            Pressure = 101325,
            MassFlow = 0.0,
            MolarCompositions = new Dictionary<string, double> { { "Water", 1.0 } }
        };
        var outletRes = await HttpClient.PostAsJsonAsync($"/api/v1/simulations/{simId}/streams", outletRequest);
        var outletData = await outletRes.Content.ReadFromJsonAsync<JsonElement>();
        Guid outletId = outletData.GetProperty("streamId").GetGuid();

        // 4. Add Mixer
        var addUnitRequest = new AddUnitRequest
        {
            Name = "Mixer1",
            UnitOperation = UnitOperationType.Mixer
        };
        var mixerRes = await HttpClient.PostAsJsonAsync($"/api/v1/simulations/{simId}/units", addUnitRequest);
        var mixerData = await mixerRes.Content.ReadFromJsonAsync<JsonElement>();
        Guid mixerId = mixerData.GetProperty("unitId").GetGuid();

        // 5. Connect
        await HttpClient.PutAsJsonAsync($"/api/v1/simulations/{simId}/connect", new ConnectStreamRequest
        {
            UnitId = mixerId,
            StreamId = inlet1Id,
            PortType = PortType.Inlet
        });
        await HttpClient.PutAsJsonAsync($"/api/v1/simulations/{simId}/connect", new ConnectStreamRequest
        {
            UnitId = mixerId,
            StreamId = inlet2Id,
            PortType = PortType.Inlet
        });
        await HttpClient.PutAsJsonAsync($"/api/v1/simulations/{simId}/connect", new ConnectStreamRequest
        {
            UnitId = mixerId,
            StreamId = outletId,
            PortType = PortType.Outlet
        });

        // 6. Submit Job
        var submitRes = await HttpClient.PostAsJsonAsync("/api/v1/simulation_jobs", new SubmitJobRequest { SimulationId = simId });
        submitRes.StatusCode.Should().Be(HttpStatusCode.Accepted);

        // 7. Poll for completion
        string status = "Pending";
        int attempts = 0;
        while (status != "Converged" && status != "Failed" && attempts < 30)
        {
            await Task.Delay(1000);
            var statusRes = await HttpClient.GetAsync($"/api/v1/simulation_jobs/{simId}/status");
            if (!statusRes.IsSuccessStatusCode)
            {
                 var err = await statusRes.Content.ReadAsStringAsync();
                 throw new Exception($"Poll failed. Status: {statusRes.StatusCode}, Body: {err}");
            }
            var statusData = await statusRes.Content.ReadFromJsonAsync<JsonElement>();
            
            if (!statusData.TryGetProperty("status", out var statusProp))
            {
                var body = statusData.GetRawText();
                throw new Exception($"JSON missing 'status' property. Body: {body}");
            }
            status = statusProp.GetString()!;
            attempts++;
        }

        status.Should().Be("Converged", because: "the mixer simulation should solve correctly");

        // 8. Verify Results
        var resultRes = await HttpClient.GetAsync($"/api/v1/simulation_jobs/{simId}/result");
        resultRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var resultData = await resultRes.Content.ReadFromJsonAsync<JsonElement>();
        
        // Assert mass flow: 1.0 + 2.0 = 3.0
        double outletMassFlow = resultData.GetProperty("results").GetProperty("materialStreams").GetProperty("Outlet").GetProperty("massFlow").GetDouble();
        outletMassFlow.Should().BeApproximately(3.0, 0.001);
    }

    [Fact]
    public async Task Should_Fail_On_Disconnected_Stream()
    {
        // 1. Create Simulation
        var createRequest = new CreateSimulationRequest
        {
            Name = "Failure Test",
            ThermoPackage = "PengRobinson",
            FlashAlgorithm = "NestedLoops",
            SystemOfUnits = "SI"
        };
        var response = await HttpClient.PostAsJsonAsync("/api/v1/simulations", createRequest);
        var simData = await response.Content.ReadFromJsonAsync<JsonElement>();
        Guid simId = simData.GetProperty("id").GetGuid();

        // Add compound
        await HttpClient.PostAsJsonAsync($"/api/v1/simulations/{simId}/compounds", new { name = "Water" });

        // Add Mixer but NO STREAMS
        var addUnitRequest = new AddUnitRequest
        {
            Name = "LonelyMixer",
            UnitOperation = UnitOperationType.Mixer
        };
        await HttpClient.PostAsJsonAsync($"/api/v1/simulations/{simId}/units", addUnitRequest);

        // 2. Submit Job
        await HttpClient.PostAsJsonAsync("/api/v1/simulation_jobs", new SubmitJobRequest { SimulationId = simId });

        // 3. Poll for failure
        string status = "Pending";
        int attempts = 0;
        while (status != "Converged" && status != "Failed" && attempts < 20)
        {
            await Task.Delay(1000);
            var statusRes = await HttpClient.GetAsync($"/api/v1/simulation_jobs/{simId}/status");
            var statusData = await statusRes.Content.ReadFromJsonAsync<JsonElement>();
            status = statusData.GetProperty("status").GetString()!;
            attempts++;
        }

        status.Should().Be("Failed");

        // 4. Verify structured error
        var resultRes = await HttpClient.GetAsync($"/api/v1/simulation_jobs/{simId}/result");
        resultRes.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var errorData = await resultRes.Content.ReadFromJsonAsync<JsonElement>();
        
        string code = errorData.GetProperty("code").GetString()!;
        code.Should().Be("SimulationFailed");
        string message = errorData.GetProperty("message").GetString()!;
        message.Should().NotBeNullOrEmpty();
        errorData.GetProperty("context").GetProperty("simulationId").GetGuid().Should().Be(simId);
    }
}
