using System;
using System.Collections.Generic;

namespace OperationalIntelligenceHub.Models
{
	public class TraceInput
	{
		public string Name { get; set; } = "";
		public object? Value { get; set; }
		public string? Source { get; set; }
		public DateTime? Timestamp { get; set; }
	}

	// Where a step sits in the chain from user input to rule evaluation.
	public enum TraceStage
	{
		Intermediate,
		Output,
		Normalisation,
		Derived
	}

	// The formula a step actually used, with the entered values substituted.
	// Symbolic uses names (TotalItems / AverageThroughput); Numerator/Denominator or Expression hold the substituted values.
	public class FormulaTrace
	{
		public string? Symbolic { get; set; }
		public string? Numerator { get; set; }
		public string? Denominator { get; set; }
		public string? Expression { get; set; }
		public string Result { get; set; } = "";
		public string? Unit { get; set; }

		public static FormulaTrace Fraction(string symbolic, string numerator, string denominator, double result, string? unit = null) => new()
		{
			Symbolic = symbolic,
			Numerator = numerator,
			Denominator = denominator,
			Result = TraceNumber.Format(result),
			Unit = unit
		};

		public static FormulaTrace Expr(string symbolic, string expression, string result, string? unit = null) => new()
		{
			Symbolic = symbolic,
			Expression = expression,
			Result = result,
			Unit = unit
		};

		public static FormulaTrace Expr(string symbolic, string expression, double result, string? unit = null) =>
			Expr(symbolic, expression, TraceNumber.Format(result), unit);
	}

	public static class TraceNumber
	{
		// Invariant, trimmed formatting so substituted values read like the numbers a user typed.
		public static string Format(double value)
		{
			if (double.IsPositiveInfinity(value) || value >= double.MaxValue) return "∞";
			if (double.IsNegativeInfinity(value) || value <= double.MinValue) return "-∞";
			if (double.IsNaN(value)) return "undefined";
			return Math.Round(value, 4).ToString("0.####", System.Globalization.CultureInfo.InvariantCulture);
		}

		public static string Format(double? value) => value.HasValue ? Format(value.Value) : "n/a";

		public static string Join(string separator, IEnumerable<double> values) =>
			string.Join(separator, values.Select(Format));
	}

	public class CalculationStep
	{
		public string StepName { get; set; } = "";
		public string? Description { get; set; }
		public object? Value { get; set; }
		public TraceStage Stage { get; set; } = TraceStage.Intermediate;
		public FormulaTrace? Formula { get; set; }
	}

	public class PatternDetection
	{
		public string Name { get; set; } = "";
		public string? Description { get; set; }
		public double? Confidence { get; set; }
	}

	public class SignalTrace
	{
		public string SignalName { get; set; } = "";
		public Guid? WorkspaceId { get; set; }
		public Guid? SquadId { get; set; }
		public DateTime Timestamp { get; set; } = DateTime.UtcNow;

		public List<TraceInput> Inputs { get; set; } = new();
		public List<CalculationStep> CalculationSteps { get; set; } = new();
		public List<RuleEvaluation> RuleEvaluations { get; set; } = new();
		public List<PatternDetection> PatternDetections { get; set; } = new();

		public string? Notes { get; set; }
	}

	public class CalculationTrace
	{
		public string CalculationName { get; set; } = "";
		public List<CalculationStep> Steps { get; set; } = new();
		public List<PatternDetection> PatternDetections { get; set; } = new();
	}
}