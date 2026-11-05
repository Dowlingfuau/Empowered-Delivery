using OperationalIntelligenceHub.Models;

namespace OperationalIntelligenceHub.Services
{
public class SignalNormaliserService
{
    public double Normalise(SignalDefinition definition, double rawValue) =>
        NormaliseTraced(definition, rawValue).Value;

    // The value and the formula that produced it, from one code path.
    public (double Value, FormulaTrace Formula) NormaliseTraced(SignalDefinition definition, double rawValue)
    {
        switch (definition.NormalisationRule)
        {
            case "value/5":
            {
                var value = rawValue / 5.0;
                return (value, FormulaTrace.Fraction("Level ÷ 5", TraceNumber.Format(rawValue), "5", value));
            }
            case "percentage":
            {
                var value = rawValue / 100.0;
                return (value, FormulaTrace.Fraction("Value ÷ 100", TraceNumber.Format(rawValue), "100", value));
            }
            default:
            {
                var value = Clamp(rawValue);
                return (value, FormulaTrace.Expr("Clamp(Value, 0, 1)", $"Clamp({TraceNumber.Format(rawValue)}, 0, 1)", value));
            }
        }
    }
    public double NormaliseLevel(int level, int maxLevel = 5)
    {
        return (double)level / maxLevel;
    }

    public double NormalisePercentage(double value)
    {
        return value / 100.0;
    }

    public double Clamp(double value)
    {
        if (value < 0) return 0;
        if (value > 1) return 1;
        return value;
    }
}
}