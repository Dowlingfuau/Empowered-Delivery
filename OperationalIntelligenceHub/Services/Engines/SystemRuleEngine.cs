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
                    var clauses = new List<ConditionClause>();
                    var matched = EvaluateCondition(condition.Condition, inputs, clauses);

                    evaluations.Add(new RuleEvaluation
                    {
                        RuleName = rule.Name,
                        Condition = condition.Condition,
                        Result = matched,
                        Output = condition.Result,
                        Layer = "System",
                        Clauses = clauses
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

        private bool EvaluateCondition(string condition, List<DerivedSignal> signals, List<ConditionClause>? clauses = null)
        {
            Console.WriteLine($"Evaluating: {condition}");
            var parts = condition.Split("AND", StringSplitOptions.TrimEntries);

            foreach (var part in parts)
            {
                var tokens = part.Split("=", StringSplitOptions.TrimEntries);
                if (tokens.Length != 2)
                {
                    clauses?.Add(new ConditionClause
                    {
                        Subject = part,
                        Operator = "=",
                        Threshold = "",
                        Actual = "n/a",
                        Matched = false,
                        Note = "Clause is not in the form Signal = Intensity."
                    });
                    return false;
                }

                var signalName = tokens[0];
                var expectedIntensity = tokens[1];

                var signal = signals.FirstOrDefault(s => s.Name == signalName);

                bool clauseMatched = signal != null && signal.Intensity == expectedIntensity;
                clauses?.Add(new ConditionClause
                {
                    Subject = signalName,
                    Operator = "=",
                    Threshold = expectedIntensity,
                    Actual = signal == null ? "not triggered" : signal.Intensity,
                    Matched = clauseMatched,
                    Note = signal == null ? "This behaviour signal did not trigger." : null
                });

                if (!clauseMatched)
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