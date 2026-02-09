# Fix Functional Test MassTransit Connection (TODO #4)

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Fix MassTransit `SqlTransportOptions` configuration so functional tests can run without connection string conflicts.

**Architecture:** Ensure test factory's SQL transport configuration fully replaces API's configuration instead of being additive.

**Tech Stack:** C#, MassTransit, xUnit, SQL Server

---

## Task 1: Investigate Configuration Issue

**Files:**
- Read: `Enerflow.Tests.Functional/TestWebApplicationFactory.cs`
- Read: `Enerflow.API/Program.cs`

**Step 1: Document current configuration pattern**

Identify where `SqlTransportOptions` is configured in both API and test factory.

**Step 2: Research MassTransit configuration**

Check MassTransit docs for proper test configuration override patterns.

**Step 3: Document findings**

Create `docs/TESTING/MASSTRANSIT_TEST_CONFIG.md` with findings.

**Step 4: Commit**

```bash
git add docs/TESTING/MASSTRANSIT_TEST_CONFIG.md
git commit -m "docs: document MassTransit test configuration issue"
```

---

## Task 2: Write Failing Test

**Files:**
- Modify: `Enerflow.Tests.Functional/Scenarios/SimulationFlowTests.cs`

**Step 1: Uncomment or write basic functional test**

```csharp
[Fact]
public async Task CreateAndRunSimulation_ValidInput_ReturnsResults()
{
    // Arrange
    var client = _factory.CreateClient();
    var simulation = CreateTestSimulation();
    
    // Act
    var createResponse = await client.PostAsJsonAsync("/api/simulations", simulation);
    createResponse.EnsureSuccessStatusCode();
    
    var simId = await createResponse.Content.ReadFromJsonAsync<Guid>();
    var runResponse = await client.PostAsync($"/api/simulations/{simId}/run", null);
    
    // Assert
    runResponse.EnsureSuccessStatusCode();
}
```

**Step 2: Run test**

```bash
dotnet test Enerflow.Tests.Functional --filter "FullyQualifiedName~SimulationFlowTests" -v n
```

Expected: FAIL with connection string error

**Step 3: Commit**

```bash
git add Enerflow.Tests.Functional/Scenarios/SimulationFlowTests.cs
git commit -m "test: add functional test to reproduce MassTransit config issue"
```

---

## Task 3: Fix Configuration Override

**Files:**
- Modify: `Enerflow.Tests.Functional/TestWebApplicationFactory.cs`

**Step 1: Replace additive configuration with override**

Change from:
```csharp
services.AddMassTransit(x =>
{
    x.AddSqlMessageScheduler();
    x.UsingInMemory((context, cfg) => { /* ... */ });
});
```

To:
```csharp
// Remove existing MassTransit registrations
services.RemoveAll<IBus>();
services.RemoveAll<IPublishEndpoint>();
services.RemoveAll(typeof(IRequestClient<>));

// Add fresh test configuration
services.AddMassTransit(x =>
{
    x.UsingInMemory((context, cfg) =>
    {
        cfg.UseInMemoryScheduler();
        cfg.ConfigureEndpoints(context);
    });
});
```

**Step 2: Run test**

```bash
dotnet test Enerflow.Tests.Functional --filter "FullyQualifiedName~SimulationFlowTests" -v n
```

Expected: PASS

**Step 3: Commit**

```bash
git add Enerflow.Tests.Functional/TestWebApplicationFactory.cs
git commit -m "fix: properly override MassTransit configuration in functional tests"
```

---

## Task 4: Verify All Functional Tests

**Files:**
- Run: All functional tests

**Step 1: Run full functional test suite**

```bash
dotnet test Enerflow.Tests.Functional -v n
```

Expected: All tests PASS

**Step 2: Commit if additional fixes needed**

```bash
git add .
git commit -m "fix: address remaining functional test issues"
```
