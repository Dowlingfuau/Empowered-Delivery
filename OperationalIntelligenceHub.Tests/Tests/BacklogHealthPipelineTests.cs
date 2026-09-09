using Xunit;
using OperationalIntelligenceHub.Models;
using OperationalIntelligenceHub.Services;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit.Abstractions;
using System.Data;

public class BacklogHealthPipelineTests
{
    private readonly ITestOutputHelper _output;

    public BacklogHealthPipelineTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task HighPredictability_ShouldTriggerDeliveryInstabilityHigh()
    {
        // Arrange
        var repo = new TestRuleRepositoryService();

        repo.AddFlowRule(new DerivedSignalRule
        {
            Name = "DeliveryInstability",
            Domain = "Flow",
            Priority = 8,
            Confidence_Weight = 0.85,
            Rules = new List<RuleCondition>
            {
                new RuleCondition
                {
                    Condition = "BacklogPredictability > 0.6",
                    Result = "High"
                }
            },
            Interpretation = "Delivery output varies significantly"
        });

        var pipeline = CreatePipeline(repo);

        var signals = new List<Signal>
        {
            new Signal
            {
                Name = "BacklogPredictability",
                NormalisedValue = 0.8
            }
        };

        // Act
        var (behaviour, system, insights, evaluations, _) =
            await pipeline.ProcessSignals(signals);

        // Debug (CRITICAL)
        foreach (var b in behaviour)
            _output.WriteLine($"BEHAVIOUR: {b.Name} {b.Intensity}");

        foreach (var i in insights)
            _output.WriteLine($"INSIGHT: {i.Name} {i.Intensity}");

        // Assert
        Assert.Contains(behaviour,
            b => b.Name == "DeliveryInstability" && b.Intensity == "High");
    }

    [Fact]
    public async Task LargeBacklog_ShouldTriggerBacklogPressureHigh()
    {
        var repo = new TestRuleRepositoryService();

        repo.AddFlowRule(new DerivedSignalRule
        {
            Name = "BacklogPressure",
            Domain = "Flow",
            Priority = 8,
            Confidence_Weight = 0.85,
            Rules = new List<RuleCondition>
            {
                new RuleCondition
                {
                    Condition = "BacklogSize > 0.7",
                    Result = "High"
                }
            },
            Interpretation = "Backlog is too large"
        });

        var pipeline = CreatePipeline(repo);

        var signals = new List<Signal>
        {
            new Signal { Name = "BacklogSize", NormalisedValue = 0.9 }
        };

        var (behaviour, system, insights, _, _) =
            await pipeline.ProcessSignals(signals);

        Assert.Contains(behaviour,
            b => b.Name == "BacklogPressure" && b.Intensity == "High");
    }

    [Fact]
    public async Task HighAgeAndHighSize_ShouldPrioritiseBacklogPressureOverAging()
    {
        var repo = new TestRuleRepositoryService();

        // Size rule (higher priority)
        repo.AddFlowRule(new DerivedSignalRule
        {
            Name = "BacklogPressure",
            Domain = "Flow",
            Priority = 9,
            Confidence_Weight = 0.9,
            Rules = new List<RuleCondition>
            {
                new RuleCondition
                {
                    Condition = "BacklogSize > 0.7",
                    Result = "High"
                }
            },
            Interpretation = "Backlog is too large"
        });

        // Age rule (lower priority)
        repo.AddFlowRule(new DerivedSignalRule
        {
            Name = "BacklogAgingRisk",
            Domain = "Flow",
            Priority = 7,
            Confidence_Weight = 0.8,
            Rules = new List<RuleCondition>
            {
                new RuleCondition
                {
                    Condition = "BacklogAge > 0.7",
                    Result = "High"
                }
            },
            Interpretation = "Work is aging"
        });

        var pipeline = CreatePipeline(repo);

        var signals = new List<Signal>
        {
            new Signal { Name = "BacklogSize", NormalisedValue = 0.9 },
            new Signal { Name = "BacklogAge", NormalisedValue = 0.9 }
        };

        var (behaviour, system, insights, _, _) =
            await pipeline.ProcessSignals(signals);

        // Debug
        foreach (var i in insights)
            _output.WriteLine($"INSIGHT: {i.Name} {i.Score}");

        // Assert BOTH exist
        Assert.Contains(insights, i => i.Name == "BacklogPressure");
        Assert.Contains(insights, i => i.Name == "BacklogAgingRisk");

        // Assert ordering (THIS is the important part)
        Assert.True(insights[0].Name == "BacklogPressure");
    }

