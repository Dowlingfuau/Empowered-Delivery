using OperationalIntelligenceHub.Models;

namespace OperationalIntelligenceHub.Services;

public record MaturityDraftUpdate(string Lens, string Theme, int Level, string Note);

public static class MaturityDraftTransfer
{
    public static IReadOnlyList<MaturityDraftUpdate> Plan(AssessmentSession draft, AssessmentSession target)
    {
        var updates = new List<MaturityDraftUpdate>();
        foreach (var lens in draft.LensAssessments)
        {
            foreach (var theme in lens.Themes.Where(theme => theme.Level > 0))
            {
                var targetLens = target.LensAssessments.SingleOrDefault(item =>
                    string.Equals(item.Lens, lens.Lens, StringComparison.OrdinalIgnoreCase));
                var targetTheme = targetLens?.Themes.SingleOrDefault(item =>
                    string.Equals(item.Theme, theme.Theme, StringComparison.OrdinalIgnoreCase));
                if (targetLens is null || targetTheme is null || theme.Level > 5)
                {
                    throw new InvalidOperationException($"The session cannot accept the draft selection for {lens.Lens} / {theme.Theme}.");
                }

                updates.Add(new MaturityDraftUpdate(targetLens.Lens, targetTheme.Theme, theme.Level, theme.BehaviouralNote));
            }
        }
        return updates;
    }
}
