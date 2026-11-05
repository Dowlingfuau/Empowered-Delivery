using System.Text.Json;
using OperationalIntelligenceHub.Models;
using OperationalIntelligenceHub.Services;

public class FlowRuleConfigurationTests
{
    [Fact]
    public void FlowDisruptionIsRemovedAndFlowOverloadSystemIsTheSingleOverloadRuleAndTemplate()
    {
        var repository = LoadProductionRules();

        Assert.DoesNotContain(repository.FlowRules, rule => rule.Name == "FlowDisruption");
        Assert.Single(repository.SystemRules, rule => rule.Name == "FlowOverloadSystem");

        var templatePath = FindRepositoryFile("wwwroot", "data", "insights", "insight_templates.json");
        using var document = JsonDocument.Parse(File.ReadAllText(templatePath));
        var templateSignals = document.RootElement
            .GetProperty("insight_templates")
            .EnumerateArray()
            .SelectMany(template => template.GetProperty("signals").EnumerateArray())
            .Select(signal => signal.GetProperty("name").GetString());

        Assert.DoesNotContain("FlowDisruption", templateSignals);
        Assert.Contains("FlowOverloadSystem", templateSignals);
    }

    [Theory]
    [InlineData(5.99, null)]
    [InlineData(6, "Low")]
    [InlineData(7.99, "Low")]
    [InlineData(8, "Moderate")]
    [InlineData(12, "Moderate")]
    [InlineData(12.01, "High")]
    public async Task BacklogPressure_UsesTheAgreedWorkHorizonBands(double workHorizon, string? expectedIntensity)
    {
        var result = await Evaluate(new Signal { Name = "WorkHorizon", NormalisedValue = workHorizon });

        var pressure = result.behaviour.SingleOrDefault(signal => signal.Name == "BacklogPressure");
        if (expectedIntensity == null)
        {
            Assert.Null(pressure);
            return;
        }

        Assert.Equal(expectedIntensity, pressure?.Intensity);
    }

    [Theory]
    [InlineData(0, null)]
    [InlineData(3.99, "Low")]
    [InlineData(4, "Moderate")]
    [InlineData(11.99, "Moderate")]
    [InlineData(12, "High")]
    public async Task IntakePressure_UsesNetGrowthInDeliveryCapacityWeeks(double growthWeeks, string? expectedIntensity)
    {
        var result = await Evaluate(new Signal { Name = "BacklogGrowthWeeks", NormalisedValue = growthWeeks });

        var pressure = result.behaviour.SingleOrDefault(signal => signal.Name == "IntakePressure");
        if (expectedIntensity == null)
        {
            Assert.Null(pressure);
            return;
        }

        Assert.Equal(expectedIntensity, pressure?.Intensity);
    }

    [Theory]
    [InlineData(0.8, 1, null)]
    [InlineData(0.799, 1, "Moderate")]
    [InlineData(0.6, 1, "Moderate")]
    [InlineData(0.599, 1, "High")]
    [InlineData(0.4, 0, null)]
    public async Task DeliveryInstability_RequiresAvailableHistoryAndMeaningfulVariation(
        double predictability,
        double historyAvailable,
        string? expectedIntensity)
    {
        var result = await Evaluate(
            new Signal { Name = "DeliveryPredictability", NormalisedValue = predictability },
            new Signal { Name = "DeliveryPredictabilityDataAvailable", NormalisedValue = historyAvailable });

        var instability = result.behaviour.SingleOrDefault(signal => signal.Name == "DeliveryInstability");
        if (expectedIntensity == null)
        {
            Assert.Null(instability);
            return;
        }

        Assert.Equal(expectedIntensity, instability?.Intensity);
    }

    [Fact]
    public async Task BacklogAgeRulesSeparateFreshWorkFromOldWorkAndIdleWorkRequiresStableCapacity()
    {
        var oldWork = await Evaluate(
            new Signal { Name = "OldWorkPercentage", NormalisedValue = 0.51 },
            new Signal { Name = "FreshWorkPercentage", NormalisedValue = 0.1 },
            new Signal { Name = "WorkHorizon", NormalisedValue = 6 },
            new Signal { Name = "DeliveryPredictability", NormalisedValue = 0.8 },
            new Signal { Name = "DeliveryPredictabilityDataAvailable", NormalisedValue = 1 });
        var freshWork = await Evaluate(
            new Signal { Name = "OldWorkPercentage", NormalisedValue = 0.2 },
            new Signal { Name = "FreshWorkPercentage", NormalisedValue = 0.51 });

        Assert.Contains(oldWork.behaviour, signal => signal.Name == "BacklogAgingRisk" && signal.Intensity == "High");
        Assert.Contains(oldWork.behaviour, signal => signal.Name == "IdleWorkRisk" && signal.Intensity == "High");
        Assert.DoesNotContain(freshWork.behaviour, signal => signal.Name == "BacklogAgingRisk");
        Assert.Contains(freshWork.behaviour, signal => signal.Name == "BacklogUnderRipe" && signal.Intensity == "High");
    }

