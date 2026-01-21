using Serilog;
using Serilog.Core;
using DWSIMAutomation = DWSIM.Automation.Automation;
using DWSIM.GlobalSettings;
using DWSIM.Thermodynamics.Streams;
using DWSIM.Interfaces;
using DWSIM.UnitOperations.Streams;

namespace Enerflow.Tests.DWSIM;

/// <summary>
/// Base class for all DWSIM API tests.
/// Provides automation setup, logging, and common helper methods.
/// </summary>
public abstract class TestBase : IDisposable
{
    protected readonly Logger Logger;
    protected readonly DWSIMAutomation Automation;
    private readonly string _testName;
    private readonly string _logFilePath;

    protected TestBase()
    {
        _testName = GetType().Name;

        // Configure file logging in TestResults directory
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        _logFilePath = Path.Combine("TestResults", $"{_testName}_{timestamp}.log");

        // Ensure TestResults directory exists
        Directory.CreateDirectory("TestResults");

        // Initialize logger with both console and file output
        Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                _logFilePath,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                rollingInterval: RollingInterval.Infinite)
            .CreateLogger();

        Logger.Information("========================================");
        Logger.Information("{TestName} - Starting Test", _testName);
        Logger.Information("Log file: {LogFile}", _logFilePath);
        Logger.Information("========================================");

        // CRITICAL: Set DWSIM to automation mode before any operations
        Settings.AutomationMode = true;
        Logger.Debug("DWSIM GlobalSettings.AutomationMode = true");

        // Initialize DWSIM Automation
        Automation = new DWSIMAutomation();
        Logger.Information("DWSIM Automation initialized");
    }

    /// <summary>
    /// Asserts that the flowsheet solved successfully without errors.
    /// </summary>
    protected void AssertConverged(IFlowsheet flowsheet)
    {
        Assert.NotNull(flowsheet);

        if (flowsheet.Solved == false)
        {
            var errorMsg = !string.IsNullOrEmpty(flowsheet.ErrorMessage)
                ? flowsheet.ErrorMessage
                : "Flowsheet failed to solve (no error message provided)";

            Logger.Error("Flowsheet convergence failed: {Error}", errorMsg);
            Assert.Fail($"Flowsheet did not converge: {errorMsg}");
        }

        // Check for errors in individual objects
        var errors = new List<string>();
        foreach (var obj in flowsheet.SimulationObjects.Values)
        {
            if (!string.IsNullOrEmpty(obj.ErrorMessage))
            {
                errors.Add($"{obj.Name}: {obj.ErrorMessage}");
            }
        }

        if (errors.Any())
        {
            Logger.Error("Simulation objects have errors:");
            foreach (var error in errors)
            {
                Logger.Error("  - {Error}", error);
            }
            Assert.Fail($"Flowsheet solved but {errors.Count} object(s) have errors:\n" + string.Join("\n", errors));
        }

        Logger.Information("✓ Flowsheet converged successfully");
    }

    /// <summary>
    /// Logs comprehensive summary of the entire flowsheet.
    /// </summary>
    protected void LogFlowsheetSummary(IFlowsheet flowsheet)
    {
        Logger.Information("========================================");
        Logger.Information("FLOWSHEET SUMMARY");
        Logger.Information("========================================");
        Logger.Information("Total Simulation Objects: {Count}", flowsheet.SimulationObjects.Count);
        Logger.Information("Solved: {Solved}", flowsheet.Solved);

        foreach (var obj in flowsheet.SimulationObjects.Values)
        {
            if (obj is MaterialStream ms)
            {
                Logger.Information("  [MATERIAL STREAM] {Name}", ms.Name);
                TestHelpers.LogStreamProperties(ms, Logger);
            }
            else if (obj is EnergyStream es)
            {
                Logger.Information("  [ENERGY STREAM] {Name} - Energy Flow: {Energy:F2} kW", es.Name, es.EnergyFlow ?? 0);
            }
            else
            {
                Logger.Information("  [UNIT OP] {Name} ({Type}) - Calculated: {Calculated}",
                    obj.Name,
                    obj.GetType().Name,
                    obj.Calculated);
            }
        }
        Logger.Information("========================================");
    }

    public virtual void Dispose()
    {
        Logger.Information("========================================");
        Logger.Information("{TestName} - Test Complete", _testName);
        Logger.Information("Log saved to: {LogFile}", _logFilePath);
        Logger.Information("========================================");

        Automation.ReleaseResources();
        Logger.Dispose();
    }
}
