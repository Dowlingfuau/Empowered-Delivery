namespace OperationalIntelligenceHub.Models
{
    public class SignalContext
    {
        // Existing signals (your current pipeline output)
        public Dictionary<string, Signal> Signals { get; set; } = new();

        // RAW inputs (from diagnostics)
        public Dictionary<string, double> Raw { get; set; } = new();

        // Derived system behaviour (cross-signal reasoning inputs)
        public Dictionary<string, double> Derived { get; set; } = new();

        // Shape, pattern, distribution, etc
        public Dictionary<string, object> Metadata { get; set; } = new();
    }
}