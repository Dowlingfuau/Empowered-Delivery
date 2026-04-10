using System.Collections.Generic;
using System.Linq;

namespace OperationalIntelligenceHub.Models
{
    public class MaturityLensAssessment
    {
        public string Lens { get; set; } = string.Empty;
        public List<MaturityTheme> Themes { get; set; } = new List<MaturityTheme>();

        public void InitializeThemes()
        {
            var themeNames = new[] { "Ownership", "Trust", "Value", "Learning", "System" };
            foreach (var t in themeNames)
            {
                if (!Themes.Any(theme => theme.Theme == t))
                {
                    Themes.Add(new MaturityTheme { Theme = t, Level = 0 });
                }
            }
        }
    public int GetThemeScore(string theme)
    {
        return Themes.FirstOrDefault(t => t.Theme == theme)?.Level ?? 0;
    }
    public int AverageScore =>
        Themes.Count == 0 ? 0 :
        (int)Math.Round(Themes.Average(t => t.Level));
        public bool IsComplete => Themes.All(t => t.Level > 0);
    }
}