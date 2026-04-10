using OperationalIntelligenceHub.Models;

namespace OperationalIntelligenceHub.Services
{
    public class SystemRuleEngine
    {
        private readonly RuleRepositoryService _ruleRepo;

        public SystemRuleEngine(RuleRepositoryService ruleRepo)
        {
            _ruleRepo = ruleRepo;
        }

        public (List<DerivedSignal>, List<RuleEvaluation>) Evaluate(List<DerivedSignal> inputs)
        {
            var results = new List<DerivedSignal>();
            var evaluations = new List<RuleEvaluation>();

            foreach (var rule in _ruleRepo.SystemRules)
            {
                foreach (var condition in rule.Rules)
                {
                    var matched = EvaluateCondition(condition.Condition, inputs);

                    evaluations.Add(new RuleEvaluation
                    {
                        RuleName = rule.Name,
                        Condition = condition.Condition,
                        Result = matched,
                        Output = condition.Result
                    });

                    if (matched)
                    {
                        // Prevent Duplicates
                        if (!results.Any(r => r.Name == rule.Name))
                        {
                            results.Add(new DerivedSignal
                        {
                            Name = rule.Name,
                            Domain = rule.Domain,
                            Score = MapIntensity(condition.Result),
                            Intensity = condition.Result,
                            Source = "SystemRuleEngine",
                            ContributingSignals = rule.SourceSignals ?? new List<string>()
                        });
                        }

                        break;
                    }
                }
            }

            return (results, evaluations);
        }

        private bool EvaluateCondition(string condition, List<DerivedSignal> signals)
        {
            Console.WriteLine($"Evaluating: {condition}");
            var parts = condition.Split("AND", StringSplitOptions.TrimEntries);

            foreach (var part in parts)
            {
                var tokens = part.Split("=", StringSplitOptions.TrimEntries);
                if (tokens.Length != 2)
                    return false;

                var signalName = tokens[0];
                var expectedIntensity = tokens[1];

                var signal = signals.FirstOrDefault(s => s.Name == signalName);

                if (signal == null || signal.Intensity != expectedIntensity)
                    return false;
            }

            return true;
        }

        private double MapIntensity(string result)
        {
            return result switch
            {
                "High" => 1.0,
                "Moderate" => 0.6,
                "Low" => 0.3,
                _ => 0
            };
        }
    }
}