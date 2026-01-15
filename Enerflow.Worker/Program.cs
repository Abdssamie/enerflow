using System.Text.Json;
using Enerflow.Domain.DTOs;
using Enerflow.Worker;

class Program
{
    static void Main(string[] args)
    {
        string? jobFile = null;
        string? outputFile = null;

        // Simple arg parsing (Enterprise would use System.CommandLine)
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--job" && i + 1 < args.Length) jobFile = args[i + 1];
            if (args[i] == "--output" && i + 1 < args.Length) outputFile = args[i + 1];
        }

        if (string.IsNullOrEmpty(jobFile) || string.IsNullOrEmpty(outputFile))
        {
            Console.WriteLine("Usage: Enerflow.Worker --job <job.json> --output <result.json>");
            Environment.Exit(1);
        }

        try
        {
            // 1. Read Job Config
            if (!File.Exists(jobFile)) throw new FileNotFoundException($"Job file not found: {jobFile}");
            
            var jobJson = File.ReadAllText(jobFile);
            var job = JsonSerializer.Deserialize<SimulationJob>(jobJson);

            if (job == null) throw new Exception("Failed to deserialize job configuration.");

            // 2. Initialize Automation Service with the Template File
            using var service = new AutomationService(job.SimulationFilePath);
            
            // 3. Run the Job (Apply Diffs -> Solve -> Collect)
            var result = service.RunJob(job);
            
            // 4. Serialize Result to JSON
            var resultJson = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
            
            // 5. Write to output file
            File.WriteAllText(outputFile, resultJson);
            
            Console.WriteLine($"Result written to {outputFile}");
            
            // Exit code based on success
            Environment.Exit(result.Success ? 0 : 1);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Critical Worker Error: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
            
            // Attempt to write a failure result file if possible
            try 
            {
                var failureResult = new SimulationResult 
                { 
                    Success = false, 
                    StatusMessage = $"Critical Crash: {ex.Message}",
                    ValidationErrors = new List<string> { ex.ToString() }
                };
                File.WriteAllText(outputFile!, JsonSerializer.Serialize(failureResult));
            }
            catch { /* Best effort */ }

            Environment.Exit(1);
        }
    }   
}
