namespace OperationalIntelligenceHub.Models;

public sealed class MaturityBehaviourEvidence
{
    public string BehaviourId { get; set; } = string.Empty;
    public string CatalogueVersion { get; set; } = string.Empty;
    public string Headline { get; set; } = string.Empty;
    public List<string> Signals { get; set; } = new();
    public List<string> Examples { get; set; } = new();
}
