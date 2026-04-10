// TEMP: Keep this as - Experimental engine, Advanced logic engine (future), Comparison baseline
using OperationalIntelligenceHub.Models;

namespace OperationalIntelligenceHub.Services
{
    public class BehaviourRuleEngine
    {
        public List<DerivedSignal> Evaluate(List<Signal> signals)
        {
            var derived = new List<DerivedSignal>();

            // Example Rule 1: Weak Ownership
            var ownershipSignals = signals
                .Where(s => s.Name.Contains("Ownership"))
                .ToList();

            if (ownershipSignals.Any())
            {
                var avg = ownershipSignals.Average(s => s.NormalisedValue);

                if (avg < 0.4)
                {
                    derived.Add(new DerivedSignal
                    {
                        Name = "WeakOwnership",
                        Domain = "Capability",
                        Score = avg,
                        Source = "BehaviourRuleEngine",
                        WorkspaceId = ownershipSignals.First().WorkspaceId,
                        SquadId = ownershipSignals.First().SquadId,
                        ContributingSignals = ownershipSignals.Select(s => s.Name).ToList()
                    });
                }
            }

            // Example Rule 2: Strong Ownership
            if (ownershipSignals.Any())
            {
                var avg = ownershipSignals.Average(s => s.NormalisedValue);

                if (avg >= 0.7)
                {
                    derived.Add(new DerivedSignal
                    {
                        Name = "StrongOwnership",
                        Domain = "Capability",
                        Score = avg,
                        Source = "BehaviourRuleEngine",
                        WorkspaceId = ownershipSignals.First().WorkspaceId,
                        SquadId = ownershipSignals.First().SquadId,
                        ContributingSignals = ownershipSignals.Select(s => s.Name).ToList()
                    });
                }
            }

            return derived;
        }
    }
}