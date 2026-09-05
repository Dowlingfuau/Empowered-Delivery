using OperationalIntelligenceHub.Models;

namespace OperationalIntelligenceHub.Services
{
public class SignalNormaliserService
{
    public double Normalise(SignalDefinition definition, double rawValue)
    {
       return definition.NormalisationRule switch
       {
           "value/5" => rawValue / 5.0,
           "percentage" => rawValue / 100.0,
           _ => Clamp(rawValue)
       };
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