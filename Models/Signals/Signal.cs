namespace OperationalIntelligenceHub.Models
{
    // RAW SYSTEM STATE VARIABLE
    public class Signal
    {
        public string Name { get; set; }
        public string Domain { get; set; }
        public double RawValue { get; set; }
        public double NormalisedValue { get; set; }
        public Guid WorkspaceId { get; set; }
        public Guid SquadId { get; set; }
        public string SourceDiagnostic { get; set; }
        public DateTime Timestamp { get; set; }
        public string Category { get; set; } // Metric, Behaviour, Observation, Derived
        public double Confidence { get; set; } = 1.0;
        public int Direction { get; set; } = 0; // -1, 0, +1
        public SignalType Type { get; set; } = SignalType.Normalised;
        public string? Shape { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
        public enum SignalType
        {
            Raw,
            Derived,
            Normalised
        }
        // UX only
        public double? ContinuumValue { get; set; } // default center
    }
}