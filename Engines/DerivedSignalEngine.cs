using System.IO.Pipelines;
using System.Text.Json;
using OperationalIntelligenceHub.Models;
namespace OperationalIntelligenceHub.Services
{
    public class DerivedSignalEngine
    {
        private readonly RuleRepositoryService _ruleRepo;
        public DerivedSignalEngine(RuleRepositoryService ruleRepo)
        {
            _ruleRepo = ruleRepo;
        }
        private double MapIntensityToScore(string intensity)
        {
            return intensity switch
            {
                "High" => 1.0,
                "Moderate" => 0.6,
                "Low" => 0.2,
                _ => 0.5
            };
        }
        public async Task<(List<DerivedSignal>, List<RuleEvaluation>)> EvaluateSignals(List<Signal> signals)
        {
            Console.WriteLine($">>> Engine RuleRepo instance: {_ruleRepo.GetHashCode()}");
            Console.WriteLine($">>> Behaviour rules: {_ruleRepo.BehaviourRules?.Count}");
            Console.WriteLine($">>> Flow rules: {_ruleRepo.FlowRules?.Count}");

            var derivedSignals = new Dictionary<string, DerivedSignal>();
            var evaluations = new List<RuleEvaluation>();

            var allRules = new List<DerivedSignalRule>();

            if (_ruleRepo.BehaviourRules != null)
                allRules.AddRange(_ruleRepo.BehaviourRules);

            if (_ruleRepo.FlowRules != null)
                allRules.AddRange(_ruleRepo.FlowRules);

            Console.WriteLine($">>> TOTAL RULE COUNT: {allRules.Count}");

            // 1. JSON RULE ENGINE (Primary)
            foreach (var rule in allRules)
            {
                string? matchedResult = null;
                string? matchedCondition = null;

                var source = rule.Domain == "Flow"
                    ? "FlowRuleEngine"
                    : "BehaviourRuleEngine";
                

                foreach (var ruleItem in rule.Rules)
                {
                    var evalResult = EvaluateExpression(ruleItem.Condition, signals);

                    Console.WriteLine($">>> Rule: {rule.Name} | Condition: {ruleItem.Condition} | ReadResult: {evalResult}");

                    evaluations.Add(new RuleEvaluation
                    {
                        RuleName = rule.Name,
                        Condition = ruleItem.Condition,
                        Result = evalResult,
                        Output = ruleItem.Result
                    });

                    if (evalResult)
                    {
                        matchedResult = ruleItem.Result;
                        matchedCondition = ruleItem.Condition;
                        break;
                    }
                }

                if (matchedResult != null)
                {
                    var score = MapIntensityToScore(matchedResult);
                    // Build reasoning
                    var contributingSignals = ExtractSignalsFromCondition(matchedCondition!);
                    var reasons = new List<InsightReason>();
                    foreach (var signalName in contributingSignals)
                    {
                        var signal = signals.FirstOrDefault(s => s.Name == signalName);

                        if (signal != null)
                        {
                            reasons.Add(new InsightReason
                            {
                                SignalName = signal.Name,
                                SignalValue = signal.NormalisedValue,
                                Condition = matchedCondition!,
                                Result = matchedResult!,
                                ContributionScore = score * rule.Priority * rule.Confidence_Weight
                            });
                        }
                    }
                    if (derivedSignals.ContainsKey(rule.Name))
                    {
                        // Keep highest severity
                        if (score > derivedSignals[rule.Name].Score)
                            {
                                derivedSignals[rule.Name].Score = score;
                                derivedSignals[rule.Name].Intensity = matchedResult;
                                derivedSignals[rule.Name].Reasons = reasons;
                            }
                    }
                    else
                    {
                        derivedSignals[rule.Name] = new DerivedSignal
                        {
                            Name = rule.Name,
                            Domain = rule.Domain,
                            Score = score,
                            Intensity = matchedResult,
                            Source = source,
                            WorkspaceId = signals.First().WorkspaceId,
                            SquadId = signals.First().SquadId,
                            ContributingSignals = contributingSignals,
                            Reasons = reasons
                        };
                    }
                }
            }   
            return (derivedSignals.Values.ToList(), evaluations);
        }
            

        private List<string> ExtractSignalsFromCondition(string condition)
        {
            var parts = condition.Split("AND", StringSplitOptions.TrimEntries);

            return parts
                .Select(p => p.Split(' ')[0])
                .Distinct()
                .ToList();
        }
        private bool EvaluateExpression(string expression, List<Signal> signals)
        {
            var orGroups = expression.Split("OR", StringSplitOptions.TrimEntries);
            foreach (var group in orGroups)
                {
                    var conditions = group.Split("AND", StringSplitOptions.TrimEntries);

                    bool allTrue = true;

                    foreach (var cond in conditions)
                    {
                        var parts = cond.Trim().Split(' ');

                        var signalName = parts[0];
                        var op = parts[1];
                        double value;
                        try
                            {
                                value = double.Parse(parts[2]);
                            }
                            catch
                            {
                                Console.WriteLine($">>> INVALID RULE: {expression}");
                                return false;
                            }
                        
                        var signal = signals.FirstOrDefault(s => s.Name == signalName);

                        if (signal == null)
                        {
                            allTrue = false;
                            break;
                        }
                        bool result = op switch
                        {
                            "<" => signal.NormalisedValue < value,
                            ">" => signal.NormalisedValue > value,
                            "<=" => signal.NormalisedValue <= value,
                            ">=" => signal.NormalisedValue >= value,
                            _ => false
                        };

                        if (!result)
                        {
                            allTrue = false;
                            break;
                        }
                    }

                    if (allTrue)
                        return true;
            }

            return false;
        }
    }
}