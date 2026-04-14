using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using OperationalIntelligenceHub;
using Blazored.LocalStorage;
using OperationalIntelligenceHub.Services;
using OperationalIntelligenceHub.Shared;
using OperationalIntelligenceHub.Components;
using OperationalIntelligenceHub.Pages.Hub.Diagnostics;
using Microsoft.AspNetCore.Components.Forms.Mapping;
using Microsoft.JSInterop;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddSingleton(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddScoped<AssessmentRepositoryService>();
builder.Services.AddScoped<ChartService>();
builder.Services.AddScoped<ChartDefinitionService>();
builder.Services.AddScoped<CoachHubStateService>();
builder.Services.AddScoped<DeepDiveService>();
builder.Services.AddScoped<LocalStorageService>();
builder.Services.AddScoped<WorkspaceRepositoryService>();
builder.Services.AddScoped<SessionService>();
builder.Services.AddScoped<TooltipService>();
builder.Services.AddScoped<BehaviourCatalogueService>();
builder.Services.AddScoped<MaturitySignalBuilderService>();
builder.Services.AddScoped<SignalNormaliserService>();
builder.Services.AddScoped<SignalPipelineService>();
builder.Services.AddScoped<SignalMappingLoaderService>();
builder.Services.AddScoped<DerivedSignalEngine>();
builder.Services.AddSingleton<RuleRepositoryService>();
builder.Services.AddScoped<SignalRegistryService>();
builder.Services.AddScoped<BehaviourRuleEngine>();
builder.Services.AddScoped<SystemRuleEngine>();
builder.Services.AddScoped<SignalValidationService>();
builder.Services.AddScoped<OperationalInsightEngine>();
builder.Services.AddScoped<BacklogHealthSignalBuilderService>();
builder.Services.AddScoped<HealthAggregationService>();

var host = builder.Build();

try
{
	var ruleRepo = host.Services.GetRequiredService<RuleRepositoryService>();
	await ruleRepo.InitializeAsync();

	// Try to capture .NET-side unhandled exceptions and report to window.__logBlazorError
	var jsRuntime = host.Services.GetService<IJSRuntime>();

	AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
	{
		try
		{
			var msg = e?.ExceptionObject?.ToString() ?? e?.ToString() ?? "Unhandled exception (no details)";
			Console.Error.WriteLine("UnhandledException: " + msg);
			_ = Task.Run(async () =>
			{
				try { if (jsRuntime != null) await jsRuntime.InvokeVoidAsync("__logBlazorError", msg); } catch { }
			});
		}
		catch { }
	};

	TaskScheduler.UnobservedTaskException += (sender, e) =>
	{
		try
		{
			var msg = e?.Exception?.ToString() ?? "UnobservedTaskException";
			Console.Error.WriteLine("UnobservedTaskException: " + msg);
			_ = Task.Run(async () =>
			{
				try { if (jsRuntime != null) await jsRuntime.InvokeVoidAsync("__logBlazorError", msg); } catch { }
			});
		}
		catch { }
	};

	try
	{
		await host.RunAsync();
	}
	catch (Exception ex)
	{
		Console.Error.WriteLine("Unhandled exception running host: " + ex);
		try { if (jsRuntime != null) await jsRuntime.InvokeVoidAsync("__logBlazorError", ex.ToString()); } catch { }
		throw;
	}
}
catch (Exception ex)
{
	Console.Error.WriteLine("Startup exception: " + ex);
	try { var js = host.Services.GetService<IJSRuntime>(); if (js != null) await js.InvokeVoidAsync("__logBlazorError", ex.ToString()); } catch { }
	throw;
}