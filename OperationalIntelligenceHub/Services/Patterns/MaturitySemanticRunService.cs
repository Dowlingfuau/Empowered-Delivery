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
    private readonly MaturityNarrativeComposer _composer;
    private readonly ILogger<MaturitySemanticRunService> _logger;

    public MaturitySemanticRunService(MaturitySemanticRuleRepository repository,
        MaturitySemanticEvaluator evaluator, MaturityNarrativeComposer composer, ILogger<MaturitySemanticRunService> logger)
    {
        _repository = repository;
        _evaluator = evaluator;
        _composer = composer;
        _logger = logger;
    }

    public async Task<MaturitySemanticRun> EvaluateAsync(AssessmentSession session, MaturitySemanticRun? previous = null)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (string.IsNullOrWhiteSpace(session.AssessmentId))
            throw new ArgumentException("Semantic evaluation requires a saved assessment.", nameof(session));

        var fingerprint = string.Empty;
        var stage = "AssessmentSnapshot";
        SemanticEvaluationResult? result = null;
        MaturityEligibilityDecision? decision = null;
        MaturityInterpretationProfile? profile = null;
        try
        {
            var json = JsonSerializer.Serialize(session);
            var snapshot = JsonSerializer.Deserialize<AssessmentSession>(json)
                ?? throw new InvalidOperationException("The assessment snapshot could not be created.");
            fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)));
            stage = "DefinitionLoading";
            var definition = await _repository.LoadAsync("BF01");
            var canReuseEvaluation = previous?.Evaluation is { } prior &&
                previous.AssessmentId == snapshot.AssessmentId &&
                previous.AssessmentFingerprint == fingerprint &&
                prior.Definition.DefinitionFingerprint == definition.ValidatedFingerprint;

            stage = "EvidenceAndRuleEvaluation";
            result = canReuseEvaluation ? previous!.Evaluation! : await _evaluator.EvaluateAsync(snapshot, definition);
            stage = "Eligibility";
            decision = MaturityEligibilityPolicy.Assess(result);
            stage = "Interpretation";
            profile = MaturityInterpretationBuilder.Build(result);
            MaturityDiagnosticFinding? finding = null;
            if (decision.Status == MaturityEligibilityStatus.EligibleProvisional)
            {
                stage = "NarrativeComposition";
                var content = await _composer.LoadAsync(definition);
                if (canReuseEvaluation && previous!.Error is null &&
                    previous.EligibilityDecision?.PolicyVersion == decision.PolicyVersion &&
                    previous.Finding?.NarrativeFingerprint == content.Fingerprint)
                    return previous;
                finding = _composer.Compose(result, profile, content);
            }
            else if (canReuseEvaluation && previous!.Error is null &&
                previous.EligibilityDecision?.PolicyVersion == decision.PolicyVersion)
                return previous;

            return new MaturitySemanticRun(Guid.NewGuid().ToString(), snapshot.AssessmentId, fingerprint,
                definition.PatternId, definition.Source, result, decision.Status.ToString(), decision.Reason, null, null)
            {
                EligibilityDecision = decision,
                Interpretation = profile,
                Finding = finding
            };
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Maturity semantic evaluation failed at {Stage} for assessment {AssessmentId}",
                stage, session.AssessmentId);
            decision ??= MaturityEligibilityPolicy.Assess(result, exception.Message);
            return new MaturitySemanticRun(Guid.NewGuid().ToString(), session.AssessmentId, fingerprint,
                "BF01", MaturitySemanticRuleRepository.DefaultSource, result, decision.Status.ToString(),
                decision.Reason, stage, exception.Message)
            {
                EligibilityDecision = decision,
                Interpretation = profile
            };
        }
    }
}
