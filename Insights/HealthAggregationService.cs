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
            Signal? predictability,
            List<OperationalInsight> insights)
        {
            // ----------------------------
            // 1. BASELINE (continuum-driven)
            // ----------------------------
            double baseline =
                (size?.ContinuumValue ?? 0.5) * 0.2 +
                (age?.ContinuumValue ?? 0.5) * 0.3 +
                (volatility?.ContinuumValue ?? 0.5) * 0.3 +
                (prioritisation?.ContinuumValue ?? 0.5) * 0.2;

            // ----------------------------
            // 2. INSIGHT MODIFIER
            // ----------------------------
            var risk = insights
                .Where(i => i.Category == "Risk")
                .Sum(i => i.Score);

            var positive = insights
                .Where(i => i.Category == "Positive")
                .Sum(i => i.Score);

            var constraint = insights
                .Where(i => i.Category == "Constraint")
                .Sum(i => i.Score);

            double modifier = (risk * 0.6 + constraint * 0.4) - positive;

            double adjusted = baseline + (modifier / 20.0);

            adjusted = Math.Clamp(adjusted, 0, 1);

            // ----------------------------
            // 3. PREDICTABILITY OVERRIDE
            // ----------------------------
            var predictabilityValue = predictability?.NormalisedValue ?? 0;

            if (predictabilityValue > 0.9)
            {
                return new SystemHealth
                {
                    Position = 1.0,
                    State = "Unhealthy"
                };
            }

            // soft influence
            adjusted += predictabilityValue * 0.2;
            adjusted = Math.Clamp(adjusted, 0, 1);

            // ----------------------------
            // 4. STATE MAPPING
            // ----------------------------
            string state =
                adjusted < 0.4 ? "Healthy" :
                adjusted < 0.7 ? "Needs Attention" :
                "Unhealthy";

            return new SystemHealth
            {
                Position = adjusted,
                State = state
            };
        }
    }
}