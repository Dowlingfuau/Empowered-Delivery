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
            int? totalItems,
            int itemsAdded,
            int itemsCompleted,
            List<int> throughputHistory,
            int? freshItems,
            int? midAgeItems,
            int? oldItems,
            int highPriority,
            int mediumPriority,
            int lowPriority,
            int unprioritisedItems,
            int matchedVolatilityPeriodCount,
            bool prioritisationDataAvailable,
            IReadOnlyList<(string Label, int Added, int Completed)>? matchedPeriods = null
        )
        {
            var signals = new List<Signal>();
            var signalTraces = new List<SignalTrace>();

            // ================================
            // BACKLOG SIZE
            // ================================

            double avgThroughput = throughputHistory.Any() ? throughputHistory.Average() : 0;
            bool hasBacklogSizeData = avgThroughput > 0 && totalItems.HasValue;
            double weeksofWork = hasBacklogSizeData ? totalItems.GetValueOrDefault() / avgThroughput : 0;

            // Normalised score
            var (sizeNormalised, sizeBand) =
                weeksofWork <= 0 ? (0.0, "≤ 0") :
                weeksofWork < 1 ? (0.3, "(0, 1)") :
                weeksofWork < 3 ? (0.6, "[1, 3)") :
                weeksofWork <= 5 ? (1.0, "[3, 5]") :
                weeksofWork <= 8 ? (0.6, "(5, 8]") :
                weeksofWork <= 12 ? (0.3, "(8, 12]") : (0.0, "> 12");


            // CONTINUUM CALCULATION
            double centre = 5.0;
            double max = 12.0;

            double sizeMagnitude = Math.Abs(weeksofWork - centre);

            double sizeContinuumValue;
            if (!hasBacklogSizeData)
            {
                sizeContinuumValue = 0.5;
            }
            else if (weeksofWork <= centre)
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
            if (!hasBacklogSizeData)
                sizeDirection = 0;
            else if (weeksofWork < centre)
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
                { "sizeDataAvailable", hasBacklogSizeData },
                { "weeksofWork", weeksofWork},
                { "averageThroughput", avgThroughput }
            };
            if (totalItems.HasValue)
                sizeSignal.Metadata.Add("totalItems", totalItems.Value);

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
                        StepName = "AverageThroughput",
                        Description = "Mean of the throughput values entered. Each entered period counts equally; no conversion between period lengths is applied.",
                        Stage = TraceStage.Intermediate,
                        Value = avgThroughput,
                        Formula = AverageFormula(throughputHistory, avgThroughput)
                    },
                    new CalculationStep
                    {
                        StepName = "WeeksOfWork",
                        Description = hasBacklogSizeData
                            ? "Total items divided by average throughput (items per period); the result is read as weeks of work"
                            : "Needs total items and an average throughput above 0, so the result is 0",
                        Stage = TraceStage.Output,
                        Value = weeksofWork,
                        Formula = hasBacklogSizeData
                            ? FormulaTrace.Fraction("TotalItems ÷ AverageThroughput",
                                $"{totalItems.GetValueOrDefault()} backlog items",
                                $"{TraceNumber.Format(avgThroughput)} items per period",
                                weeksofWork, "weeks")
                            : null
                    },
                    new CalculationStep
                    {
                        StepName = "Normalisation",
                        Description = $"Weeks of work is looked up in a band table (0 → 0.3 → 0.6 → 1.0 for 3-5 weeks → 0.6 → 0.3 → 0), then clamped to 0-1",
                        Stage = TraceStage.Normalisation,
                        Value = sizeSignal.NormalisedValue,
                        Formula = FormulaTrace.Expr("Band(WeeksOfWork)",
                            $"{TraceNumber.Format(weeksofWork)} falls in {sizeBand}",
                            sizeSignal.NormalisedValue)
                    },
                    new CalculationStep
                    {
                        StepName = "ContinuumValue",
                        Description = !hasBacklogSizeData
                            ? "No size data, so the continuum sits at the neutral 0.5"
                            : weeksofWork <= centre
                                ? "At or left of the 5-week centre: weeks ÷ 5"
                                : "Right of the 5-week centre: 1 − (weeks − 5) ÷ 7",
                        Stage = TraceStage.Derived,
                        Value = sizeContinuumValue,
                        Formula = !hasBacklogSizeData
                            ? null
                            : weeksofWork <= centre
                                ? FormulaTrace.Fraction("WeeksOfWork ÷ Centre", TraceNumber.Format(weeksofWork), TraceNumber.Format(centre), sizeContinuumValue)
                                : FormulaTrace.Expr("1 − (WeeksOfWork − Centre) ÷ (Max − Centre)",
                                    $"1 − ({TraceNumber.Format(weeksofWork)} − {TraceNumber.Format(centre)}) ÷ ({TraceNumber.Format(max)} − {TraceNumber.Format(centre)})",
                                    sizeContinuumValue)
                    },
                    new CalculationStep
                    {
                        StepName = "Direction",
                        Description = "−1 below the 5-week centre, +1 above it, 0 at it or without data",
                        Stage = TraceStage.Derived,
                        Value = sizeDirection
                    }
                }
            });

            // ================================
            // BACKLOG AGE
            // ================================

            bool hasCompleteAgeDistribution =
                freshItems.HasValue && midAgeItems.HasValue && oldItems.HasValue;
            int freshCount = freshItems ?? 0;
            int midAgeCount = midAgeItems ?? 0;
            int oldCount = oldItems ?? 0;
            int? totalAgeItems = hasCompleteAgeDistribution
                ? freshCount + midAgeCount + oldCount
                : null;

            double freshPercent = 0;
            double midPercent = 0;
            double oldPercent = 0;
            
            if (!totalAgeItems.HasValue || totalAgeItems.Value == 0)
            {
                var neutral = CreateSignal("BacklogAge", "Flow", 0.5, 0.5);
                neutral.Shape = "Symmetric";
                neutral.ContinuumValue = 0.5;
                neutral.Metadata = new Dictionary<string, object>
                {
                    { "ageDataAvailable", false }
                };
                signals.Add(neutral);
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
                        new CalculationStep { StepName = "AgeDataAvailable", Description = "A complete, non-empty age distribution is required", Value = false },
                        new CalculationStep
                        {
                            StepName = "Normalisation",
                            Description = "Without age data the signal is set to the neutral value 0.5",
                            Stage = TraceStage.Normalisation,
                            Value = neutral.NormalisedValue
                        }
                    }
                });
            }
            else
            {
                int ageItemTotal = totalAgeItems.Value;
                freshPercent = (double)freshCount / ageItemTotal;
                midPercent = (double)midAgeCount / ageItemTotal;
                oldPercent = (double)oldCount / ageItemTotal;

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
                { "ageDataAvailable", true },
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
                    new CalculationStep { StepName = "TotalAgeItems", Description = "Fresh + mid-age + old items", Stage = TraceStage.Intermediate, Value = ageItemTotal,
                        Formula = FormulaTrace.Expr("Fresh + MidAge + Old", $"{freshCount} + {midAgeCount} + {oldCount}", ageItemTotal, "items") },
                    new CalculationStep { StepName = "FreshPercent", Description = "Fresh items / total age items", Stage = TraceStage.Intermediate, Value = freshPercent,
                        Formula = FormulaTrace.Fraction("Fresh ÷ TotalAgeItems", $"{freshCount}", $"{ageItemTotal}", freshPercent) },
                    new CalculationStep { StepName = "MidPercent", Description = "Mid-age items / total age items", Stage = TraceStage.Intermediate, Value = midPercent,
                        Formula = FormulaTrace.Fraction("MidAge ÷ TotalAgeItems", $"{midAgeCount}", $"{ageItemTotal}", midPercent) },
                    new CalculationStep { StepName = "OldPercent", Description = "Old items / total age items", Stage = TraceStage.Intermediate, Value = oldPercent,
                        Formula = FormulaTrace.Fraction("Old ÷ TotalAgeItems", $"{oldCount}", $"{ageItemTotal}", oldPercent) },
                    new CalculationStep { StepName = "DistributionDistance", Description = "Sum absolute differences between actual and ideal distribution (ideal = 0.2 / 0.6 / 0.2)", Stage = TraceStage.Intermediate, Value = distributionDistance,
                        Formula = FormulaTrace.Expr("Σ |Actual − Ideal|",
                            $"|{TraceNumber.Format(freshPercent)} − 0.2| + |{TraceNumber.Format(midPercent)} − 0.6| + |{TraceNumber.Format(oldPercent)} − 0.2|",
                            distributionDistance) },
                    new CalculationStep { StepName = "AgeScore", Description = "Converted score from distribution distance (1 = ideal, 0 = worst)", Stage = TraceStage.Output, Value = ageScore,
                        Formula = FormulaTrace.Expr("1 − DistributionDistance ÷ MaxDistance",
                            $"1 − {TraceNumber.Format(distributionDistance)} ÷ {TraceNumber.Format(maxDistance)}", ageScore) },
                    new CalculationStep { StepName = "Normalisation", Description = "The age score is already 0-1; it is clamped to 0-1 and used as the signal value", Stage = TraceStage.Normalisation, Value = ageSignal.NormalisedValue,
                        Formula = FormulaTrace.Expr("Clamp(AgeScore, 0, 1)", $"Clamp({TraceNumber.Format(ageScore)}, 0, 1)", ageSignal.NormalisedValue) },
                    new CalculationStep { StepName = "Bias", Description = "Fresh percent minus old percent", Stage = TraceStage.Derived, Value = bias,
                        Formula = FormulaTrace.Expr("FreshPercent − OldPercent", $"{TraceNumber.Format(freshPercent)} − {TraceNumber.Format(oldPercent)}", bias) },
                    new CalculationStep { StepName = "Direction", Description = "−1 when fresh work dominates, +1 when old work dominates, 0 when balanced", Stage = TraceStage.Derived, Value = ageDirection },
                    new CalculationStep { StepName = "ContinuumValue", Description = "Age score reduced by the strength of the bias", Stage = TraceStage.Derived, Value = ageContinuumValue,
                        Formula = FormulaTrace.Expr("AgeScore × (1 − |Bias|)", $"{TraceNumber.Format(ageScore)} × (1 − {TraceNumber.Format(directionalStrength)})", ageContinuumValue) },
                    new CalculationStep { StepName = "Shape", Description = "Shape classification of age distribution", Stage = TraceStage.Derived, Value = shape }
                }
            });
            }
                
            AddCalculatedSignal(
                signals,
                signalTraces,
                workspaceId,
                squadId,
                "FreshWorkPercentage",
                freshPercent,
                "Share of the backlog in the fresh (<2 weeks) age bucket.");

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
            int netChange = itemsAdded - itemsCompleted;
            int? startingBacklog = totalItems.HasValue
                ? totalItems.Value - netChange
                : null;
            double? relativeDelta = startingBacklog.HasValue && startingBacklog.Value > 0
                ? (double)netChange / startingBacklog.Value
                : null;
            double backlogGrowthWeeks = matchedVolatilityPeriodCount > 0 && avgThroughput > 0
                ? Math.Max(0, netChange / avgThroughput)
                : 0;
            double backlogGrowthPercentage = relativeDelta.HasValue
                ? Math.Max(0, relativeDelta.Value)
                : 0;
            double backlogShrinkagePercentage = relativeDelta.HasValue
                ? Math.Max(0, -relativeDelta.Value)
                : 0;

            AddCalculatedSignal(
                signals,
                signalTraces,
                workspaceId,
                squadId,
                "BacklogGrowthWeeks",
                backlogGrowthWeeks,
                "Positive net backlog growth divided by average throughput, expressed as weeks of delivery capacity.");
            AddCalculatedSignal(
                signals,
                signalTraces,
                workspaceId,
                squadId,
                "BacklogGrowthPercentage",
                backlogGrowthPercentage,
                "Positive backlog growth divided by the starting backlog; zero when the backlog is shrinking or the percentage is unavailable.");
            AddCalculatedSignal(
                signals,
                signalTraces,
                workspaceId,
                squadId,
                "BacklogShrinkagePercentage",
                backlogShrinkagePercentage,
                "Positive backlog shrinkage divided by the starting backlog; zero when the backlog is growing or the percentage is unavailable.");

            // Continuum (UX only - magnitude of change vs backlog size)
            double volatilityContinuumValue;
            if (matchedVolatilityPeriodCount == 0 || !totalItems.HasValue)
            {
                volatilityContinuumValue = 0.5;
            }
            else if (relativeDelta.HasValue)
            {
                double normalisedMagnitude = Math.Min(Math.Abs(relativeDelta.Value), 1.0);
                volatilityContinuumValue = 1 - normalisedMagnitude;
            }
            else
            {
                volatilityContinuumValue = netChange == 0 ? 1.0 : 0.0;
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
                { "volatilityDataAvailable", matchedVolatilityPeriodCount > 0 },
                { "matchedPeriodCount", matchedVolatilityPeriodCount },
                { "netChange", netChange },
                { "pattern", pattern },
                { "ratio", ratio },
                { "balanceScore", balanceRatio },
                { "itemsAdded", itemsAdded },
                { "itemsCompleted", itemsCompleted }
            };
            if (startingBacklog.HasValue)
                volatilitySignal.Metadata.Add("startingBacklog", startingBacklog.Value);
            if (relativeDelta.HasValue)
            {
                volatilitySignal.Metadata.Add("relativeDelta", relativeDelta.Value);
                volatilitySignal.Metadata.Add("percentageChange", relativeDelta.Value);
            }
            else
            {
                string unavailableReason = startingBacklog switch
                {
                    null => "Cannot calculate percentage change without the current backlog count.",
                    0 => "Percentage change is undefined from a zero starting backlog.",
                    _ => "The derived starting backlog is negative; check the inputs."
                };
                volatilitySignal.Metadata.Add("percentageChangeUnavailable", unavailableReason);
            }
            
            signals.Add(volatilitySignal);

            // Formula text for the trace, built from the same values and branches used above.
            string ratioDescription =
                itemsCompleted == 0 && itemsAdded == 0 ? "Nothing added or completed, so the ratio is the neutral 1" :
                itemsCompleted == 0 ? "Nothing completed, so growth is unbounded (∞)" :
                "Items added divided by items completed";
            FormulaTrace? ratioFormula = itemsCompleted == 0
                ? null
                : FormulaTrace.Fraction("ItemsAdded ÷ ItemsCompleted", $"{itemsAdded}", $"{itemsCompleted}", ratio);
            FormulaTrace? balanceFormula =
                itemsAdded == 0 || itemsCompleted == 0
                    ? null
                    : ratio >= 1
                        ? FormulaTrace.Fraction("ItemsCompleted ÷ ItemsAdded (ratio ≥ 1)", $"{itemsCompleted}", $"{itemsAdded}", balanceRatio)
                        : FormulaTrace.Fraction("ItemsAdded ÷ ItemsCompleted (ratio < 1)", $"{itemsAdded}", $"{itemsCompleted}", balanceRatio);
            string volatilityContinuumDescription =
                matchedVolatilityPeriodCount == 0 || !totalItems.HasValue ? "No matched periods or no backlog count, so the continuum sits at the neutral 0.5" :
                relativeDelta.HasValue ? "1 minus the size of the change relative to the starting backlog (capped at 1)" :
                "Percentage change is unavailable: 1 when there is no net change, otherwise 0";
            FormulaTrace? volatilityContinuumFormula = relativeDelta.HasValue && matchedVolatilityPeriodCount > 0 && totalItems.HasValue
                ? FormulaTrace.Expr("1 − min(|PercentageChange|, 1)",
                    $"1 − min(|{TraceNumber.Format(relativeDelta.Value)}|, 1)", volatilityContinuumValue)
                : null;

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
                    new TraceInput { Name = "TotalItems", Value = totalItems, Source = "BacklogInput" },
                    new TraceInput { Name = "StartingBacklog", Value = startingBacklog, Source = "Calculation" },
                    new TraceInput { Name = "MatchedPeriodCount", Value = matchedVolatilityPeriodCount, Source = "BacklogInput" }
                },
                CalculationSteps = new List<CalculationStep>
                {
                    new CalculationStep { StepName = "MatchedPeriods", Description = "Added and completed counts are compared only for periods with both values", Stage = TraceStage.Intermediate, Value = matchedVolatilityPeriodCount },
                    new CalculationStep { StepName = "ItemsAdded", Description = "Items added across matched periods", Stage = TraceStage.Intermediate, Value = itemsAdded,
                        Formula = SumFormula("Σ Added", matchedPeriods?.Select(p => p.Added).ToList(), itemsAdded) },
                    new CalculationStep { StepName = "ItemsCompleted", Description = "Items completed across matched periods", Stage = TraceStage.Intermediate, Value = itemsCompleted,
                        Formula = SumFormula("Σ Completed", matchedPeriods?.Select(p => p.Completed).ToList(), itemsCompleted) },
                    new CalculationStep { StepName = "Ratio", Description = ratioDescription, Stage = TraceStage.Output, Value = ratio,
                        Formula = ratioFormula },
                    new CalculationStep { StepName = "BalanceRatio", Description = "Symmetric balance score (0-1): the smaller of added and completed divided by the larger", Stage = TraceStage.Output, Value = balanceRatio,
                        Formula = balanceFormula },
                    new CalculationStep { StepName = "Normalisation", Description = "The balance ratio is already 0-1; it is clamped to 0-1 and used as the signal value. The raw value is the ratio.", Stage = TraceStage.Normalisation, Value = volatilitySignal.NormalisedValue,
                        Formula = FormulaTrace.Expr("Clamp(BalanceRatio, 0, 1)", $"Clamp({TraceNumber.Format(balanceRatio)}, 0, 1)", volatilitySignal.NormalisedValue) },
                    new CalculationStep { StepName = "NetChange", Description = "Items added minus items completed", Stage = TraceStage.Derived, Value = netChange,
                        Formula = FormulaTrace.Expr("ItemsAdded − ItemsCompleted", $"{itemsAdded} − {itemsCompleted}", netChange, "items") },
                    new CalculationStep { StepName = "StartingBacklog", Description = "Current backlog minus net change", Stage = TraceStage.Derived, Value = startingBacklog,
                        Formula = startingBacklog.HasValue
                            ? FormulaTrace.Expr("TotalItems − NetChange", $"{totalItems.GetValueOrDefault()} − ({netChange})", startingBacklog.Value, "items")
                            : null },
                    new CalculationStep { StepName = "PercentageChange", Description = "Net change divided by starting backlog", Stage = TraceStage.Derived, Value = relativeDelta,
                        Formula = relativeDelta.HasValue
                            ? FormulaTrace.Fraction("NetChange ÷ StartingBacklog", $"{netChange}", $"{startingBacklog.GetValueOrDefault()}", relativeDelta.Value)
                            : null },
                    new CalculationStep { StepName = "VolatilityContinuum", Description = volatilityContinuumDescription, Stage = TraceStage.Derived, Value = volatilityContinuumValue,
                        Formula = volatilityContinuumFormula },
                    new CalculationStep { StepName = "Direction", Description = "+1 when more was added than completed, −1 when more was completed, 0 when equal", Stage = TraceStage.Derived, Value = volatilityDirection },
                    new CalculationStep { StepName = "Pattern", Description = "Flat when total flow is below half the average throughput", Stage = TraceStage.Derived, Value = pattern,
                        Formula = avgThroughput > 0
                            ? FormulaTrace.Expr("(Added + Completed) < 0.5 × AverageThroughput",
                                $"({itemsAdded} + {itemsCompleted}) = {TraceNumber.Format(totalFlow)} vs 0.5 × {TraceNumber.Format(avgThroughput)} = {TraceNumber.Format(0.5 * avgThroughput)}",
                                isFlat ? "Flat" : "None")
                            : null }
                }
            });

            Console.WriteLine(
            $"VOL → ratio:{ratio:F2} | score:{balanceRatio:F2} | dir:{volatilityDirection} | delta:{relativeDelta:F2} | pattern:{pattern}");

            // ================================
            // PRIORITISATION
            // ================================

            int prioritisedTotal = highPriority + mediumPriority + lowPriority;
            // derive unprioritised from totalItems
            int derivedUnprioritised = Math.Max(0, (totalItems ?? 0) - prioritisedTotal);
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
            bool hasPrioritisationMetrics =
                prioritisationDataAvailable && totalItems.HasValue && totalItems.Value > 0;
            double unprioritisedWorkPercentage = hasPrioritisationMetrics
                ? unprioritisedPercent
                : 0;
            double priorityBandConcentration = hasPrioritisationMetrics
                ? Math.Max(highPercent, Math.Max(mediumPercent, lowPercent))
                : 0;

            AddCalculatedSignal(
                signals,
                signalTraces,
                workspaceId,
                squadId,
                "UnprioritisedWorkPercentage",
                unprioritisedWorkPercentage,
                "Share of the backlog with no priority, or zero when priority data is unavailable.");
            AddCalculatedSignal(
                signals,
                signalTraces,
                workspaceId,
                squadId,
                "PriorityBandConcentration",
                priorityBandConcentration,
                "Share of the backlog in its largest priority band, or zero when priority data is unavailable.");

            // Continuum Value
            double prioritisationContinuumValue =
                1 - distributionDeviation; // closer to 1 = more balanced
            prioritisationContinuumValue = Math.Max(0, Math.Min(1, prioritisationContinuumValue));
            // Direction
            int prioritisationDirection;
            if (!prioritisationDataAvailable || !totalItems.HasValue || totalItems.Value == 0)
            {
                prioritisationContinuumValue = 0.5;
                prioritisationDirection = 0;
            }
            else if (unprioritisedPercent > 0.2)
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
                { "prioritisationDataAvailable", prioritisationDataAvailable && totalItems.HasValue && totalItems.Value > 0 },
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
                    new CalculationStep { StepName = "PrioritisedTotal", Description = "High + medium + low priority items", Stage = TraceStage.Intermediate, Value = prioritisedTotal,
                        Formula = FormulaTrace.Expr("High + Medium + Low", $"{highPriority} + {mediumPriority} + {lowPriority}", prioritisedTotal, "items") },
                    new CalculationStep { StepName = "UnprioritisedItems", Description = "Total items not covered by a priority (never below 0). The unprioritised input is not used; this is derived from the backlog total.", Stage = TraceStage.Intermediate, Value = derivedUnprioritised,
                        Formula = FormulaTrace.Expr("max(0, TotalItems − PrioritisedTotal)", $"max(0, {totalItems ?? 0} − {prioritisedTotal})", derivedUnprioritised, "items") },
                    new CalculationStep { StepName = "TotalPriorityItems", Description = "Prioritised plus unprioritised items", Stage = TraceStage.Intermediate, Value = totalPriorityItems,
                        Formula = FormulaTrace.Expr("PrioritisedTotal + Unprioritised", $"{prioritisedTotal} + {derivedUnprioritised}", totalPriorityItems, "items") },
                    new CalculationStep { StepName = "HighPercent", Description = "High priority percentage", Stage = TraceStage.Intermediate, Value = highPercent,
                        Formula = FormulaTrace.Fraction("High ÷ TotalPriorityItems", $"{highPriority}", $"{totalPriorityItems}", highPercent) },
                    new CalculationStep { StepName = "MediumPercent", Description = "Medium priority percentage", Stage = TraceStage.Intermediate, Value = mediumPercent,
                        Formula = FormulaTrace.Fraction("Medium ÷ TotalPriorityItems", $"{mediumPriority}", $"{totalPriorityItems}", mediumPercent) },
                    new CalculationStep { StepName = "LowPercent", Description = "Low priority percentage", Stage = TraceStage.Intermediate, Value = lowPercent,
                        Formula = FormulaTrace.Fraction("Low ÷ TotalPriorityItems", $"{lowPriority}", $"{totalPriorityItems}", lowPercent) },
                    new CalculationStep { StepName = "UnprioritisedPercent", Description = "Unprioritised percentage", Stage = TraceStage.Intermediate, Value = unprioritisedPercent,
                        Formula = FormulaTrace.Fraction("Unprioritised ÷ TotalPriorityItems", $"{derivedUnprioritised}", $"{totalPriorityItems}", unprioritisedPercent) },
                    new CalculationStep { StepName = "Coverage", Description = "Coverage (1 - unprioritised percent)", Stage = TraceStage.Output, Value = coverage,
                        Formula = FormulaTrace.Expr("1 − UnprioritisedPercent", $"1 − {TraceNumber.Format(unprioritisedPercent)}", coverage) },
                    new CalculationStep { StepName = "DistributionDeviation", Description = "Distance of each priority share from an even third", Stage = TraceStage.Intermediate, Value = distributionDeviation,
                        Formula = FormulaTrace.Expr("Σ |Share − 1/3|",
                            $"|{TraceNumber.Format(highPercent)} − 0.3333| + |{TraceNumber.Format(mediumPercent)} − 0.3333| + |{TraceNumber.Format(lowPercent)} − 0.3333|",
                            distributionDeviation) },
                    new CalculationStep { StepName = "DistributionScore", Description = "Balance score for distribution, clamped to 0-1", Stage = TraceStage.Output, Value = distributionScore,
                        Formula = FormulaTrace.Expr("1 − DistributionDeviation ÷ MaxDeviation",
                            $"1 − {TraceNumber.Format(distributionDeviation)} ÷ {TraceNumber.Format(maxDeviation)}", distributionScore) },
                    new CalculationStep { StepName = "PrioritisationScore", Description = "Combined prioritisation score: half coverage, half distribution", Stage = TraceStage.Output, Value = prioritisationScore,
                        Formula = FormulaTrace.Expr("Coverage × 0.5 + DistributionScore × 0.5",
                            $"{TraceNumber.Format(coverage)} × 0.5 + {TraceNumber.Format(distributionScore)} × 0.5", prioritisationScore) },
                    new CalculationStep { StepName = "Normalisation", Description = "The combined score is already 0-1; it is clamped to 0-1 and used as the signal value. The raw value is the coverage.", Stage = TraceStage.Normalisation, Value = prioritisationSignal.NormalisedValue,
                        Formula = FormulaTrace.Expr("Clamp(PrioritisationScore, 0, 1)", $"Clamp({TraceNumber.Format(prioritisationScore)}, 0, 1)", prioritisationSignal.NormalisedValue) },
                    new CalculationStep { StepName = "ContinuumValue", Description = "1 − deviation, clamped to 0-1; 0.5 when there is no prioritisation data", Stage = TraceStage.Derived, Value = prioritisationContinuumValue },
                    new CalculationStep { StepName = "Direction", Description = "Direction indicator (-1,0,1): −1 when more than 20% is unprioritised, +1 when the deviation exceeds 0.2", Stage = TraceStage.Derived, Value = prioritisationDirection }
                }
            });

            // ================================
            // DELIVERY PREDICTABILITY
            // ================================

            double avg = avgThroughput;
            double stdDev = 0;
            double variance = 0;

            if (throughputHistory.Count >= 2 && avg > 0)
            {
                variance = throughputHistory.Sum(v => Math.Pow(v - avg, 2)) / throughputHistory.Count;
                stdDev = Math.Sqrt(variance);
            }

            double cv = avg == 0 ? 1 : stdDev / avg;
            bool hasPredictabilityData = throughputHistory.Count >= 2 && avg > 0;
            double predictability = !hasPredictabilityData ? 0.5 : Math.Max(0, 1 - cv);

            var predictabilitySignal = CreateSignal("DeliveryPredictability", "Flow", cv, predictability);
            predictabilitySignal.Metadata = new Dictionary<string, object>
            {
                { "predictabilityDataAvailable", hasPredictabilityData },
                { "averageThroughput", avg},
                { "standardDeviation", stdDev },
                { "coefficientOfVariation", cv },
                { "historyLength", throughputHistory.Count }
            };

            signals.Add(predictabilitySignal);
            AddCalculatedSignal(
                signals,
                signalTraces,
                workspaceId,
                squadId,
                "DeliveryPredictabilityDataAvailable",
                hasPredictabilityData ? 1 : 0,
                "1 when at least two throughput observations are available; otherwise 0.");

            // Build trace for Predictability
            var predictabilityCalculationSteps = new List<CalculationStep>();
            if (throughputHistory.Count >= 2 && avg > 0)
            {
                predictabilityCalculationSteps.Add(new CalculationStep
                {
                    StepName = "AverageThroughput",
                    Description = "Average of throughput history",
                    Stage = TraceStage.Intermediate,
                    Value = avg,
                    Formula = AverageFormula(throughputHistory, avg)
                });
                predictabilityCalculationSteps.Add(new CalculationStep
                {
                    StepName = "Variance",
                    Description = "Variance of throughput values (population variance: mean of squared differences from the average)",
                    Stage = TraceStage.Intermediate,
                    Value = variance,
                    Formula = FormulaTrace.Fraction("Σ (x − Average)² ÷ n",
                        string.Join(" + ", throughputHistory.Select(v => $"({v} − {TraceNumber.Format(avg)})²")),
                        $"{throughputHistory.Count}", variance)
                });
                predictabilityCalculationSteps.Add(new CalculationStep
                {
                    StepName = "StandardDeviation",
                    Description = "Square root of the variance",
                    Stage = TraceStage.Intermediate,
                    Value = stdDev,
                    Formula = FormulaTrace.Expr("√Variance", $"√{TraceNumber.Format(variance)}", stdDev)
                });
                predictabilityCalculationSteps.Add(new CalculationStep
                {
                    StepName = "CoefficientOfVariation",
                    Description = "Standard deviation divided by mean. This is the raw value of the signal.",
                    Stage = TraceStage.Output,
                    Value = cv,
                    Formula = FormulaTrace.Fraction("StandardDeviation ÷ Average", TraceNumber.Format(stdDev), TraceNumber.Format(avg), cv)
                });
                predictabilityCalculationSteps.Add(new CalculationStep
                {
                    StepName = "Normalisation",
                    Description = "Predictability is 1 minus the coefficient of variation, floored at 0, then clamped to 0-1",
                    Stage = TraceStage.Normalisation,
                    Value = predictabilitySignal.NormalisedValue,
                    Formula = FormulaTrace.Expr("max(0, 1 − CoefficientOfVariation)", $"max(0, 1 − {TraceNumber.Format(cv)})", predictabilitySignal.NormalisedValue)
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
                predictabilityCalculationSteps.Add(new CalculationStep
                {
                    StepName = "Normalisation",
                    Description = "Needs at least two throughput values and an average above 0; otherwise the signal is the neutral 0.5",
                    Stage = TraceStage.Normalisation,
                    Value = predictabilitySignal.NormalisedValue
                });
            }

            signalTraces.Add(new SignalTrace
            {
                SignalName = "DeliveryPredictability",
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
                    new CalculationStep { StepName = "RawValue", Description = "Sum of the added counts for matched periods", Stage = TraceStage.Output, Value = itemsAdded,
                        Formula = SumFormula("Σ Added", matchedPeriods?.Select(p => p.Added).ToList(), itemsAdded) },
                    PassThrough(itemsAdded)
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
                    new CalculationStep { StepName = "RawValue", Description = "Sum of the completed counts for matched periods", Stage = TraceStage.Output, Value = itemsCompleted,
                        Formula = SumFormula("Σ Completed", matchedPeriods?.Select(p => p.Completed).ToList(), itemsCompleted) },
                    PassThrough(itemsCompleted)
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
                    new CalculationStep
                    {
                        StepName = "AverageThroughput",
                        Description = "Mean of the throughput values entered. Each entered period counts equally; no conversion between period lengths is applied.",
                        Stage = TraceStage.Intermediate,
                        Value = avgThroughput,
                        Formula = AverageFormula(throughputHistory, avgThroughput)
                    },
                    new CalculationStep
                    {
                        StepName = "WeeksOfWork",
                        Description = hasBacklogSizeData
                            ? "Total items divided by average throughput. The rule engine reads this value as WorkHorizon."
                            : "Needs total items and an average throughput above 0, so the result is 0",
                        Stage = TraceStage.Output,
                        Value = weeksofWork,
                        Formula = hasBacklogSizeData
                            ? FormulaTrace.Fraction("TotalItems ÷ AverageThroughput",
                                $"{totalItems.GetValueOrDefault()} backlog items",
                                $"{TraceNumber.Format(avgThroughput)} items per period",
                                weeksofWork, "weeks")
                            : null
                    },
                    PassThrough(weeksofWork)
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
                    new CalculationStep { StepName = "Ratio", Description = ratioDescription, Stage = TraceStage.Output, Value = ratio, Formula = ratioFormula },
                    new CalculationStep { StepName = "BalanceScore", Description = "Balance score derived from ratio", Stage = TraceStage.Intermediate, Value = balanceRatio, Formula = balanceFormula },
                    PassThrough(ratio)
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
                    new CalculationStep { StepName = "OldPercent", Description = "Old items / total age items", Stage = TraceStage.Output, Value = oldPercent,
                        Formula = totalAgeItems.HasValue && totalAgeItems.Value > 0
                            ? FormulaTrace.Fraction("Old ÷ TotalAgeItems", $"{oldCount}", $"{totalAgeItems.Value}", oldPercent)
                            : null },
                    PassThrough(oldPercent)
                }
            });


            return (signals, signalTraces);
        }

        private static FormulaTrace AverageFormula(IReadOnlyList<int> values, double average) =>
            values.Count == 0
                ? FormulaTrace.Expr("Σ x ÷ n", "no throughput values entered", 0)
                : FormulaTrace.Fraction("Σ x ÷ n", string.Join(" + ", values), $"{values.Count}", average, "items per period");

        private static FormulaTrace? SumFormula(string symbolic, IReadOnlyList<int>? values, int total) =>
            values == null
                ? null
                : FormulaTrace.Expr(symbolic, values.Count == 0 ? "0" : string.Join(" + ", values), total);

        private static void AddCalculatedSignal(
            List<Signal> signals,
            List<SignalTrace> signalTraces,
            Guid workspaceId,
            Guid squadId,
            string name,
            double value,
            string description)
        {
            signals.Add(new Signal
            {
                Name = name,
                Domain = "Flow",
                RawValue = value,
                NormalisedValue = value,
                WorkspaceId = workspaceId,
                SquadId = squadId,
                SourceDiagnostic = "BacklogHealth",
                Timestamp = DateTime.UtcNow,
                Category = "Calculation",
                Type = Signal.SignalType.Derived
            });

            signalTraces.Add(new SignalTrace
            {
                SignalName = name,
                WorkspaceId = workspaceId,
                SquadId = squadId,
                Inputs = new List<TraceInput>
                {
                    new TraceInput { Name = name, Value = value, Source = "Calculation" }
                },
                CalculationSteps = new List<CalculationStep>
                {
                    new CalculationStep { StepName = "Value", Description = description, Stage = TraceStage.Output, Value = value },
                    PassThrough(value)
                }
            });
        }

        // Signals created without scaling or clamping carry their raw value straight through.
        private static CalculationStep PassThrough(double value) => new()
        {
            StepName = "Normalisation",
            Description = "Used unchanged: no scaling or clamping is applied to this signal",
            Stage = TraceStage.Normalisation,
            Value = value
        };

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

        // Delivery rhythm classification, moved verbatim from the Backlog Health page so the page consumes the result.
        public string ClassifyDeliveryRhythm(IReadOnlyList<int>? throughputHistory)
        {
            if (throughputHistory == null || throughputHistory.Count < 3)
                return "Insufficient data";

            var diffs = throughputHistory
                .Zip(throughputHistory.Skip(1), (a, b) => b - a)
                .ToList();

            if (diffs.Count < 2)
                return "Insufficient data";

            // --- Step 1: detect alternating pattern
            bool alternating = diffs.Zip(diffs.Skip(1), (a, b) => a * b < 0).All(x => x);

            // --- Step 2: measure magnitude
            double avg = throughputHistory.Average();
            double avgDiff = diffs.Average(d => Math.Abs(d));

            // magnitude threshold relative to system size
            bool significantSwing = avg > 0 && (avgDiff / avg) > 0.4;

            if (alternating && significantSwing)
                return "Cyclical";

            // fallback to CV-based classification
            double std = Math.Sqrt(throughputHistory.Sum(x => Math.Pow(x - avg, 2)) / throughputHistory.Count);
            double cv = avg == 0 ? 0 : std / avg;

            if (cv <= 0.2)
                return "Stable";

            return "Chaotic";
        }

    }
}
