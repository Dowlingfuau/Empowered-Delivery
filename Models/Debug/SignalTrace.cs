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

	public class CalculationStep
	{
		public string StepName { get; set; } = "";
		public string? Description { get; set; }
		public object? Value { get; set; }
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