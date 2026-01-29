# Quickstart Guide: Backend Testing

**Feature**: Backend Test Coverage & MVP Readiness Assessment  
**Date**: 2025-01-30  
**Phase**: 1 - Design

## Overview

This guide provides step-by-step instructions for running tests, generating coverage reports, and validating MVP readiness for the Enerflow backend.

## Prerequisites

- .NET 10.0 SDK installed
- Docker installed and running (for Testcontainers)
- Git repository cloned
- All NuGet packages restored

```bash
# Verify prerequisites
dotnet --version  # Should show 10.0.x
docker --version  # Should show Docker version
git status        # Should show clean working directory

# Restore packages
dotnet restore
```

## Running Tests

### Run All Tests

```bash
# Run all test projects
dotnet test

# Run with detailed output
dotnet test --verbosity normal

# Run with logger for better formatting
dotnet test --logger "console;verbosity=detailed"
```

### Run Tests by Project

```bash
# Unit tests only
dotnet test Enerflow.Tests.Unit/Enerflow.Tests.Unit.csproj

# Integration tests only
dotnet test Enerflow.Tests.Integration/Enerflow.Tests.Integration.csproj

# Functional tests only (requires Docker)
dotnet test Enerflow.Tests.Functional/Enerflow.Tests.Functional.csproj

# DWSIM scenario tests
dotnet test Enerflow.Tests.DWSIM/Enerflow.Tests.DWSIM.csproj

# Performance tests (if implemented)
dotnet test Enerflow.Tests.Performance/Enerflow.Tests.Performance.csproj
```

### Run Tests by Category

```bash
# Run only API tests
dotnet test --filter "Layer=API"

# Run only Worker tests
dotnet test --filter "Layer=Worker"

# Run only unit tests
dotnet test --filter "Category=Unit"

# Run only integration tests
dotnet test --filter "Category=Integration"

# Run fast tests only (exclude slow functional tests)
dotnet test --filter "Speed!=Slow"
```

### Run Specific Test

```bash
# Run single test by full name
dotnet test --filter "FullyQualifiedName=Enerflow.Tests.Unit.API.Controllers.SimulationsControllerTests.CreateSimulation_WithValidData_ReturnsCreatedResult"

# Run all tests in a class
dotnet test --filter "FullyQualifiedName~SimulationsControllerTests"

# Run tests matching pattern
dotnet test --filter "Name~CreateSimulation"
```

## Generating Coverage Reports

### Basic Coverage

```bash
# Run tests with coverage collection
dotnet test /p:CollectCoverage=true

# Specify output format (cobertura for CI/CD)
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura

# Specify output location
dotnet test /p:CollectCoverage=true /p:CoverletOutput=./TestResults/
```

### Coverage by Layer

```bash
# API layer coverage
dotnet test Enerflow.Tests.Unit/Enerflow.Tests.Unit.csproj \
  /p:CollectCoverage=true \
  /p:Include="[Enerflow.API]*" \
  /p:CoverletOutputFormat=cobertura

# Worker layer coverage
dotnet test Enerflow.Tests.Integration/Enerflow.Tests.Integration.csproj \
  /p:CollectCoverage=true \
  /p:Include="[Enerflow.Worker]*" \
  /p:CoverletOutputFormat=cobertura

# Service layer coverage
dotnet test /p:CollectCoverage=true \
  /p:Include="[Enerflow.Simulation]*" \
  /p:CoverletOutputFormat=cobertura

# Infrastructure layer coverage
dotnet test /p:CollectCoverage=true \
  /p:Include="[Enerflow.Infrastructure]*" \
  /p:CoverletOutputFormat=cobertura
```

### HTML Coverage Report

```bash
# Install ReportGenerator (one-time)
dotnet tool install -g dotnet-reportgenerator-globaltool

# Run tests with coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura /p:CoverletOutput=./TestResults/

# Generate HTML report
reportgenerator \
  -reports:"**/TestResults/coverage.cobertura.xml" \
  -targetdir:"TestResults/CoverageReport" \
  -reporttypes:"Html;Badges"

# Open report in browser
# Linux/Mac
open TestResults/CoverageReport/index.html
# Windows
start TestResults/CoverageReport/index.html
```

### Coverage with Thresholds

```bash
# Fail if coverage below 80%
dotnet test \
  /p:CollectCoverage=true \
  /p:Threshold=80 \
  /p:ThresholdType=line \
  /p:ThresholdStat=total

# Layer-specific thresholds (requires custom script)
./scripts/validate-coverage-targets.sh
```

## Running Functional Tests

### Prerequisites for Functional Tests

