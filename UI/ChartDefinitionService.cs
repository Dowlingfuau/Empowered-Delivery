using OperationalIntelligenceHub.Models;

namespace OperationalIntelligenceHub.Services
{
    // This is just a DATA MODEL
    public class ChartDefinition
    {
        public string Title { get; set; } = string.Empty;
        public string CanvasId { get; set; } = string.Empty;
        public string[] Labels { get; set; } = Array.Empty<string>();
        public object[]? Datasets { get; set; }
    }

    // This is the SERVICE
    public class ChartDefinitionService
    {
        public List<ChartDefinition> BuildBacklogLensCharts(
            IEnumerable<AssessmentResult> data,
            Func<string, string> colorSelector)
        {
            var list = new List<ChartDefinition>();

            var lenses = new (string Title, Func<AssessmentResult, int?> Selector)[]
            {
                ("Backlog Size", r => r.BacklogSize),
                ("Backlog Age", r => r.BacklogAge),
                ("Backlog Volatility", r => r.BacklogVolatility),
                ("Prioritisation", r => r.BacklogPriority),
                ("Velocity Predictability", r => r.BacklogPredictability)
            };

            foreach (var lens in lenses)
            {
                var grouped = data
                    .GroupBy(r => r.SquadName)
                    .OrderBy(g => g.Key);

                var labels = data
                    .OrderBy(r => r.Date)
                    .Select(r => r.Date.ToString("dd MMM"))
                    .Distinct()
                    .ToArray();

                var datasets = grouped.Select(g =>
                {
                    var ordered = g.OrderBy(r => r.Date);

                    return new
                    {
                        label = g.Key,
                        data = ordered.Select(r => lens.Selector(r) ?? 0).ToArray(),
                        borderColor = colorSelector(g.Key)
                    };
                }).ToArray();

                list.Add(new ChartDefinition
                {
                    CanvasId = $"backlog-{lens.Title.Replace(" ", "")}",
                    Title = lens.Title,
                    Labels = labels,
                    Datasets = datasets
                });
            }

            return list;
        }
        public List<ChartDefinition> BuildTeamHealthLensCharts(
            IEnumerable<AssessmentResult> data,
            Func<string, string> colorSelector)
        {
            var list = new List<ChartDefinition>();

            var lenses = new (string Title, Func<AssessmentResult, int?> Selector)[]
            {
                ("Clarity of Purpose", r => r.ClarityScore),
                ("Psychological Safety", r => r.SafetyScore),
                ("Flow of Work", r => r.FlowScore),
                ("Collaboration", r => r.CollaborationScore),
                ("Focus & Discipline", r => r.FocusScore),
                ("Stakeholder Engagement", r => r.StakeholderScore),
                ("Continuous Improvement", r => r.ImprovementScore),
                ("Energy & Morale", r => r.MoraleScore)
            };

            foreach (var lens in lenses)
            {
                var grouped = data
                    .GroupBy(r => r.SquadName)
                    .OrderBy(g => g.Key);

                var labels = data
                    .OrderBy(r => r.Date)
                    .Select(r => r.Date.ToString("dd MMM"))
                    .Distinct()
                    .ToArray();

                var datasets = grouped.Select(g =>
                {
                    var ordered = g.OrderBy(r => r.Date);

                    return new
                    {
                        label = g.Key,
                        data = ordered.Select(r => lens.Selector(r) ?? 0).ToArray(),
                        borderColor = colorSelector(g.Key)
                    };
                }).ToArray();

                list.Add(new ChartDefinition
                {
                    CanvasId = $"team-{lens.Title.Replace(" ", "")}",
                    Title = lens.Title,
                    Labels = labels,
                    Datasets = datasets
                });
            }

            return list;
        }
        public ChartDefinition BuildMaturityRadarChart(AssessmentSession session)
        {
            var labels = MaturityTheme.Themes;

            var datasets = session.LensAssessments
                .OrderBy(l => l.Lens)
                .Select(lens => new
                {
                    label = lens.Lens,
                    data = labels.Select(theme =>
                        lens.GetThemeScore(theme)).ToArray()
                })
                .ToArray();

            return new ChartDefinition
            {
                CanvasId = "maturity-radar",
                Title = "Maturity Profile",
                Labels = labels,
                Datasets = datasets
            };
        }
    }
}