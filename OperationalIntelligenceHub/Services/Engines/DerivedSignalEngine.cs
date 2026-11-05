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
                string? matchedInterpretation = null;

                var source = rule.Domain == "Flow"
                    ? "FlowRuleEngine"
                    : "BehaviourRuleEngine";
                

                foreach (var ruleItem in rule.Rules)
                {
                    var clauses = new List<ConditionClause>();
                    var evalResult = EvaluateExpression(ruleItem.Condition, signals, clauses);

                    Console.WriteLine($">>> Rule: {rule.Name} | Condition: {ruleItem.Condition} | ReadResult: {evalResult}");

                    evaluations.Add(new RuleEvaluation
                    {
                        RuleName = rule.Name,
                        Condition = ruleItem.Condition,
                        Result = evalResult,
                        Output = ruleItem.Result,
                        Layer = rule.Domain == "Flow" ? "Flow" : "Behaviour",
                        Clauses = clauses
                    });

                    if (evalResult)
                    {
                        matchedResult = ruleItem.Result;
                        matchedCondition = ruleItem.Condition;
                        matchedInterpretation = ruleItem.Interpretation;
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
                                derivedSignals[rule.Name].Interpretation = matchedInterpretation ?? "";
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
                            Interpretation = matchedInterpretation ?? "",
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
        private bool EvaluateExpression(string expression, List<Signal> signals, List<ConditionClause>? clauses = null)
        {
            var orGroups = expression.Split("OR", StringSplitOptions.TrimEntries);
            for (int groupIndex = 0; groupIndex < orGroups.Length; groupIndex++)
                {
                    var group = orGroups[groupIndex];
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
                                clauses?.Add(new ConditionClause
                                {
                                    Subject = signalName,
                                    Operator = op,
                                    Threshold = parts[2],
                                    Actual = "n/a",
                                    Matched = false,
                                    GroupIndex = groupIndex,
                                    Note = "Threshold is not a number; the rule is invalid."
                                });
                                return false;
                            }
                        
                        var signal = signals.FirstOrDefault(s => s.Name == signalName);

                        if (signal == null)
                        {
                            clauses?.Add(new ConditionClause
                            {
                                Subject = signalName,
                                Operator = op,
                                Threshold = TraceNumber.Format(value),
                                Actual = "missing",
                                Matched = false,
                                GroupIndex = groupIndex,
                                Note = "Signal was not produced, so the clause cannot match."
                            });
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

                        clauses?.Add(new ConditionClause
                        {
                            Subject = signalName,
                            Operator = op,
                            Threshold = TraceNumber.Format(value),
                            Actual = TraceNumber.Format(signal.NormalisedValue),
                            Matched = result,
                            GroupIndex = groupIndex
                        });

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