    [Fact]
    public async Task PrioritisationRulesReportUnprioritisedWorkAndPriorityConcentrationSeparately()
    {
        var result = await Evaluate(
            new Signal { Name = "UnprioritisedWorkPercentage", NormalisedValue = 0.51 },
            new Signal { Name = "PriorityBandConcentration", NormalisedValue = 0.6 });

        Assert.Contains(result.behaviour, signal => signal.Name == "UnprioritisedWorkRisk" && signal.Intensity == "High");
        Assert.Contains(result.behaviour, signal => signal.Name == "PriorityDifferentiationRisk" && signal.Intensity == "High");
        Assert.DoesNotContain(result.behaviour, signal => signal.Name == "PrioritisationClarity");
    }

    [Theory]
    [InlineData("BacklogGrowthPercentage", 0.31, "The backlog is growing rapidly")]
    [InlineData("BacklogShrinkagePercentage", 0.31, "The backlog is shrinking rapidly")]
    public async Task BacklogVolatility_ReportsTheDirectionOfRapidChange(
        string signalName,
        double percentage,
        string expectedInterpretation)
    {
        var result = await Evaluate(new Signal { Name = signalName, NormalisedValue = percentage });
        var insight = Assert.Single(result.insights, item => item.Name == "BacklogVolatility");

        Assert.StartsWith(expectedInterpretation, insight.Interpretation);
    }

    [Fact]
    public async Task HighBacklogAndIntakePressureProduceOneSystemOverloadInsight()
    {
        var result = await Evaluate(
            new Signal { Name = "WorkHorizon", NormalisedValue = 13 },
            new Signal { Name = "BacklogGrowthWeeks", NormalisedValue = 12 });

        Assert.Single(result.system, signal => signal.Name == "FlowOverloadSystem");
        Assert.Single(result.insights, insight => insight.Name == "FlowOverloadSystem");
        Assert.DoesNotContain(result.behaviour, signal => signal.Name == "FlowDisruption");
        Assert.DoesNotContain(result.insights, insight => insight.Name == "FlowDisruption");
    }

    private static async Task<(
        List<DerivedSignal> behaviour,
        List<DerivedSignal> system,
        List<OperationalInsight> insights,
        List<RuleEvaluation> evaluations,
        SystemHealth health)> Evaluate(params Signal[] signals)
    {
        var repository = LoadProductionRules();
        var pipeline = new SignalPipelineService(
            null!,
            new DerivedSignalEngine(repository),
            new SystemRuleEngine(repository),
            new OperationalInsightEngine(repository),
            new HealthAggregationService());

        return await pipeline.ProcessSignals(signals.ToList());
    }

    private static TestRuleRepositoryService LoadProductionRules()
    {
        var repository = new TestRuleRepositoryService();
        repository.FlowRules.Clear();
        repository.FlowRules.AddRange(LoadRules("wwwroot", "data", "rules", "flow_rules_v3.json"));
        repository.SystemRules.Clear();
        repository.SystemRules.AddRange(LoadRules("wwwroot", "data", "rules", "system_rules.json"));
        return repository;
    }

    private static List<DerivedSignalRule> LoadRules(params string[] pathParts)
    {
        var path = FindRepositoryFile(pathParts);
        var rules = JsonSerializer.Deserialize<DerivedSignalRuleSet>(
            File.ReadAllText(path),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })?.Derived_Signal_Rules;

        return rules ?? throw new InvalidDataException($"No derived signal rules were loaded from '{path}'.");
    }

    private static string FindRepositoryFile(params string[] pathParts)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            var path = Path.Combine(new[] { directory.FullName, "OperationalIntelligenceHub" }.Concat(pathParts).ToArray());
            if (File.Exists(path))
                return path;

            directory = directory.Parent;
        }

        throw new FileNotFoundException($"Could not find repository file '{Path.Combine(pathParts)}'.");
    }
}