```bash
# Ensure Docker is running
docker ps

# Pull required images (optional, will auto-pull)
docker pull postgres:15-alpine
docker pull rabbitmq:3-management-alpine
```

### Run Functional Tests

```bash
# Run all functional tests
dotnet test Enerflow.Tests.Functional/Enerflow.Tests.Functional.csproj

# Run with detailed logging
dotnet test Enerflow.Tests.Functional/Enerflow.Tests.Functional.csproj \
  --logger "console;verbosity=detailed"

# Run specific scenario
dotnet test Enerflow.Tests.Functional/Enerflow.Tests.Functional.csproj \
  --filter "FullyQualifiedName~SimulationFlowTests"
```

### Troubleshooting Functional Tests

**Issue**: Testcontainers fail to start

```bash
# Check Docker is running
docker ps

# Check Docker resources (memory, CPU)
docker info

# Clean up old containers
docker container prune -f
docker volume prune -f
```

**Issue**: "Connection refused" error

```bash
# Check if ports are available
netstat -an | grep 5432  # Postgres
netstat -an | grep 5672  # RabbitMQ

# Kill processes using ports
# Linux/Mac
lsof -ti:5432 | xargs kill -9
# Windows
netstat -ano | findstr :5432
taskkill /PID <PID> /F
```

**Issue**: Tests timeout

```bash
# Increase test timeout
dotnet test --blame-hang-timeout 5m

# Run tests sequentially (not parallel)
dotnet test -- NUnit.NumberOfTestWorkers=1
```

## Running Performance Tests

### Prerequisites

```bash
# Ensure API is running
dotnet run --project Enerflow.API/Enerflow.API.csproj

# Or run in Docker
docker-compose up -d
```

### Run Performance Tests

```bash
# Run all performance tests
dotnet test Enerflow.Tests.Performance/Enerflow.Tests.Performance.csproj

# Run specific load test
dotnet test Enerflow.Tests.Performance/Enerflow.Tests.Performance.csproj \
  --filter "FullyQualifiedName~SimulationSubmissionLoadTests"

# Generate performance report
# NBomber generates HTML reports automatically in bin/Debug/net10.0/reports/
```

### Interpreting Performance Results

```
Scenario: simulation_submission
├── Total Requests: 1200
├── Successful: 1195 (99.6%)
├── Failed: 5 (0.4%)
├── RPS: 10.0
├── Latency:
│   ├── Min: 45ms
│   ├── p50: 98ms ✅ (target: <100ms)
│   ├── p95: 456ms ✅ (target: <500ms)
│   └── p99: 892ms ✅ (target: <1000ms)
└── Status: PASSED
```

## Validating MVP Readiness

### Check Coverage Targets

```bash
# Run all tests with coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura

# Generate report
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"CoverageReport" -reporttypes:"Html"

# Open report and verify:
# - API Layer: ≥80% ✅
# - Worker Layer: ≥80% ✅
# - Service Layer: ≥70% ✅
# - Infrastructure Layer: ≥70% ✅
# - Domain Layer: ≥70% ✅
```

### Run MVP Readiness Checklist

```bash
# Run validation script (to be created)
./scripts/validate-mvp-readiness.sh

# Expected output:
# ✅ SC-000: Functional tests unblocked
# ✅ SC-001: API layer 80% coverage
# ✅ SC-002: Worker layer 80% coverage
# ✅ SC-003: Infrastructure layer 70% coverage
# ✅ SC-004: Service layer 70% coverage
# ✅ SC-005: All functional tests pass
# ✅ SC-006: Zero critical bugs
# ✅ SC-007: 4 incomplete features completed
# ✅ SC-008: Feature tests pass
# ✅ SC-009: Test suite <10min
# ✅ SC-010: Flaky tests <5%
# ✅ SC-011: Performance targets met
# ✅ SC-012: CI/CD automated
# ✅ SC-013: Local test execution works
# ✅ SC-014: MVP assessment complete
# ✅ SC-015: MVP READY status
#
# MVP Status: ✅ READY
```

## CI/CD Integration

### GitHub Actions

