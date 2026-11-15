using OperationalIntelligenceHub.Models;

namespace OperationalIntelligenceHub.Services;

public static class TeamHealthTraceAdapter
{
    public static (List<TraceInput> Inputs, List<CalculationTrace> Calculations) Build(
        IReadOnlyList<(string Name, int Score)> scores, int overallScore)
    {
        var inputs = scores.Select(score => new TraceInput
        {
            Name = score.Name,
            Value = score.Score == 0 ? "Not answered (0 in current calculation)" : score.Score,
            Source = "TeamHealth",
            Kind = TraceInputKind.UserInput
        }).ToList();

        var calculations = new List<CalculationTrace>
        {
            new()
            {
                CalculationName = "Overall Team Health score",
                Inputs = inputs,
                Steps = new()
                {
                    new CalculationStep
                    {
                        StepName = "Average of eight dimension scores",
                        Description = "The existing calculation includes unanswered dimensions as 0 and uses integer division.",
                        Stage = TraceStage.Output,
                        Value = overallScore,
                        Formula = FormulaTrace.Expr(
                            "(Sum of dimension scores) / 8 (integer division)",
                            $"({string.Join(" + ", scores.Select(score => score.Score))}) / 8",
                            overallScore)
                    }
                }
            }
        };
        return (inputs, calculations);
    }
}
