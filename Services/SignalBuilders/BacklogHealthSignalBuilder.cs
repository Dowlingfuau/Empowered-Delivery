using System.Data.Common;
using OperationalIntelligenceHub.Models;

namespace OperationalIntelligenceHub.Services
{
    public class BacklogHealthSignalBuilderService
    {
        // TODO: Add metadata
        public List<Signal> BuildSignals(
            Guid workspaceId,
            Guid squadId,
            int totalItems,
            int itemsAdded,
            int itemsCompleted,
            List<int> throughputHistory,
            int freshItems,
            int midAgeItems,
            int oldItems,
            int highPriority,
            int mediumPriority,
            int lowPriority,
            int unprioritisedItems
        )
        {
            var signals = new List<Signal>();

            // ================================
            // BACKLOG SIZE
            // ================================

            double avgThroughput = throughputHistory.Any() ? throughputHistory.Average() : 0;
            double weeksofWork = avgThroughput == 0 ? 0 : totalItems / avgThroughput;

            // Normalised score
            double sizeNormalised =
                weeksofWork <= 0 ? 0.0:
                weeksofWork < 1 ? 0.3 :
                weeksofWork < 3 ? 0.6 :
                weeksofWork <= 5 ? 1.0 :
                weeksofWork <= 8 ? 0.6 :
                weeksofWork <= 12 ? 0.3 : 0.0;


            // CONTINUUM CALCULATION
            double centre = 5.0;
            double max = 12.0;

            double sizeMagnitude = Math.Abs(weeksofWork - centre);

            double sizeContinuumValue;
            if (weeksofWork <= centre)
            {
                // LEFT side (0 - 5)
                double leftMax = centre;
                sizeContinuumValue = weeksofWork / leftMax;
            }
            else
            {
                // RIGHT side (5 - 12)
                double rightMax = max - centre;
                sizeContinuumValue = 1 - ((weeksofWork - centre) / rightMax);
            }

            // Direction
            int sizeDirection;
            if (weeksofWork < centre)
                sizeDirection = -1;
            else if (weeksofWork > centre)
                sizeDirection = 1;
            else
                sizeDirection = 0;

            // Create signal
            var sizeSignal = CreateSignal("BacklogSize", "Flow", weeksofWork, sizeNormalised);

            // Attach continuum properties
            sizeSignal.ContinuumValue = sizeContinuumValue;
            sizeSignal.Direction = sizeDirection;
            sizeSignal.Metadata = new Dictionary<string, object>
            {
                { "weeksofWork", weeksofWork},
                { "averageThroughput", avgThroughput },
                { "totalItems", totalItems }
            };

            signals.Add(sizeSignal);

            // ================================
            // BACKLOG AGE
            // ================================

            int totalAgeItems = freshItems + midAgeItems + oldItems;

            double freshPercent = 0;
            double midPercent = 0;
            double oldPercent = 0;
            
            //Avoid divide by zero
            if (totalAgeItems == 0)
            {
                var neutral = CreateSignal("BacklogAge", "Flow", 0.5, 0.5);
                neutral.Shape = "Symmetric";
                signals.Add(neutral);
            }
            else
            {
                freshPercent = (double)freshItems / totalAgeItems;
                midPercent = (double)midAgeItems / totalAgeItems;
                oldPercent = (double)oldItems / totalAgeItems; 

            // Calculate Score
            // Actual distribution
            var actual = new[]
            {
                freshPercent,
                midPercent,
                oldPercent
            };

            // Ideal bell distribution (3 bucket approx)
            var ideal = new[]
            {
                0.2,
                0.6,
                0.2
            };

            // Calculate distance
            double distributionDistance = 0;

            for (int i = 0; i < 3; i++)
            {
                distributionDistance += Math.Abs(actual[i] - ideal [i]);
            }

            // Convert to score (0 = bad, 1 = ideal)
            double maxDistance = 1.6;
            
            double ageScore = 1 - (distributionDistance / maxDistance);

            // Calculate deviation
            double bias = freshPercent - oldPercent;

            int ageDirection =
                bias == 0 ? 0 :
                bias > 0 ? -1 : 1;

            double directionalStrength = Math.Abs(bias);
            
            // Shape
            // TEMP: Split mid into 3 (approx for now)
            string shape;

            // Strong symmetry (ideal)
            if (Math.Abs(freshPercent - oldPercent) < 0.1 && midPercent >= 0.5)
            {
                shape = "Symmetric";
            }
            // Bimodal (both extremes high, mid low)
            else if (freshPercent > 0.3 && oldPercent > 0.3 && midPercent < 0.3)
            {
                shape = "Bimodal";
            }
            // Left skew (fresh dominant)
            else if (freshPercent > oldPercent + 0.1)
            {
                shape = "SkewLeft";
            }
            // Right skew (old dominant)
            else if (oldPercent > freshPercent + 0.1)
            {
                shape = "SkewRight";
            }
            // Fallback
            else
            {
                shape = "Symmetric";
            }

            // Create signal
            double ageContinuumValue = ageScore;
            ageContinuumValue = ageContinuumValue * (1 - directionalStrength);

            var ageSignal = CreateSignal("BacklogAge", "Flow", ageScore, ageScore);

            ageSignal.Direction = ageDirection;
            ageSignal.Shape = shape;
            ageSignal.ContinuumValue = ageContinuumValue;
            ageSignal.Metadata = new Dictionary<string, object>
            {
                { "freshPercent", freshPercent },
                { "midPercent", midPercent },
                { "oldPercent", oldPercent },
                { "shape", shape },
                { "bias", bias }
            };
            
            signals.Add(ageSignal);
            }
                
            // TODO: Add recency 

            // ================================
            // BACKLOG VOLATILITY
            // ================================

            // Ratio
            double ratio;

            if (itemsCompleted == 0 && itemsAdded == 0)
            {
                ratio = 1; // neutral
            }
            else if (itemsCompleted == 0)
            {
                ratio = double.MaxValue; // Extreme growth
            }
            else
            {
                ratio = (double)itemsAdded / itemsCompleted;
            }
            // Balance score 9symmetric 0-1)
            double balanceRatio;

            if (itemsAdded == 0 && itemsCompleted == 0)
            {
                balanceRatio = 1.0;
            }
            else if (itemsAdded == 0 || itemsCompleted == 0)
            {
                balanceRatio = 0.0;
            }
            else if (ratio >= 1)
            {
                balanceRatio = (double)itemsCompleted / itemsAdded;
            }
            else
            {
                balanceRatio = (double)itemsAdded / itemsCompleted;
            }
            // Direction
            int volatilityDirection =
                itemsAdded == itemsCompleted ? 0 :
                itemsAdded > itemsCompleted ? 1 : -1;
            // Relative delta (sclae-aware)
            double relativeDelta = 0;

            if (totalItems >0)
            {
                relativeDelta = (double)(itemsAdded - itemsCompleted) / totalItems;
            }
            // Continuum (UX only - magnitude of change vs backlog size)
            double volatilityContinuumValue;
            //Handle empty backlog case
            if (totalItems == 0)
            {
                volatilityContinuumValue = itemsAdded > 0 ? 0.0 : 1.0;
            }
            else
            {
                double volatilityMagnitude = Math.Abs(relativeDelta);
                double maxExpectedChange = 1.0; // 100% change = extreme
                double normalisedMagnitude = Math.Min(volatilityMagnitude / maxExpectedChange, 1.0);
                volatilityContinuumValue = 1 - normalisedMagnitude;
            }
            // Activity (for flat detection)

            double totalFlow = itemsAdded + itemsCompleted;

            bool isFlat = avgThroughput > 0 && totalFlow < (0.5 * avgThroughput);
            // TODO: Pattern (simple v1) - Cyclical detection will be added later using bucketed inputs
            string pattern = isFlat ? "Flat" : "None";
            // Creat signal
            var volatilitySignal = CreateSignal("BacklogVolatility", "Flow", ratio, balanceRatio);
            volatilitySignal.ContinuumValue = volatilityContinuumValue;
            volatilitySignal.Direction = volatilityDirection;
            volatilitySignal.Metadata = new Dictionary<string, object>
            {
                { "relativeDelta", relativeDelta },
                { "pattern", pattern },
                { "ratio", ratio },
                { "balanceScore", balanceRatio },
                { "itemsAdded", itemsAdded },
                { "itemsCompleted", itemsCompleted }
            };
            
            signals.Add(volatilitySignal);

            Console.WriteLine(
            $"VOL → ratio:{ratio:F2} | score:{balanceRatio:F2} | dir:{volatilityDirection} | delta:{relativeDelta:F2} | pattern:{pattern}");

            // ================================
            // PRIORITISATION
            // ================================

            int prioritisedTotal = highPriority + mediumPriority + lowPriority;
            // derive unprioritised from totalItems
            int derivedUnprioritised = Math.Max(0, totalItems - prioritisedTotal);
            // recompute total safely
            int totalPriorityItems = prioritisedTotal + derivedUnprioritised;
            // percentages
            double highPercent = totalPriorityItems == 0 ? 0 : (double)highPriority / totalPriorityItems;
            double mediumPercent = totalPriorityItems == 0 ? 0 : (double)mediumPriority / totalPriorityItems;
            double lowPercent = totalPriorityItems == 0 ? 0 : (double)lowPriority / totalPriorityItems;
            double unprioritisedPercent = totalPriorityItems == 0 ? 0 : (double)derivedUnprioritised / totalPriorityItems;

            double coverage = 1 - unprioritisedPercent;
            // Equal distribution baseline
            double expected = 1.0 / 3.0;

            double distributionDeviation = 
                Math.Abs(highPercent - expected) +
                Math.Abs(mediumPercent - expected) +
                Math.Abs(lowPercent - expected);
            // Normalise (max possible deviation = ~1.33)
            double maxDeviation = 1.33;

            double distributionScore = 1 - (distributionDeviation / maxDeviation);
            distributionScore = Math.Max(0, Math.Min(1, distributionScore));

            double highPenalty = highPercent <= 0.4
                ? 1.0
                : Math.Max(0, 1 - (highPercent - 0.4));

            double prioritisationScore =
                (coverage * 0.5) +
                (distributionScore * 0.5);
            // Continuum Value
            double prioritisationContinuumValue =
                1 - distributionDeviation; // closer to 1 = more balanced
            prioritisationContinuumValue = Math.Max(0, Math.Min(1, prioritisationContinuumValue));
            // Direction
            int prioritisationDirection;
            if (unprioritisedPercent > 0.2)
            {
                prioritisationDirection = -1; // no priorities
            }
            else
            {
                prioritisationDirection = distributionDeviation > 0.2 ? 1 : 0;
            }
            // Build Signal
            var prioritisationSignal = CreateSignal("BacklogPrioritisation", "Flow", coverage, prioritisationScore);

            prioritisationSignal.ContinuumValue = prioritisationContinuumValue;
            prioritisationSignal.Direction = prioritisationDirection;
            prioritisationSignal.Metadata = new Dictionary<string, object>
            {
                { "highPercent", highPercent },
                { "mediumPercent", mediumPercent },
                { "lowPercent", lowPercent },
                { "unprioritisedPercent", unprioritisedPercent },
                { "coverage", coverage }
            };

            signals.Add(prioritisationSignal);

            // ================================
            // DELIVERY PREDICTABILITY
            // ================================

            double avg = avgThroughput;
            double stdDev = 0;

            if (throughputHistory.Any() && avg > 0)
            {
                var variance = throughputHistory.Sum(v => Math.Pow(v - avg, 2)) / throughputHistory.Count;
                stdDev = Math.Sqrt(variance);
            }

            double cv = avg == 0 ? 1 : stdDev / avg;
            double predictability = avg == 0 ? 0 : Math.Max(0, 1 - cv);

            var predictabilitySignal = CreateSignal("DeliveryPredictability", "Flow", cv, predictability);
            predictabilitySignal.Metadata = new Dictionary<string, object>
            {
                { "averageThroughput", avg},
                { "standardDeviation", stdDev },
                { "coefficientOfVariation", cv },
                { "historyLength", throughputHistory.Count }
            };

            // ================================
            // RAW SIGNALS
            // ================================

            signals.Add(new Signal
            {
                Name = "ItemsAdded",
                Domain = "Flow",
                RawValue = itemsAdded,
                NormalisedValue = itemsAdded,
                Type = Signal.SignalType.Raw,
                Category = "Input",
                SourceDiagnostic = "BacklogHealth",
                Timestamp = DateTime.UtcNow
            });

            signals.Add(new Signal
            {
                Name = "ItemsCompleted",
                Domain = "Flow",
                RawValue = itemsCompleted,
                NormalisedValue = itemsCompleted,
                Type = Signal.SignalType.Raw,
                Category = "Input",
                SourceDiagnostic = "BacklogHealth",
                Timestamp = DateTime.UtcNow
            });
            // TODO: Split WorkHorizon into:
            // - BacklogHorizon (time-based)
            // - BacklogManageability (cognitive load)
            signals.Add(new Signal
            {
                Name = "WorkHorizon",
                Domain = "Flow",
                RawValue = weeksofWork,
                NormalisedValue = weeksofWork,
                Type = Signal.SignalType.Derived,
                Category = "Calculation",
                SourceDiagnostic = "BacklogHealth",
                Timestamp = DateTime.UtcNow
            });
            signals.Add(new Signal
            {
                Name = "FlowBalance",
                Domain = "Flow",
                RawValue = ratio,
                NormalisedValue = ratio,
                Type = Signal.SignalType.Derived,
                Category = "Calculation",
                SourceDiagnostic = "BacklogHealth",
                Timestamp = DateTime.UtcNow
            });
            signals.Add(new Signal
            {
                Name = "OldWorkPercentage",
                Domain = "Flow",
                RawValue = oldPercent,
                NormalisedValue = oldPercent,
                Type = Signal.SignalType.Derived,
                Category = "Calculation",
                SourceDiagnostic = "BacklogHealth",
                Timestamp = DateTime.UtcNow
            });


            return signals;
        }

        private Signal CreateSignal(string name, string domain, double rawValue, double normalisedValue)
        {
            return new Signal
            {
                Name = name,
                Domain = domain,
                RawValue = rawValue,
                NormalisedValue = Math.Max(0, Math.Min(1, normalisedValue)),
                Type = Signal.SignalType.Normalised,
                Category = "Metric",
                SourceDiagnostic = "BacklogHealth",
                Timestamp = DateTime.UtcNow,
                Confidence = 1.0
            };
        }
    }
}