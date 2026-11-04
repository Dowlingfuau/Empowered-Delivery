using OperationalIntelligenceHub.Models;

namespace OperationalIntelligenceHub.Services
{
    public class SystemHealth
    {
        public double Position { get; set; }
        public string State { get; set; } = "";
    }

    public class HealthAggregationService
    {
        public SystemHealth Calculate(
            Signal? size,
            Signal? age,
            Signal? volatility,
            Signal? prioritisation,
            Signal? predictability)
        {
            var weightedSignals = new[]
            {
                (Value: size?.ContinuumValue, Weight: 0.10, Available: IsAvailable(size, "sizeDataAvailable")),
                (Value: age?.ContinuumValue, Weight: 0.20, Available: IsAvailable(age, "ageDataAvailable")),
                (Value: volatility?.ContinuumValue, Weight: 0.25, Available: IsAvailable(volatility, "volatilityDataAvailable")),
                (Value: prioritisation?.ContinuumValue, Weight: 0.15, Available: IsAvailable(prioritisation, "prioritisationDataAvailable")),
                (Value: (double?)predictability?.NormalisedValue, Weight: 0.30, Available: HasPredictabilityData(predictability))
            };

            var availableSignals = weightedSignals.Where(item => item.Available).ToList();
            if (availableSignals.Count == 0)
            {
                return new SystemHealth
                {
                    Position = 0.5,
                    State = "Insufficient Data"
                };
            }

            double availableWeight = availableSignals.Sum(item => item.Weight);
            double position = Math.Clamp(
                availableSignals.Sum(item => item.Value!.Value * item.Weight) / availableWeight,
                0,
                1);
            position = Math.Round(position, 12);

            string state =
                position < 0.4 ? "Unhealthy" :
                position < 0.7 ? "Needs Attention" :
                "Healthy";

            return new SystemHealth
            {
                Position = position,
                State = state
            };
        }

        private static bool IsAvailable(Signal? signal, string metadataKey) =>
            signal?.ContinuumValue.HasValue == true &&
            signal?.Metadata?.TryGetValue(metadataKey, out var value) == true &&
            value is true;

        private static bool HasPredictabilityData(Signal? signal) =>
            signal?.Metadata?.TryGetValue("predictabilityDataAvailable", out var availableValue) == true &&
            availableValue is true &&
            signal.Metadata.TryGetValue("historyLength", out var historyValue) &&
            historyValue is int historyLength &&
            historyLength >= 2 &&
            signal.Metadata.TryGetValue("averageThroughput", out var averageValue) &&
            averageValue is double averageThroughput &&
            averageThroughput > 0;
    }
}