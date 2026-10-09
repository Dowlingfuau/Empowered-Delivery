using OperationalIntelligenceHub.Models;

namespace OperationalIntelligenceHub.Components.Domain
{
    /// <summary>
    /// Presentation model for one Backlog Health signal. Every value is supplied by the
    /// existing page calculations and signal traces; nothing is derived here.
    /// </summary>
    public sealed class BacklogHealthSignalReading
    {
        public string Id { get; init; } = string.Empty;
        public string Title { get; init; } = string.Empty;
        public string Explanation { get; init; } = string.Empty;
        public bool Available { get; init; }
        public double Value { get; init; } = 0.5;
        public string Label { get; init; } = string.Empty;
        public int Direction { get; init; }
        public List<string> Zones { get; init; } = new();
        public string EvidenceTitle { get; init; } = string.Empty;
        public List<KeyValuePair<string, string>> Evidence { get; init; } = new();
        public string Interpretation { get; init; } = string.Empty;
        public SignalTrace? Trace { get; init; }
    }
}
