using System.Data.Common;
using OperationalIntelligenceHub.Models;

namespace OperationalIntelligenceHub.Services
{
    public class BacklogHealthSignalBuilderService
    {
        // TODO: Add metadata
        public (List<Signal> Signals, List<SignalTrace> SignalTraces) BuildSignals(
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
            var signalTraces = new List<SignalTrace>();

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
            signalTraces.Add(new SignalTrace
            {
                SignalName = "BacklogSize",
                WorkspaceId = workspaceId,
                SquadId = squadId,
                Inputs = new List<TraceInput>
                {
                    new TraceInput
                    {
                        Name = "TotalItems",
                        Value = totalItems,
                        Source = "BacklogInput"
                    },
                    new TraceInput
                    {
                        Name = "ThroughputHistory",
                        Value = throughputHistory.ToList(),
                        Source = "ThroughputHistory"
                    },
                    new TraceInput
                    {
                        Name = "AverageThroughput",
                        Value = avgThroughput,
                        Source = "ThroughputHistory"
                    }
                },
                CalculationSteps = new List<CalculationStep>
                {
                    new CalculationStep
                    {
                        StepName = "WeeksOfWork",
                        Description = "TotalItems divided by throughput",
                        Value = weeksofWork
                    }
                }
            });

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
            // Trace for BacklogAge
            signalTraces.Add(new SignalTrace
            {
                SignalName = "BacklogAge",
                WorkspaceId = workspaceId,
                SquadId = squadId,
                Inputs = new List<TraceInput>
                {
                    new TraceInput { Name = "FreshItems", Value = freshItems, Source = "BacklogInput" },
                    new TraceInput { Name = "MidAgeItems", Value = midAgeItems, Source = "BacklogInput" },
                    new TraceInput { Name = "OldItems", Value = oldItems, Source = "BacklogInput" },
                    new TraceInput { Name = "TotalAgeItems", Value = totalAgeItems, Source = "Calculation" }
                },
                CalculationSteps = new List<CalculationStep>
                {
                    new CalculationStep { StepName = "FreshPercent", Description = "Fresh items / total age items", Value = freshPercent },
                    new CalculationStep { StepName = "MidPercent", Description = "Mid-age items / total age items", Value = midPercent },
                    new CalculationStep { StepName = "OldPercent", Description = "Old items / total age items", Value = oldPercent },
                    new CalculationStep { StepName = "DistributionDistance", Description = "Sum absolute differences between actual and ideal distribution", Value = distributionDistance },
                    new CalculationStep { StepName = "AgeScore", Description = "Converted score from distribution distance", Value = ageScore },
                    new CalculationStep { StepName = "Bias", Description = "Fresh percent minus old percent", Value = bias },
                    new CalculationStep { StepName = "Shape", Description = "Shape classification of age distribution", Value = shape }
                }
            });
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

            // Trace for BacklogVolatility
            signalTraces.Add(new SignalTrace
            {
                SignalName = "BacklogVolatility",
                WorkspaceId = workspaceId,
                SquadId = squadId,
                Inputs = new List<TraceInput>
                {
                    new TraceInput { Name = "ItemsAdded", Value = itemsAdded, Source = "BacklogInput" },
                    new TraceInput { Name = "ItemsCompleted", Value = itemsCompleted, Source = "BacklogInput" },
                    new TraceInput { Name = "TotalItems", Value = totalItems, Source = "BacklogInput" }
                },
                CalculationSteps = new List<CalculationStep>
                {
                    new CalculationStep { StepName = "Ratio", Description = "Items added divided by items completed", Value = ratio },
                    new CalculationStep { StepName = "BalanceRatio", Description = "Symmetric balance score (0-1)", Value = balanceRatio },
                    new CalculationStep { StepName = "RelativeDelta", Description = "Relative change scaled by backlog size", Value = relativeDelta },
                    new CalculationStep { StepName = "VolatilityContinuum", Description = "Continuum value for volatility UX", Value = volatilityContinuumValue },
                    new CalculationStep { StepName = "Pattern", Description = "Detected pattern", Value = pattern }
                }
            });

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

            // Trace for BacklogPrioritisation
            signalTraces.Add(new SignalTrace
            {
                SignalName = "BacklogPrioritisation",
                WorkspaceId = workspaceId,
                SquadId = squadId,
                Inputs = new List<TraceInput>
                {
                    new TraceInput { Name = "HighPriority", Value = highPriority, Source = "BacklogInput" },
                    new TraceInput { Name = "MediumPriority", Value = mediumPriority, Source = "BacklogInput" },
                    new TraceInput { Name = "LowPriority", Value = lowPriority, Source = "BacklogInput" },
                    new TraceInput { Name = "TotalItems", Value = totalItems, Source = "BacklogInput" }
                },
                CalculationSteps = new List<CalculationStep>
                {
                    new CalculationStep { StepName = "HighPercent", Description = "High priority percentage", Value = highPercent },
                    new CalculationStep { StepName = "MediumPercent", Description = "Medium priority percentage", Value = mediumPercent },
                    new CalculationStep { StepName = "LowPercent", Description = "Low priority percentage", Value = lowPercent },
                    new CalculationStep { StepName = "UnprioritisedPercent", Description = "Unprioritised percentage", Value = unprioritisedPercent },
                    new CalculationStep { StepName = "Coverage", Description = "Coverage (1 - unprioritised percent)", Value = coverage },
                    new CalculationStep { StepName = "DistributionScore", Description = "Balance score for distribution", Value = distributionScore },
                    new CalculationStep { StepName = "PrioritisationScore", Description = "Combined prioritisation score", Value = prioritisationScore },
                    new CalculationStep { StepName = "Direction", Description = "Direction indicator (-1,0,1)", Value = prioritisationDirection }
                }
            });

            // ================================
            // DELIVERY PREDICTABILITY
            // ================================

            double avg = avgThroughput;
            double stdDev = 0;
            double variance = 0;

            if (throughputHistory.Any() && avg > 0)
            {
                variance = throughputHistory.Sum(v => Math.Pow(v - avg, 2)) / throughputHistory.Count;
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

            signals.Add(predictabilitySignal);

            // Build trace for Predictability
            var predictabilityCalculationSteps = new List<CalculationStep>();
            if (throughputHistory.Any() && avg > 0)
            {
                predictabilityCalculationSteps.Add(new CalculationStep
                {
                    StepName = "AverageThroughput",
                    Description = "Average of throughput history",
                    Value = avg
                });
                predictabilityCalculationSteps.Add(new CalculationStep
                {
                    StepName = "Variance",
                    Description = "Variance of throughput values",
                    Value = variance
                });
                predictabilityCalculationSteps.Add(new CalculationStep
                {
                    StepName = "CoefficientOfVariation",
                    Description = "Standard deviation divided by mean",
                    Value = cv
                });
            }
            else
            {
                predictabilityCalculationSteps.Add(new CalculationStep
                {
                    StepName = "InsufficientData",
                    Description = "Insufficient data for predictability calculation",
                    Value = null
                });
            }

            signalTraces.Add(new SignalTrace
            {
                SignalName = "Predictability",
                WorkspaceId = workspaceId,
                SquadId = squadId,
                Inputs = new List<TraceInput>
                {
                    new TraceInput
                    {
                        Name = "ThroughputHistory",
                        Value = throughputHistory.ToList(),
                        Source = "ThroughputHistory"
                    },
                    new TraceInput
                    {
                        Name = "AverageThroughput",
                        Value = avg,
                        Source = "ThroughputHistory"
                    },
                    new TraceInput
                    {
                        Name = "Variance",
                        Value = variance,
                        Source = "Calculation"
                    },
                    new TraceInput
                    {
                        Name = "StandardDeviation",
                        Value = stdDev,
                        Source = "Calculation"
                    },
                    new TraceInput
                    {
                        Name = "CoefficientOfVariation",
                        Value = cv,
                        Source = "Calculation"
                    }
                },
                CalculationSteps = predictabilityCalculationSteps
            });

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
            signalTraces.Add(new SignalTrace
            {
                SignalName = "ItemsAdded",
                WorkspaceId = workspaceId,
                SquadId = squadId,
                Inputs = new List<TraceInput>
                {
                    new TraceInput { Name = "ItemsAdded", Value = itemsAdded, Source = "BacklogInput" }
                },
                CalculationSteps = new List<CalculationStep>
                {
                    new CalculationStep { StepName = "RawValue", Description = "Raw input value", Value = itemsAdded }
                }
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
            signalTraces.Add(new SignalTrace
            {
                SignalName = "ItemsCompleted",
                WorkspaceId = workspaceId,
                SquadId = squadId,
                Inputs = new List<TraceInput>
                {
                    new TraceInput { Name = "ItemsCompleted", Value = itemsCompleted, Source = "BacklogInput" }
                },
                CalculationSteps = new List<CalculationStep>
                {
                    new CalculationStep { StepName = "RawValue", Description = "Raw input value", Value = itemsCompleted }
                }
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
            signalTraces.Add(new SignalTrace
            {
                SignalName = "WorkHorizon",
                WorkspaceId = workspaceId,
                SquadId = squadId,
                Inputs = new List<TraceInput>
                {
                    new TraceInput { Name = "TotalItems", Value = totalItems, Source = "BacklogInput" },
                    new TraceInput { Name = "ThroughputHistory", Value = throughputHistory.ToList(), Source = "ThroughputHistory" },
                    new TraceInput { Name = "AverageThroughput", Value = avgThroughput, Source = "ThroughputHistory" }
                },
                CalculationSteps = new List<CalculationStep>
                {
                    new CalculationStep { StepName = "WeeksOfWork", Description = "Total items divided by average throughput", Value = weeksofWork }
                }
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
            signalTraces.Add(new SignalTrace
            {
                SignalName = "FlowBalance",
                WorkspaceId = workspaceId,
                SquadId = squadId,
                Inputs = new List<TraceInput>
                {
                    new TraceInput { Name = "ItemsAdded", Value = itemsAdded, Source = "BacklogInput" },
                    new TraceInput { Name = "ItemsCompleted", Value = itemsCompleted, Source = "BacklogInput" }
                },
                CalculationSteps = new List<CalculationStep>
                {
                    new CalculationStep { StepName = "Ratio", Description = "Items added / items completed", Value = ratio },
                    new CalculationStep { StepName = "BalanceScore", Description = "Balance score derived from ratio", Value = balanceRatio }
                }
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
            signalTraces.Add(new SignalTrace
            {
                SignalName = "OldWorkPercentage",
                WorkspaceId = workspaceId,
                SquadId = squadId,
                Inputs = new List<TraceInput>
                {
                    new TraceInput { Name = "OldItems", Value = oldItems, Source = "BacklogInput" },
                    new TraceInput { Name = "TotalAgeItems", Value = totalAgeItems, Source = "Calculation" }
                },
                CalculationSteps = new List<CalculationStep>
                {
                    new CalculationStep { StepName = "OldPercent", Description = "Old items / total age items", Value = oldPercent }
                }
            });


            return (signals, signalTraces);
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
