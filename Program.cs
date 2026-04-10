using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using OperationalIntelligenceHub;
using Blazored.LocalStorage;
using OperationalIntelligenceHub.Services;
using OperationalIntelligenceHub.Shared;
using OperationalIntelligenceHub.Components;
using OperationalIntelligenceHub.Pages.Hub.Diagnostics;
using Microsoft.AspNetCore.Components.Forms.Mapping;

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

var ruleRepo = host.Services.GetRequiredService<RuleRepositoryService>();
await ruleRepo.InitializeAsync();

await host.RunAsync();