```yaml
# .github/workflows/test-coverage.yml
name: Test Coverage

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    
    services:
      postgres:
        image: postgres:15-alpine
        env:
          POSTGRES_PASSWORD: postgres
        options: >-
          --health-cmd pg_isready
          --health-interval 10s
          --health-timeout 5s
        --health-retries 5
        ports:
          - 5432:5432
      
      rabbitmq:
        image: rabbitmq:3-management-alpine
        ports:
          - 5672:5672
          - 15672:15672
    
    steps:
      - uses: actions/checkout@v3
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '10.0.x'
      
      - name: Restore dependencies
        run: dotnet restore
      
      - name: Run tests with coverage
        run: |
          dotnet test \
            --no-restore \
            --verbosity normal \
            /p:CollectCoverage=true \
            /p:CoverletOutputFormat=cobertura \
            /p:CoverletOutput=./TestResults/
      
      - name: Generate coverage report
        run: |
          dotnet tool install -g dotnet-reportgenerator-globaltool
          reportgenerator \
            -reports:"**/TestResults/coverage.cobertura.xml" \
            -targetdir:"CoverageReport" \
            -reporttypes:"Html;Badges"
      
      - name: Upload coverage report
        uses: actions/upload-artifact@v3
        with:
          name: coverage-report
          path: CoverageReport/
      
      - name: Check coverage thresholds
        run: ./scripts/validate-coverage-targets.sh
```

### Azure DevOps

```yaml
# azure-pipelines.yml
trigger:
  - master
  - develop

pool:
  vmImage: 'ubuntu-latest'

steps:
- task: UseDotNet@2
  inputs:
    version: '10.0.x'

- task: DotNetCoreCLI@2
  displayName: 'Restore packages'
  inputs:
    command: 'restore'

- task: DotNetCoreCLI@2
  displayName: 'Run tests with coverage'
  inputs:
    command: 'test'
    arguments: '/p:CollectCoverage=true /p:CoverletOutputFormat=cobertura'
    publishTestResults: true

- script: |
    dotnet tool install -g dotnet-reportgenerator-globaltool
    reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"CoverageReport" -reporttypes:"HtmlInline_AzurePipelines;Cobertura"
  displayName: 'Generate coverage report'

- task: PublishCodeCoverageResults@1
  inputs:
    codeCoverageTool: 'Cobertura'
    summaryFileLocation: '**/coverage.cobertura.xml'
    reportDirectory: 'CoverageReport'
```

## Common Issues & Solutions

### Issue: Tests fail locally but pass in CI

**Solution**: Ensure consistent environment

```bUse Docker for consistent environment
docker-compose -f docker-compose.test.yml up -d
dotnet test
docker-compose -f docker-compose.test.yml down
```

### Issue: Slow test execution

**Solution**: Run tests in parallel

```bash
# Enable parallel execution (default)
dotnet test --parallel

# Limit parallelism
dotnet test -- NUnit.NumberOfTestWorkers=4
```

### Issue: Flaky tests

**Solution**: Identify and fix flaky tests

```bash
# Run tests multiple times
for i in {1..10}; do
  dotnet test --logger "trx;LogFileName=test_run_$i.trx"
done

# Analyze results for intermittent failures
./scripts/analyze-flaky-tests.sh
```

### Issue: Coverage report not generated

**Solution**: Check Coverlet configuration

```bash
# Verify Coverlet is installed
dotnet list package | grep coverlet

# Install if missing
dotnet add package coverlet.collector
dotnet add package coverlet.msbuild

# Run with explicit output
dotnet test /p:CollectCoverage=true /p:CoverletOutput=./coverage.json
```

## Best Practices

1. **Run tests before committing**
   ```bash
   git add .
   dotnet test
   git commit -m "Your message"
 n2. **Generate coverage reports regularly**
   ```bash
   # Weekly coverage check
   dotnet test /p:CollectCoverage=true
   reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"CoverageReport"
   ```

3. **Keep tests fast**
   - Unit tests: <100ms each
   - Integration tests: <1s each
   - Functional tests: <10s each

4. **Isolate test data**
   - Use test fixtures for setup/teardown
   - Don't share data between tests
   - Clean up after each test

5. **Monitor flaky tests**
   - Track flaky test rate (target: <5%)
   - Fix flaky tests immediately
   - Use proper async/await patterns

## Next Steps

1. **Fix Critical Blocker**: Resolve Testcontainer/MassTransit/Postgres connection issue
2. **Implement API Tests**: Achieve 80% coverage on API layer
3. **Implement Worker Tests**: Achieve 80% coverage on Worker layer
4. **Complete Incomplete Features**: Implement 4 TODO features
5. **Validate MVP Readiness**: Run full validation checklist

## Resources

- **xUnit Documentation**: https://xunit.net/
- **Coverlet Documentation**: https://github.com/coverlet-coverage/coverlet
- **ReportGenerator Documentation**: https://github.com/danielpalme/ReportGenerator
- **Testcontainers Documentation**: https://dotnet.testcontainers.org/
- **NBomber Documentation**: https://nbomber.com/

---

**Status**: ✅ Complete  
**Ready for Implementation**: Yes  
**Next Command**: `/speckit.tasks` (to generate detailed task breakdown)
