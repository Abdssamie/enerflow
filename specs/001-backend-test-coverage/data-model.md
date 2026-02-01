# Data Model: Backend Test Coverage & MVP Readiness

**Feature**: Backend Test Coverage & MVP Readiness Assessment  
**Date**: 2025-01-30  
**Phase**: 1 - Design

## Overview

This document defines test-specific entities and data structures used for organizing, tracking, and reporting on backend test coverage. These entities complement the existing Enerflow domain model (Simulation, Compound, Stream, UnitOperation) by providing test infrastructure and reporting capabilities.

**Note**: This feature tests existing domain entities rather than creating new business entities. The data model here focuses on test organization and coverage tracking.

## Test Organization Entities

### 1. Test Suite

**Purpose**: Organize tests by type and layer for structured execution and reporting

**Attributes**:
- `Name` (string): Descriptive name (e.g., "API Controller Tests", "Worker Consumer Tests")
- `Type` (enum): Unit | Integration | Functional | Performance
- `Layer` (enum): API | Worker | Service | Infrastructure | Domain
- `TestCount` (int): Total number of tests in suite
- `PassCount` (int): Number of passing tests
- `FailCount` (int): Number of failing tests
- `SkipCount` (int): Number of skipped tests
- `ExecutionTime` (TimeSpan): Total execution time
- `LastRun` (DateTime): Timestamp of last execution

**Relationships**:
- Contains many `Test Case` entities
- Produces one `Test Coverage Report` per layer

**Example**:
```csharp
public class TestSuite
{
    public string Name { get; set; } = string.Empty;
    public TestType Type { get; set; }
    public LayerType Layer { get; set; }
    public int TestCount { get; set; }
    public int PassCount { get; set; }
    public int FailCount { get; set; }
    public int SkipCount { get; set; }
    public TimeSpan ExecutionTime { get; set; }
    public DateTime LastRun { get; set; }
    public List<TestCase> TestCases { get; set; } = new();
}

public enum TestType { Unit, Integration, Functional, Performance }
public enum LayerType { API, Worker, Service, Infrastructure, Domain }
```

### 2. Test Case

**Purpose**: Individual test with pass/fail status and execution details

**Attributes**:
- `Name` (string): Test method name
- `FullName` (string): Fully qualified name including namespace and class
- `Status` (enum): Passed | Failed | Skipped
- `ExecutionTime` (TimeSpan): Individual test execution time
- `ErrorMessage` (string?): Error message if failed
- `StackTrace` (string?): Stack trace if failed
- `Traits` (Dictionary<string, string>): Test metadata (category, priority, etc.)

**Relationships**:
- Belongs to one `Test Suite`
- May reference `Test Fixture` for shared setup

**Example**:
```csharp
public class TestCase
{
    public string Name { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public TestStatus Status { get; set; }
    public TimeSpan ExecutionTime { get; set; }
    public string? ErrorMessage { get; set; }
    public string? StackTrace { get; set; }
    public Dictionary<string, string> Traits { get; set; } = new();
}

public enum TestStatus { Passed, Failed, Skipped }
```

### 3. Test Fixture

**Purpose**: Reusable test data and setup logic shared across multiple tests

**Attributes**:
- `Name` (string): Fixture identifier
- `SetupLogic` (Action): Initialization code
- `ownLogic` (Action): Cleanup code
- `SharedState` (Dictionary<string, object>): Shared data between tests
- `Scope` (enum): PerTest | PerClass | PerAssembly

**Relationships**:
- Used by many `Test Case` entities
- May depend on `Test Container Configuration`

**Example**:
```csharp
public class TestFixture
{
    public string Name { get; set; } = string.Empty;
    public Action? SetupLogic { get; set; }
    public Action? TeardownLogic { get; set; }
    public Dictionary<string, object> SharedState { get; set; } = new();
    public FixtureScope Scope { get; set; }
}

public enum FixtureScope { PerTest, PerClass, PerAssembly }
```

## Test Infrastructure Entities

### 4. Test Container Configuration

**Purpose**: Define containerized dependencies for functional tests (Postgres, RabbitMQ)

**Attributes**:
- `ContainerType` (enum): Postgres | RabbitMQ | Redis
- `ImageName` (string): Docker image (e.g., "postgres:15-alpine")
- `ImageTag` (string): Image version tag
- `PortMappings` (Dictionary<int, int>): Container port → Host port
- `EnvironmentVariables` (Dictionary<string, string>): Container env vars
- `WaitStrategy` (enum): UntilPortAvailable | UntilLogMessage | UntilHttpOk
- `StartupTimeout` (TimeSpan): Maximum time to wait for container startup

