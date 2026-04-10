using OperationalIntelligenceHub.Models;
namespace OperationalIntelligenceHub.Services
{
    public class OperationalInsightEngine
    {
        private readonly RuleRepositoryService _ruleRepo;
        public OperationalInsightEngine(RuleRepositoryService ruleRepo)
        {
            _ruleRepo = ruleRepo;
        }
        public List<OperationalInsight> GenerateInsights(
            List<DerivedSignal> behaviourSignals,
            List<DerivedSignal> systemSignals)
        {
            var insights = new List<OperationalInsight>();

            var allSignals = behaviourSignals.Concat(systemSignals).ToList();

            foreach (var signal in allSignals)
            {
                var rule = FindRule(signal.Name);

                insights.Add(new OperationalInsight
                {
                    Name = signal.Name,
                    Intensity = signal.Intensity,
                    Interpretation = rule?.Interpretation ?? "",
                    ContributingSignals = signal.ContributingSignals ?? new List<string>(),
                    Source = signal.Source,
                    Score = CalculateScore(signal.Name, signal.Intensity),
                    Reasons = signal.Reasons ?? new List<InsightReason>()
                });
            }

            return insights.OrderByDescending(i => i.Score).Take(5).ToList();
            
        }
        private DerivedSignalRule? FindRule(string name)
        {
            var behaviourRule = _ruleRepo.BehaviourRules
                .FirstOrDefault(r => r.Name == name);

            if (behaviourRule != null)
                return behaviourRule;
            
            var flowRule = _ruleRepo.FlowRules?
                .FirstOrDefault(r => r.Name == name);

            if (flowRule != null)
                return flowRule;

            return _ruleRepo.SystemRules
                .FirstOrDefault(r => r.Name == name);
        }
        private double CalculateScore(string name, string intensity)
        {
            var rule = FindRule(name);

            if (rule == null)
                return 0;

            // Map intensity → numeric
            var intensityScore = intensity switch
            {
                "High" => 1.0,
                "Moderate" => 0.6,
                "Low" => 0.2,
                _ => 0
            };

            var priority = GetPriority(name);
            var confidence = GetConfidence(name);

            return intensityScore * priority * confidence;
        }
        private double GetPriority(string name)
        {
            var rule = FindRule(name);
            return rule?.Priority ?? 0;
        }

        private double GetConfidence(string name)
        {
            var rule = FindRule(name);
            return rule?.Confidence_Weight ?? 0;
        }
    }
}