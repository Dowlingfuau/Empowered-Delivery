namespace OperationalIntelligenceHub.Models
{
    // Why an insight was placed where it was. Keeps the interim Overall fallback explicit and inspectable.
    public enum InsightPlacementReason
    {
        Anchored,
        SystemLevel,
        Compound,
        UnmappedSignal,
        NoContributingSignals
    }

    // How one signal identifier on an insight resolved. SourceId is preserved exactly as the engine emitted it.
    public record InsightSignalReference(string SourceId, string? CanonicalId, string? AnchorId);

    // Presentation context for an insight. The insight itself is never modified.
    public class InsightPlacement
    {
        public OperationalInsight Insight { get; init; } = new();

        // Null means the insight belongs to the overall area of the diagnostic.
        public string? AnchorId { get; init; }

        public InsightPlacementReason Reason { get; init; }

        // Authored in the rule configuration. Not part of the engine output contract.
        public string? Category { get; init; }

        public IReadOnlyList<InsightSignalReference> Signals { get; init; } = Array.Empty<InsightSignalReference>();
    }

    // Configuration: one diagnostic area and the signal identifiers that belong to it.
    public class InsightAnchorDefinition
    {
        public string Id { get; set; } = "";
        public string Label { get; set; } = "";
        public string CanonicalSignal { get; set; } = "";
        public List<string> Aliases { get; set; } = new();
    }

    public class DiagnosticAnchorSet
    {
        public List<InsightAnchorDefinition> Anchors { get; set; } = new();
    }

    public class InsightAnchorConfig
    {
        public Dictionary<string, DiagnosticAnchorSet> Diagnostics { get; set; } = new();
    }
}