**Relationships**:
- Used by `Functional Test Suite`
- Provides connection details to `Test Fixture`

**Example**:
```csharp
public class TestContainerConfiguration
{
    public ContainerType ContainerType { get; set; }
    public string ImageName { get; set; } = string.Empty;
    public string ImageTag { get; set; } = "latest";
    public Dictionary<int, int> PortMappings { get; set; } = new();
    public Dictionary<string, string> EnvironmentVariables { get; set; } = new();
    public WaitStrategy WaitStrategy { get; set; }
    public TimeSpan StartupTimeout { get; set; } meSpan.FromSeconds(60);
    
    public string GetConnectionString() => ContainerType switch
    {
        ContainerType.Postgres => $"Host=localhost;Port={PortMappings[5432]};Database={EnvironmentVariables["POSTGRES_DB"]};Username={EnvironmentVariables["POSTGRES_USER"]};Password={EnvironmentVariables["POSTGRES_PASSWORD"]}",
        ContainerType.RabbitMQ => $"amqp://guest:guest@localhost:{PortMappings[5672]}/",
        _ => throw new NotSupportedException()
    };
}

public enum ContainerType { Postgres, RabbitMQ, Redis }
public enum WaitStrategy { UntilPortAvailable, UntilLogMessage, UntilHttpOk }
```
 Coverage Entities

### 5. Test Coverage Report

**Purpose**: Track code coverage metrics by layer and module

**Attributes**:
- `Layer` (enum): API | Worker | Service | Infrastructure | Domain
- `ModuleName` (string): Assembly or project name
- `LineCoverage` (double): Percentage of lines covered (0-100)
- `BranchCoverage` (double): Percentage of branches covered (0-100)
- `MethodCoverage` (double): Percentage of methods covered (0-100)
- `TargetCoverage` (double): Target coverage percentage for this layer
- `CoverageGap` (double): Difference between target and actual (target - actual)
- `TotalLines` (int): Total lines of code
- `CoveredLines` (int): Lines covered by tests
- `UncoveredLines` (int): Lines not covered by tests
- `GeneratedAt` (DateTime): Report generation timestamp

**Relationships**:
- Aggregates coverage from one `Test Suite`
- Contributes to `MVP Readiness Checklist`

**Example**:
```csharp
public class TestCoverageReport
{
    public LayerType Layer { get; set; }
    public string ModuleName { get; set; } = string.Empty;
    public double LineCoverage { get; set; }
    public double BranchCoverage { get; set; }
    public double MethodCoverage { get; set; }
    public double TargetCoverage { get; set; }
    public double CoverageGap => TargetCoverage - LineCoverage;
    public int TotalLines { get; set; }
    public int CoveredLines { get; set; }
    public int UncoveredLines => TotalLines - CoveredLines;
    public DateTime GeneratedAt { get; set; }
    
    public bool MeetsTarget => LineCoverage >= TargetCoverage;
}
```

### 6. Coverage Gap

**Purpose**: Identify specific untested code areas requiring test implementation

**Attributes**:
- `Layer` (enum): API | Worker | Service | Infrastructure | Domain
- `FileName` (string): Source file path
- `ClassName` (string): Class name
- `MethodName` (string): Method `LineNumbers` (List<int>): Uncovered line numbers
- `Priority` (enum): Critical | High | Medium | Low
- `Complexity` (int): Cyclomatic complexity of uncovered code
- `EstimatedEffort` (TimeSpan): Estimated time to write tests

**Relationships**:
- Derived from `Test Coverage Report`
- Tracked in `MVP Readiness Checklist`

**Example**:
```csharp
public class CoverageGap
{
    public LayerType Layer { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public string MethodName { get; set; } = string.Empty;
    public List<int> LineNumbers { get; set; } = new();
    public GapPriority Priority { get; set; }
    public int Complexity { get; set; }
    public TimeSpan EstimatedEffort { get; set; }
}

public enum GapPriority { Critical, High, Medium, Low }
```

## Performance Testing Entities

### 7. Performance Test Scenario

**Purpose**: Define load testing scenarios with expected performance targets

