using OperationalIntelligenceHub.Models;
using OperationalIntelligenceHub.Services;

public class HealthAggregationServiceTests
{
    [Fact]
    public void Calculate_UsesApprovedWeightsAndHigherPositionMeansHealthier()
    {
        var predictability = Signal(
            "DeliveryPredictability",
            0.25,
            ("predictabilityDataAvailable", true),
            ("historyLength", 3),
            ("averageThroughput", 10.0));
        predictability.NormalisedValue = 0.75;

        var health = Calculate(
            Signal("BacklogSize", 0.0, ("sizeDataAvailable", true)),
            Signal("BacklogAge", 0.5, ("ageDataAvailable", true)),
            Signal("BacklogVolatility", 1.0, ("volatilityDataAvailable", true)),
            Signal("BacklogPrioritisation", 0.25, ("prioritisationDataAvailable", true)),
            predictability);

        Assert.Equal(0.6125, health.Position, 10);
        Assert.Equal("Needs Attention", health.State);
    }

    [Fact]
    public void Calculate_RenormalizesWeightsAcrossAvailableSignals()
    {
        var health = Calculate(
            Signal("BacklogSize", 0.0, ("sizeDataAvailable", false)),
            Signal("BacklogAge", 0.8, ("ageDataAvailable", true)),
            Signal("BacklogVolatility", 0.0, ("volatilityDataAvailable", false)),
            Signal("BacklogPrioritisation", 0.0, ("prioritisationDataAvailable", false)),
            Signal("DeliveryPredictability", 0.0, ("historyLength", 1), ("averageThroughput", 10.0)));

        Assert.Equal(0.8, health.Position, 12);
        Assert.Equal("Healthy", health.State);
    }

    [Fact]
    public void Calculate_ReturnsInsufficientDataWhenNoSignalIsAvailable()
    {
        var health = Calculate(
            Signal("BacklogSize", 0.9, ("sizeDataAvailable", false)),
            Signal("BacklogAge", 0.9, ("ageDataAvailable", false)),
            Signal("BacklogVolatility", 0.9, ("volatilityDataAvailable", false)),
            Signal("BacklogPrioritisation", 0.9, ("prioritisationDataAvailable", false)),
            Signal("DeliveryPredictability", 0.9, ("historyLength", 1), ("averageThroughput", 10.0)));

        Assert.Equal(0.5, health.Position);
        Assert.Equal("Insufficient Data", health.State);
    }

    [Theory]
    [InlineData(0.39, "Unhealthy")]
    [InlineData(0.4, "Needs Attention")]
    [InlineData(0.69, "Needs Attention")]
    [InlineData(0.7, "Healthy")]
    public void Calculate_MapsHealthThresholds(double position, string expectedState)
    {
        var health = Calculate(
            null,
            Signal("BacklogAge", position, ("ageDataAvailable", true)),
            null,
            null,
            null);

        Assert.Equal(expectedState, health.State);
    }

    private static SystemHealth Calculate(
        Signal? size,
        Signal? age,
        Signal? volatility,
        Signal? prioritisation,
        Signal? predictability) =>
        new HealthAggregationService().Calculate(size, age, volatility, prioritisation, predictability);

    private static Signal Signal(string name, double value, params (string Key, object Value)[] metadata)
    {
        var signal = new Signal
        {
            Name = name,
            ContinuumValue = value,
            NormalisedValue = value,
            Metadata = metadata.ToDictionary(item => item.Key, item => item.Value)
        };
        return signal;
    }
}
