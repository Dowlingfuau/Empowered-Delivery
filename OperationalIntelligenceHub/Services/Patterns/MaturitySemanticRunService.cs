using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using OperationalIntelligenceHub.Models;
using OperationalIntelligenceHub.Models.Patterns;

namespace OperationalIntelligenceHub.Services.Patterns;

public sealed class MaturitySemanticRunService
{
    private readonly MaturitySemanticRuleRepository _repository;
    private readonly MaturitySemanticEvaluator _evaluator;
    private readonly ILogger<MaturitySemanticRunService> _logger;

    public MaturitySemanticRunService(MaturitySemanticRuleRepository repository,
        MaturitySemanticEvaluator evaluator, ILogger<MaturitySemanticRunService> logger)
    {
        _repository = repository;
        _evaluator = evaluator;
        _logger = logger;
    }

    public async Task<MaturitySemanticRun> EvaluateAsync(AssessmentSession session, MaturitySemanticRun? previous = null)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (string.IsNullOrWhiteSpace(session.AssessmentId))
            throw new ArgumentException("Semantic evaluation requires a saved assessment.", nameof(session));

        var fingerprint = string.Empty;
        var stage = "AssessmentSnapshot";
        try
        {
            var json = JsonSerializer.Serialize(session);
            var snapshot = JsonSerializer.Deserialize<AssessmentSession>(json)
                ?? throw new InvalidOperationException("The assessment snapshot could not be created.");
            fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)));
            stage = "DefinitionLoading";
            var definition = await _repository.LoadAsync("BF01");
            if (previous?.Evaluation is { } prior &&
                previous.AssessmentId == snapshot.AssessmentId &&
                previous.AssessmentFingerprint == fingerprint &&
                prior.Definition.DefinitionFingerprint == definition.ValidatedFingerprint)
                return previous;

            stage = "EvidenceAndRuleEvaluation";
            var result = await _evaluator.EvaluateAsync(snapshot, definition);
            var candidate = result.Outcome == SemanticOutcome.Candidate;
            return new MaturitySemanticRun(Guid.NewGuid().ToString(), snapshot.AssessmentId, fingerprint,
                definition.PatternId, definition.Source, result,
                candidate ? "NotAssessed" : "NotApplicable",
                candidate ? "Eligibility has not been assessed; no diagnostic finding has been created."
                    : "No candidate was created, so eligibility was not assessed.",
                null, null);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Maturity semantic evaluation failed at {Stage} for assessment {AssessmentId}",
                stage, session.AssessmentId);
            return new MaturitySemanticRun(Guid.NewGuid().ToString(), session.AssessmentId, fingerprint,
                "BF01", MaturitySemanticRuleRepository.DefaultSource, null, "NotAssessed",
                "Eligibility could not be assessed because semantic evaluation failed.", stage, exception.Message);
        }
    }
}