**Attributes**:
- `Name` (string): Scenario identifier
- `TargetEndpoint` (string): API endpoint to test
- `HttpMethod` (string): GET | POST | PUT | DELETE
- `RequestBody` (string?): JSON request payload
- `ConcurrentUsers` (int): Number of simulated concurrent users
- `Duration` (TimeSpan): Test duration
- `RampUpTime` (TimeSpan): Time to reach full concurrent load
- `ExpectedThroughput` (double): Expected requests per second
- `TargetP50` (TimeSpan): Target 50th percentile latency
- `TargetP95` (TimeSpan): Target 95th percentile latency
- `TargetP99` (TimeSpan): Target 99th percentile latency

**Relationships**:
- Produces `Performance Test Result`
- Validates against `Performance Benchmark`

**Example**:
```csharp
public class PerformanceTestScenario
{
    public string Name { get; set; } = string.Empty;
    public string TargetEndpoint { get; set; } = string.Empty;
    public string HttpMethod { get; set; } = "GET";
    public string? RequestBody { get; set; }
    public int ConcurrentUsers { get; set; }
    public TimeSpan Duration { get; set; }
    public TimeSpan RampUpTime { get; set; }
    public double ExpectedThroughput { get; set; }
    public TimeSpan TargetP50 { get; set; }
    public TimeSpan TargetP95 { get; set; }
    public TimeSpan TargetP99 { get; set; }
}
```

### 8. Performance Test Result

**Purpose**: Capture actual performance metrics from load test execution

ributes**:
- `ScenarioName` (string): Reference to scenario
- `ExecutedAt` (DateTime): Test execution timestamp
- `TotalRequests` (int): Total requests sent
- `SuccessfulRequests` (int): Requests with 2xx/3xx status
- `FailedRequests` (int): Requests with 4xx/5xx status or timeouts
- `ActualThroughput` (double): Actual requests per second
- `ActualP50` (TimeSpan): Actual 50th percentile latency
- `ActualP95` (TimeSpan): Actual 95th percentile latency
- `ActualP99` (TimeSpan): Actual 99th percentile latency
- `MinLatency` (TimeSpan): Minimum observed latency
- `MaxLatency` (TimeSpan): Maximum observed latency
- `MeetsTargets` (bool): Whether all targets were met

**Relationships**:
- Produced by `Performance Test Scenario`
- Contributes to `MVP Readiness Checklist`

**Example**:
```csharp
public class PerformanceTestResult
{
    public string ScenarioName { get; set; } = string.Empty;
    public DateTime ExecutedAt { get; set; }
    public int TotalRequests { get; set; }
    public int SuccessfulRequests { get; set; }
    public int FailedRequests { get; set; }
    public double ActualThroughput { get; set; }
    public TimeSpan ActualP50 { get; set; }
    public T ActualP95 { get; set; }
    public TimeSpan ActualP99 { get; set; }
    public TimeSpan MinLatency { get; set; }
    public TimeSpan MaxLatency { get; set; }
    public bool MeetsTargets { get; set; }
}
```

## MVP Readiness Entities

### 9. MVP Readiness Checklist

**Purpose**: Track MVP readiness criteria and overall backend status

**Attributes**:
- `Criterion` (string): Success criterion identifier (e.g., "SC-001")
- `Description` (string): Criterion description
- `Status` (enum): Passed | Failed | InProgress | NotStarted
- `Evidence` (string): Supporting evidence (coverage report, test results, etc.)
- `Blocker` (string?): Description of blocker if failed
- `Phase` (int): Phase number (0, 1, 2, 3)
- `ValidatedAt` (DateTime?): Timestamp when criterion was validated

**Relationships**:
- References `Test Coverage Report`, `Performance Test Result`, `Incomplete Feature Status`
- Aggregates to overall MVP readiness decision

**Example**:
```csharp
public class MVPReadinessChecklist
{
    public string Criterion { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ReadinessStatus Status { get; set; }
    public string Evidence { get; set; } = string.Empty;
    public string? Blocker { get; set; }
    public int Phase { get; set; }
    public DateTime? ValidatedAt { get; set; }
}

public enum ReadinessStatus { Passed, Failed, InProgress, NotStarted }
```

### 10. Incomplete Feature Status

**Purpose**: Track completion status of 4 identified incomplete features

**Attributes**:
- `FeatureName` (string): Feature identifier
- `FileLocation` (string): Source file and line number
- `Description` (string): What needs to be implemented
- `Status` (enum): NotStarted | InProgress | Implemented | Tested
- `TestCoverage`): Coverage percentage for this feature
- `CompletedAt` (DateTime?): Timestamp when feature was completed

