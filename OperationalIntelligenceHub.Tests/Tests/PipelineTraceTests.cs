using Xunit;
using OperationalIntelligenceHub.Models;
using OperationalIntelligenceHub.Services;
using Xunit.Abstractions;
using System.IO.Pipelines;

public class PipelineTraceTests
{
    private readonly ITestOutputHelper _output;

    public PipelineTraceTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void TeamHealthTrace_ShowsUnansweredAndTheExistingOverallCalculation()
    {
        var scores = new List<(string Name, int Score)>
        {
            ("Clarity", 100), ("Safety", 60), ("Flow", 20), ("Collaboration", 0),
            ("Focus", 0), ("Stakeholders", 0), ("Improvement", 0), ("Morale", 0)
        };

        var (inputs, calculations) = TeamHealthTraceAdapter.Build(scores, 22);

        Assert.Equal(8, inputs.Count);
        Assert.Equal("Not answered (0 in current calculation)", inputs[3].Value);
        Assert.All(inputs, input => Assert.Equal(TraceInputKind.UserInput, input.Kind));
        var calculation = Assert.Single(calculations);
        Assert.Same(inputs, calculation.Inputs);
        var step = Assert.Single(calculation.Steps);
        Assert.Equal("(100 + 60 + 20 + 0 + 0 + 0 + 0 + 0) / 8", step.Formula!.Expression);
        Assert.Equal("22", step.Formula.Result);
    }

    [Fact]
    public void TraceService_UsesExplicitInputsAndExactSignalNames()
    {
        var service = new SignalTraceService();
        var traces = new List<SignalTrace>
        {
            new()
            {
                SignalName = "DeliveryPredictability",
                Inputs = new()
                {
                    new() { Name = "Squad ownership", Source = "MaturityAssessment", Value = 3, Kind = TraceInputKind.UserInput },
                    new() { Name = "AverageThroughput", Source = "ThroughputHistory", Value = 4 }
                }
            }
        };
        var extra = new List<TraceInput>
        {
            new() { Name = "Squad ownership", Source = "MaturityAssessment", Value = 3, Kind = TraceInputKind.UserInput },
            new() { Name = "Safety", Source = "TeamHealth", Value = "Not answered", Kind = TraceInputKind.UserInput }
        };

        var inputs = service.GetUserInputs(traces, extra);

        Assert.Equal(2, inputs.Count);
        Assert.Contains(inputs, input => input.Name == "Safety" && Equals(input.Value, "Not answered"));
        Assert.Null(service.FindSignalTrace(traces, "Predictability"));
        Assert.Same(traces[0], service.FindSignalTrace(traces, "DeliveryPredictability"));
    }

