namespace Enerflow.Domain.Entities;

public class MaterialStream
{
    public required Guid Id { get; set; }
    public required Guid SimulationId { get; set; }
    public required string Name { get; set; }
    
    // State Properties (SI Units)
    public double Temperature { get; set; }
    public double Pressure { get; set; }
    public double MassFlow { get; set; }
    
    public string? Phase { get; set; }
    
    // Compound fractions
    public Dictionary<string, double> MolarCompositions { get; set; } = new();
}