**Relationships**:
- Contributes to `MVP Readiness Checklist`
- Has associated `Test Case` entities

**Example**:
```csharp
public class IncompleteFeatureStatus
{
    public string FeatureName { get; set; } = string.Empty;
    public string FileLocation { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public FeatureStatus Status { get; set; }
    public double TestCoverage { get; set; }
    public DateTime? CompletedAt { get; set; }
    
    public bool IsComplete => Status == FeatureStatus.Tested && TestCoverage >= 80.0;
}

public enum FeatureStatus { NotStarted, InProgress, Implemented, Tested }
```

## Entity Relationships Diagram

```
┌─────────────────┐
│   Test Suite    │
│  (Organizes)    │
└────────┬────────┘
         │ 1:N
         ▼
┌─────────────────┐       ┌──────────────────┐
│   Test Case     │──────▶│  Test Fixture    │
│  (Individual)   │  N:1  │  (Shared Setup)  │
└─────────────────┘       └────────┬─────────┘
                                   │ N:1
                                   ▼
                          ┌──────────────────┐
                          │ Test Container Config│
                          │  (Infrastructure)    │
                          └──────────────────────┘

┌─────────────────┐       ┌──────────────────┐
│   Test Suite    │──────▶│ Coverage Report  │
│                 │  1:1  │  (Metrics)       │
└─────────────────┘       └────────┬─────────┘
                                   │ 1:N
                                   ▼
                          ┌──────────────────┐
                          │  Coverage Gap    │
                          │  (Untested Code) │
                          └──────────────────┘

┌──────────────────────┐       ┌──────────────────────┐
│ Performance Scenario │──────▶│ Performance Result   │
│  (Load Test Def)     │  1:N  │  (Actual Metrics)    │
└──────────────────────┘       └──────────┬───────────┘
                                          │
                                          │ N:1
                                          ▼
                               ┌──────────────────────┐
                               │ MVP Readiness        │
                               │ Checklist            │
                               │ (Overall Status │
                               └──────────┬───────────┘
                                          │ N:1
                                          ▼
                               ┌──────────────────────┐
                               │ Incomplete Feature   │
                               │ Status               │
                               └──────────────────────┘
```

## Validation Rules

### Test Suite
- `TestCount` must equal `PassCount + FailCount + SkipCount`
- `ExecutionTime` must be positive
- `LastRun` must be in the past

### Test Coverage Report
- `LineCoverage`, `BranchCoverage`, `MethodCoverage` must be between 0 and 100
- `CoveredLines` must be ≤ `TotalLines`
- `TargetCoverage` must match layer requirements (API: 80%, Worker: 80%, Service: 70%, Infrastructure: 70%, Domain: 70%)

### Performance Test Result
- `SuccessfulRequests + FailedRequests` must equal `TotalRequests`
- All latency values must be positive
- `MinLatency` ≤ `ActualP50` ≤ `ActualP95` ≤ `ActualP99` ≤ `MaxLatency`

### MVP Readiness Checklist
- `Status` = Passed only if `Evidence` is provided and `Blocker` is null
- `ValidatedAt` must be set when `Status` = Passed

## State Transitions

### Test Case Status
```
NotStarted → InProgress → Pas                     → Failed → InProgress (retry)
                       → Skipped
```

### Incomplete Feature Status
```
NotStarted → InProgress → Implemented → Tested
```

### MVP Readiness Status
```
NotStarted → InProgress → Passed
                       → Failed → InProgress (remediation)
```

## Storage Considerations

**Note**: These entities are primarily used for reporting and tracking during the testing phase. They do not require persistent database storage in the Enerflow production database.

**Storage Options**:
1. **In-Memory**: Test results stored in memory during test execution, exported to files/reports
2. **File-Based**: JSON/XML files for test results and coverage reports (standard xUnit/Coverlet output)
3. **CI/CD Artifacts**: Test reports uploaded as build artifacts in GitHub Actions/Azure DevOps
4. **Optional Dashboard**: If test reporting dashboard is implemented, store in separate reporting database

**Recommended Approach**: Use standard xUnit and Coverlet output formats, store as CI/CD artifacts, generate HTML reports with ReportGenerator. No custom database storage required.

---

**Data Model Status**: ✅ Complete  
**Next Step**: Create contracts documentation (test-patterns.md, coverage-targets.md, incomplete-features.md)