    [Fact]
    public void TraceInsight_FollowsMatchedSystemRulesAndReportsMissingEvidence()
    {
        var evaluations = new List<RuleEvaluation>
        {
            new()
            {
                RuleName = "SystemRisk", Layer = "System", Result = true, Output = "High",
                Condition = "BehaviourRisk = High",
                Clauses = new() { new() { Subject = "BehaviourRisk", Operator = "=", Threshold = "High", Actual = "High", Matched = true } }
            },
            new()
            {
                RuleName = "BehaviourRisk", Layer = "Behaviour", Result = true, Output = "High",
                Condition = "Missing < 0.4 OR TeamOwnershipLevel < 0.4",
                Clauses = new()
                {
                    new() { Subject = "Missing", Actual = "missing", Matched = false, GroupIndex = 0 },
                    new() { Subject = "TeamOwnershipLevel", Operator = "<", Threshold = "0.4", Actual = "0.2", Matched = true, GroupIndex = 1 }
                }
            }
        };
        var trace = new SignalTrace
        {
            SignalName = "TeamOwnershipLevel",
            BehaviourId = "Squad_Ownership_L1",
            Inputs = new() { new() { Name = "Ownership", Value = 1, Kind = TraceInputKind.UserInput } }
        };
        var insight = new InsightTraceEntry { Name = "SystemRisk", Source = "SystemRuleEngine", Intensity = "High" };
        var service = new SignalTraceService();

        var path = service.TraceInsight(insight, evaluations, new[] { trace });
        Assert.Contains(path, item => item.Description.Contains("System rule SystemRisk"));
        Assert.Contains(path, item => item.Description.Contains("Behaviour rule BehaviourRisk"));
        Assert.Contains(path, item => item.Description.Contains("Behaviour ID: Squad_Ownership_L1"));
        Assert.DoesNotContain(path, item => item.Description.StartsWith("Missing:"));

        var missingPath = service.TraceInsight(insight, evaluations, Array.Empty<SignalTrace>());
        Assert.Contains(missingPath, item => item.Description.Contains("Source signal trace is unavailable or ambiguous"));
    }
    private void TracePipeline(
        TestRuleRepositoryService repo,
        List<Signal> inputs,
        List<DerivedSignal> behaviour,
        List<DerivedSignal> system,
        List<OperationalInsight> insights)
    {
        _output.WriteLine("=== INPUT SIGNALS ===");
        foreach (var s in inputs)
            _output.WriteLine($"INPUT: {s.Name} ({s.NormalisedValue:0.00})");

        _output.WriteLine("=== BEHAVIOUR SIGNALS ===");
        foreach (var b in behaviour)
            _output.WriteLine($"BEHAVIOUR: {b.Name} ({b.Intensity})");

        _output.WriteLine("=== SYSTEM SIGNALS ===");
        foreach (var s in system)
            _output.WriteLine($"SYSTEM: {s.Name} ({s.Intensity})");

        _output.WriteLine("=== INSIGHTS ===");
        foreach (var i in insights)
        {
            _output.WriteLine($"INSIGHT: {i.Name}");
            _output.WriteLine($"    SCORE: {i.Score}");
            _output.WriteLine($"    INTERPRETATION: {i.Interpretation}");

            // 🔍 NEW: show scoring inputs
            _output.WriteLine($"    DEBUG:");
            _output.WriteLine($"      Intensity: {i.Intensity}");
            _output.WriteLine($"      Source: {i.Source}");

            // 🔍 CRITICAL: trace rule lookup
            var rule = repo.BehaviourRules.FirstOrDefault(r => r.Name == i.Name)
                ?? repo.FlowRules.FirstOrDefault(r => r.Name == i.Name)
                ?? repo.SystemRules.FirstOrDefault(r => r.Name == i.Name);

            if (rule != null)
            {
                _output.WriteLine($"      Rule Found: YES");
                _output.WriteLine($"      Priority: {rule.Priority}");
                _output.WriteLine($"      Confidence: {rule.Confidence_Weight}");
            }
            else
            {
                _output.WriteLine($"      Rule Found: NO ❌");
            }

            foreach (var r in i.Reasons)
            {
                _output.WriteLine($"  REASON:");
                _output.WriteLine($"    Signal: {r.SignalName} ({r.SignalValue:0.00})");
                _output.WriteLine($"    Condition: {r.Condition}");
                _output.WriteLine($"    Result: {r.Result}");
                _output.WriteLine($"    Contribution: {r.ContributionScore:0.00}");
            }

            _output.WriteLine("----");
        }
    }
    [Fact]
    public async Task Pipeline_Should_Trigger_DeliveryFactoryRisk()
    {
        // Arrange
        var repo = new TestRuleRepositoryService();

        // Behaviour rule
        repo.AddBehaviourRule(new DerivedSignalRule
        {
            Name = "WeakProductOwnership",
            Domain = "Capability",
            Rules = new List<RuleCondition>
            {
                new RuleCondition
                {
                    Condition = "ProductOwnershipScore < 0.4",
                    Result = "High"
                }
            },
            Interpretation = "Product ownership is weak"
        });

        // Flow rule
        repo.AddFlowRule(new DerivedSignalRule
        {
            Name = "BacklogVolatility",
            Domain = "Flow",
            Rules = new List<RuleCondition>
            {
                new RuleCondition
                {
                    Condition = "BacklogVolatility > 0.7",
                    Result = "High"
                }
            },
            Interpretation = "Backlog is unstable"
        });

        // System rule (IMPORTANT — explicitly defined here)
        repo.AddSystemRule(new DerivedSignalRule
        {
            Name = "DeliveryFactoryRisk",
            Domain = "System",
            Rules = new List<RuleCondition>
            {
                new RuleCondition
                {
                    Condition = "WeakProductOwnership = High AND BacklogVolatility = High",
                    Result = "High"
                }
            },
            Interpretation = "Delivery is operating as a factory"
        });

        var pipeline = CreatePipeline(repo);

        var inputs = new List<Signal>
        {
            new Signal { Name = "ProductOwnershipScore", NormalisedValue = 0.2 },
            new Signal { Name = "BacklogVolatility", NormalisedValue = 0.9 }
        };

        // Act
        var result = await pipeline.ProcessSignals(inputs);

        var behaviour = result.behaviour;
        var system = result.system;
        var insights = result.insights;

        // Trace (THIS is the value of this file)
        TracePipeline(repo, inputs, behaviour, system, insights);

        // Assert (split layers)
        Assert.Contains(behaviour, b => b.Name == "WeakProductOwnership");
        Assert.Contains(behaviour, b => b.Name == "BacklogVolatility");

        Assert.Contains(system, s => s.Name == "DeliveryFactoryRisk");

        Assert.Contains(insights, i => i.Name == "DeliveryFactoryRisk");
    }
    private static SignalPipelineService CreatePipeline(TestRuleRepositoryService repo)
    {
        var derived = new DerivedSignalEngine(repo);
        var system = new SystemRuleEngine(repo);
        var insight = new OperationalInsightEngine(repo);

        return new SignalPipelineService(
            null!, // safe for ProcessSignals
            derived,
            system,
            insight,
            new HealthAggregationService()
        );
    }
    
}