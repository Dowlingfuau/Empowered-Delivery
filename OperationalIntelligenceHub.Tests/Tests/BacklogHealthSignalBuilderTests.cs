using OperationalIntelligenceHub.Models;
using OperationalIntelligenceHub.Services;

public class BacklogHealthSignalBuilderTests
{
    [Theory]
    [InlineData(120, 30, 10, 0.2, 0.8, 1)]
    [InlineData(80, 10, 30, -0.2, 0.8, -1)]
    [InlineData(100, 20, 20, 0, 1, 0)]
    public void BuildSignals_UsesStartingBacklogForVolatilityAndPreservesContinuum(
        int currentBacklog,
        int itemsAdded,
        int itemsCompleted,
        double expectedChange,
        double expectedContinuum,
        int expectedDirection)
    {
        var (signals, traces) = BuildSignals(currentBacklog, itemsAdded, itemsCompleted);
        var volatility = Assert.Single(signals, signal => signal.Name == "BacklogVolatility");
        var trace = Assert.Single(traces, item => item.SignalName == "BacklogVolatility");

        Assert.Equal(expectedDirection, volatility.Direction);
        Assert.Equal(expectedContinuum, volatility.ContinuumValue);
        Assert.Equal(expectedChange, (double)volatility.Metadata!["percentageChange"]);
        Assert.Equal(currentBacklog - itemsAdded + itemsCompleted, volatility.Metadata["startingBacklog"]);
        Assert.Contains(trace.CalculationSteps, step => step.StepName == "PercentageChange" && Equals(step.Value, expectedChange));
    }

    [Fact]
    public void BuildSignals_ZeroStartingBacklogReportsPercentageUnavailableAndKeepsGrowthAtRightEndpoint()
    {
        var (signals, traces) = BuildSignals(currentBacklog: 20, itemsAdded: 20, itemsCompleted: 0);
        var volatility = Assert.Single(signals, signal => signal.Name == "BacklogVolatility");
        var trace = Assert.Single(traces, item => item.SignalName == "BacklogVolatility");

        Assert.Equal(1, volatility.Direction);
        Assert.Equal(0, volatility.ContinuumValue);
        Assert.Equal(0, volatility.Metadata!["startingBacklog"]);
        Assert.Contains("zero starting backlog", volatility.Metadata["percentageChangeUnavailable"].ToString());
        Assert.Contains(trace.CalculationSteps, step => step.StepName == "PercentageChange" && step.Value is null);
    }

    [Fact]
    public void BuildSignals_MissingCurrentBacklogDoesNotInventPercentageChange()
    {
        var (signals, _) = BuildSignals(currentBacklog: null, itemsAdded: 20, itemsCompleted: 0);
        var volatility = Assert.Single(signals, signal => signal.Name == "BacklogVolatility");

        Assert.Equal(1, volatility.Direction);
        Assert.Equal(0.5, volatility.ContinuumValue);
        Assert.Contains("without the current backlog count", volatility.Metadata!["percentageChangeUnavailable"].ToString());
    }

    [Fact]
    public void BuildSignals_IncompleteAgeDistributionProducesNeutralUnavailableSignal()
    {
        var (signals, traces) = BuildSignals(
            currentBacklog: 10,
            itemsAdded: 0,
            itemsCompleted: 0,
            freshItems: 5,
            midAgeItems: null,
            oldItems: 2);
        var age = Assert.Single(signals, signal => signal.Name == "BacklogAge");
        var trace = Assert.Single(traces, item => item.SignalName == "BacklogAge");

        Assert.Equal(0.5, age.ContinuumValue);
        Assert.Equal(0, age.Direction);
        Assert.False((bool)age.Metadata!["ageDataAvailable"]);
        Assert.Contains(trace.CalculationSteps, step => step.StepName == "AgeDataAvailable" && Equals(step.Value, false));
    }

    [Fact]
    public void BuildSignals_NoMatchedVolatilityPeriodProducesNeutralUnavailableSignal()
    {
        var (signals, _) = BuildSignals(
            currentBacklog: 10,
            itemsAdded: 0,
            itemsCompleted: 0,
            matchedVolatilityPeriodCount: 0);
        var volatility = Assert.Single(signals, signal => signal.Name == "BacklogVolatility");

        Assert.Equal(0.5, volatility.ContinuumValue);
        Assert.False((bool)volatility.Metadata!["volatilityDataAvailable"]);
    }

    [Fact]
    public void BuildSignals_OneThroughputObservationDoesNotClaimPredictability()
    {
        var (signals, traces) = BuildSignals(
            currentBacklog: 10,
            itemsAdded: 0,
            itemsCompleted: 0,
            throughputHistory: new List<int> { 10 });
        var predictability = Assert.Single(signals, signal => signal.Name == "DeliveryPredictability");
        var trace = Assert.Single(traces, item => item.SignalName == "DeliveryPredictability");

        Assert.Equal(0.5, predictability.NormalisedValue);
        Assert.False((bool)predictability.Metadata!["predictabilityDataAvailable"]);
        Assert.Contains(trace.CalculationSteps, step => step.StepName == "InsufficientData");
    }

    private static (List<Signal> Signals, List<SignalTrace> Traces) BuildSignals(
        int? currentBacklog,
        int itemsAdded,
        int itemsCompleted,
        int? freshItems = 0,
        int? midAgeItems = 0,
        int? oldItems = 0,
        int matchedVolatilityPeriodCount = 1,
        List<int>? throughputHistory = null) =>
        new BacklogHealthSignalBuilderService().BuildSignals(
            Guid.Empty,
            Guid.Empty,
            currentBacklog,
            itemsAdded,
            itemsCompleted,
            throughputHistory ?? new List<int>(),
            freshItems: freshItems,
            midAgeItems: midAgeItems,
            oldItems: oldItems,
            highPriority: 0,
            mediumPriority: 0,
            lowPriority: 0,
            unprioritisedItems: 0,
            matchedVolatilityPeriodCount: matchedVolatilityPeriodCount,
            prioritisationDataAvailable: true);
}