    [Fact]
    public async Task SmallBacklog_HighAge_ShouldPrioritiseAgingOverPressure()
    {
        var repo = new TestRuleRepositoryService();

        // Size rule (normally high priority)
        repo.AddFlowRule(new DerivedSignalRule
        {
            Name = "BacklogPressure",
            Domain = "Flow",
            Priority = 9,
            Confidence_Weight = 0.9,
            Rules = new List<RuleCondition>
            {
                new RuleCondition
                {
                    Condition = "BacklogSize > 0.7",
                    Result = "High"
                }
            },
            Interpretation = "Backlog is too large"
        });

        // Age rule
        repo.AddFlowRule(new DerivedSignalRule
        {
            Name = "BacklogAgingRisk",
            Domain = "Flow",
            Priority = 7,
            Confidence_Weight = 0.8,
            Rules = new List<RuleCondition>
            {
                new RuleCondition
                {
                    Condition = "BacklogAge > 0.7",
                    Result = "High"
                }
            },
            Interpretation = "Work is aging"
        });

        var pipeline = CreatePipeline(repo);

        var signals = new List<Signal>
        {
            new Signal { Name = "BacklogSize", NormalisedValue = 0.3 }, // SMALL backlog
            new Signal { Name = "BacklogAge", NormalisedValue = 0.9 }  // OLD work
        };

        var (behaviour, system, insights, _, _) =
            await pipeline.ProcessSignals(signals);

        foreach (var i in insights)
        {
            _output.WriteLine($"INSIGHT: {i.Name}");
            _output.WriteLine($"    SCORE: {i.Score}");
            _output.WriteLine($"    INTERPRETATION: {i.Interpretation}");

            _output.WriteLine($"  PRIORITISATION:");
            _output.WriteLine($"    Rank Score: {i.Score}");
           
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
        // Assert
        Assert.Contains(insights, i => i.Name == "BacklogAgingRisk");

        // 👇 THIS is the key expectation
        Assert.True(insights[0].Name == "BacklogAgingRisk");
    }

    [Fact]
    public async Task MultipleSignals_ShouldShowMultipleReasons()
    {
        var repo = new TestRuleRepositoryService();

        repo.AddFlowRule(new DerivedSignalRule
        {
            Name = "BacklogHealthRisk",
            Domain = "Flow",
            Priority = 8,
            Confidence_Weight = 0.9,
            Rules = new List<RuleCondition>
            {
                new RuleCondition
                {
                    Condition = "BacklogAge > 0.7 AND BacklogSize > 0.7",
                    Result = "High"
                }
            },
            Interpretation = "Backlog is unhealthy"
        });

        var pipeline = CreatePipeline(repo);

        var signals = new List<Signal>
        {
            new Signal { Name = "BacklogAge", NormalisedValue = 0.9 },
            new Signal { Name = "BacklogSize", NormalisedValue = 0.9 }
        };

        var (_, _, insights, _, _) = await pipeline.ProcessSignals(signals);

        foreach (var i in insights)
        {
            _output.WriteLine($"INSIGHT: {i.Name}");
            _output.WriteLine($"    SCORE: {i.Score}");
            _output.WriteLine($"    INTERPRETATION: {i.Interpretation}");

            _output.WriteLine($"  PRIORITISATION:");
            _output.WriteLine($"    Rank Score: {i.Score}");
           
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

        var insight = insights.First(i => i.Name == "BacklogHealthRisk");

        // 🔥 THIS is the key assertion
        Assert.True(insight.Reasons.Count == 2);
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

