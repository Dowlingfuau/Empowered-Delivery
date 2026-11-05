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

    public async Task<List<Signal>> BuildSignals(AssessmentSession session) =>
        (await BuildSignalsWithTrace(session)).Signals;

    public async Task<(List<Signal> Signals, List<SignalTrace> Traces)> BuildSignalsWithTrace(AssessmentSession session)
    {
        var signals = new List<Signal>();
        var traces = new List<SignalTrace>();
        
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

            var normalisation = new CalculationStep
            {
                StepName = "Normalisation",
                Stage = TraceStage.Normalisation
            };

            if (definition != null)
            {
                var (value, formula) = _normaliser.NormaliseTraced(definition, signal.RawValue);
                signal.NormalisedValue = value;
                normalisation.Description = $"Registry rule '{definition.NormalisationRule}'";
                normalisation.Formula = formula;
                normalisation.Value = value;
            }
            else
            {
                signal.NormalisedValue = _normaliser.Clamp(signal.RawValue);
                Console.WriteLine($"[WARN] No registry definition for signal: {signal.Name}");
                normalisation.Description = "No registry definition; the raw value is clamped to 0-1.";
                normalisation.Formula = FormulaTrace.Expr("Clamp(Value, 0, 1)", $"Clamp({TraceNumber.Format(signal.RawValue)}, 0, 1)", signal.NormalisedValue);
                normalisation.Value = signal.NormalisedValue;
            }
            signals.Add(signal);

            traces.Add(new SignalTrace
            {
                SignalName = signal.Name,
                WorkspaceId = signal.WorkspaceId,
                SquadId = signal.SquadId,
                Inputs = new List<TraceInput>
                {
                    new TraceInput { Name = $"{lens.Lens} · {theme.Theme} · behaviour level", Value = theme.Level, Source = "MaturityAssessment" }
                },
                CalculationSteps = new List<CalculationStep>
                {
                    new CalculationStep
                    {
                        StepName = "SignalMapping",
                        Description = $"Mapping for {lens.Lens} / {theme.Theme} / level {theme.Level} selects signal {mapping.SignalName}; the level becomes the raw value",
                        Stage = TraceStage.Output,
                        Value = signal.RawValue
                    },
                    normalisation
                }
            });
            }
        }
        return (signals, traces);
    }
}
}