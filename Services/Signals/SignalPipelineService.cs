using OperationalIntelligenceHub.Models;

namespace OperationalIntelligenceHub.Services
{
    public class SignalPipelineService
    {
        private readonly MaturitySignalBuilderService _maturityBuilder;
        private readonly DerivedSignalEngine _derivedSignalEngine;

        private readonly SystemRuleEngine _systemEngine;

        private readonly OperationalInsightEngine _insightEngine;

        private readonly HealthAggregationService _healthservice;

        public SignalPipelineService(
            MaturitySignalBuilderService maturityBuilder,
            DerivedSignalEngine derivedSignalEngine,
            SystemRuleEngine systemEngine,
            OperationalInsightEngine insightEngine,
            HealthAggregationService healthservice)
        {
            _maturityBuilder = maturityBuilder;
            _derivedSignalEngine = derivedSignalEngine;
            _systemEngine = systemEngine;
            _insightEngine = insightEngine;
            _healthservice = healthservice;
        }

        public async Task<(List<Signal> signals, List<DerivedSignal> behaviour, List<DerivedSignal> system, List<OperationalInsight> insights, List<RuleEvaluation> evaluations)> BuildSignalsAndDerived(AssessmentSession session)
        {
            Console.WriteLine(">>> PIPELINE: BuildSignalsAndDerived CALLED");

            // 1. Build base signals
            var signals = await _maturityBuilder.BuildSignals(session);

            Console.WriteLine(">>> PIPELINE: Calling DerivedSignalEngine");

            // 2. Behaviour layer
            var (behaviourSignals, behaviourEvaluations) = await _derivedSignalEngine.EvaluateSignals(signals);

            Console.WriteLine($">>> PIPELINE: Behaviour signals = {behaviourSignals.Count}");

            // 3. System layer
            var (systemSignals, systemEvaluations) = _systemEngine.Evaluate(behaviourSignals);

            Console.WriteLine($">>> SignalPipelineService: System signals = {systemSignals.Count}");

            // 4. Insight layer
            var insights = _insightEngine.GenerateInsights(behaviourSignals, systemSignals);

            Console.WriteLine($">>> PIPELINE: Insights = {insights.Count}");

            // 5. Combine evaluations (for diagnostics)
            var allEvaluations = behaviourEvaluations.Concat(systemEvaluations).ToList();

            return (signals, behaviourSignals, systemSignals, insights, allEvaluations);
        }
        public async Task<(
            List<DerivedSignal> behaviour,
            List<DerivedSignal> system,
            List<OperationalInsight> insights,
            List<RuleEvaluation> evaluations,
            SystemHealth health)>
        ProcessSignals(List<Signal> signals)
        {
            var predictabilitySignal = signals.FirstOrDefault(s => s.Name == "DeliveryPredictability");
            
            Console.WriteLine(">>> PIPELINE: ProcessSignals CALLED");
            if (predictabilitySignal != null)
            {
                var Confidence = predictabilitySignal.NormalisedValue;
                foreach (var signal in signals)
                {
                    if (signal.Name == "DeliveryPredictability")
                        continue;
                    signal.Confidence = Confidence;
                }
            }
            // 1. Behaviour layer
            var (behaviourSignals, behaviourEvaluations) =
                await _derivedSignalEngine.EvaluateSignals(signals);

            Console.WriteLine($">>> PIPELINE: Behaviour signals = {behaviourSignals.Count}");

            // 2. System layer
            var (systemSignals, systemEvaluations) =
                _systemEngine.Evaluate(behaviourSignals);

            Console.WriteLine($">>> PIPELINE: System signals = {systemSignals.Count}");

            // 3. Insight layer
            var insights =
                _insightEngine.GenerateInsights(behaviourSignals, systemSignals);

            Console.WriteLine($">>> PIPELINE: Insights = {insights.Count}");

            // 4. Combine evaluations
            var allEvaluations =
                behaviourEvaluations.Concat(systemEvaluations).ToList();

            // Compute Health
            var size = signals.FirstOrDefault(s => s.Name == "BacklogSize");
            var age = signals.FirstOrDefault(s => s.Name == "BacklogAge");
            var volatility = signals.FirstOrDefault(s => s.Name == "BacklogVolatility");
            var prioritisation = signals.FirstOrDefault(s => s.Name == "BacklogPrioritisation");
            var predictability = signals.FirstOrDefault(s => s.Name == "DeliveryPredictability");

            var health = _healthservice.Calculate(
                size,
                age,
                volatility,
                prioritisation,
                predictability,
                insights
            );

            return (behaviourSignals, systemSignals, insights, allEvaluations, health);
        }
        // Debug helper (replaces old SignalService.DebugSignals)
        public async Task<(List<Signal>, List<RuleEvaluation>)> DebugSignals(AssessmentSession session)
        {
            var signals = await _maturityBuilder.BuildSignals(session);
            var (_, evaluations) = await _derivedSignalEngine.EvaluateSignals(signals);

            return (signals, evaluations);
        }
    }
}