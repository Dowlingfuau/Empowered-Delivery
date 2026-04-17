using OperationalIntelligenceHub.Models;
namespace OperationalIntelligenceHub.Services
{
public abstract class SignalBuilderBaseService
{
    protected readonly SignalNormaliserService _normaliser;

    protected SignalBuilderBaseService(SignalNormaliserService normaliser)
    {
        _normaliser = normaliser;
    }

    protected Signal CreateSignal(
        string name,
        string domain,
        double rawValue,
        Guid workspaceId,
        Guid squadId,
        string source)
    {
        return new Signal
        {
            Name = name,
            Domain = domain,
            RawValue = rawValue,
            NormalisedValue = rawValue, // TEMP,
            WorkspaceId = workspaceId,
            SquadId = squadId,
            SourceDiagnostic = source,
            Timestamp = DateTime.UtcNow
        };
    }
}
}