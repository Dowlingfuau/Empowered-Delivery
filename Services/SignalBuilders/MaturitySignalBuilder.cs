using OperationalIntelligenceHub.Models;
namespace OperationalIntelligenceHub.Services
{
public class MaturitySignalBuilderService : SignalBuilderBaseService
{
    private readonly SignalMappingLoaderService _mappingLoader;
    private readonly SignalRegistryService _registry;
    public MaturitySignalBuilderService(SignalNormaliserService normaliser, 
    SignalMappingLoaderService mappingLoader,
    SignalRegistryService registry)
        : base(normaliser)
        {
            _mappingLoader = mappingLoader;
            _registry = registry;
        }

    public async Task <List<Signal>> BuildSignals(AssessmentSession session)
    {
        var signals = new List<Signal>();
        
        var mappings = await _mappingLoader.GetMappings("maturity");

        foreach (var lens in session.LensAssessments)
        {
            foreach (var theme in lens.Themes)
            {
                if (theme.Level <= 0)
                    continue;

                var mapping = mappings.FirstOrDefault(m =>
                    string.Equals(m.Lens, lens.Lens, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(m.Theme, theme.Theme, StringComparison.OrdinalIgnoreCase) &&
                    m.Level == theme.Level);
                    
                if (mapping == null)
                    {
                        Console.WriteLine($"[WARN] No mapping for {lens.Lens}-{theme.Theme}-{theme.Level}");
                        continue;
                    }

                var signal = CreateSignal(
                    mapping.SignalName,
                    mapping.Domain,
                    theme.Level,
                    Guid.Parse(session.WorkspaceId),
                    Guid.Parse(session.SquadId),
                    "MaturityAssessment"
                );

            var definition = _registry.Get(signal.Name);

            if (definition != null)
            {
                signal.NormalisedValue = _normaliser.Normalise(definition, signal.RawValue);
            }
            else
            {
                signal.NormalisedValue = _normaliser.Clamp(signal.RawValue);
                Console.WriteLine($"[WARN] No registry definition for signal: {signal.Name}");
            }
            signals.Add(signal);
            }
        }
        return signals;
    }
}
}