using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OperationalIntelligenceHub.Models;

namespace OperationalIntelligenceHub.Services
{
    public class ChartService
    {
        private readonly IJSRuntime _js;
        public ChartService(IJSRuntime js) => _js = js;
    // Existing JS calls
        public async Task CreateLineChart(string canvasId, string[] labels, double[] data, string label)
        {
            try
            {
                await _js.InvokeVoidAsync("createLineChart", canvasId, labels, data, label);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"ChartService.CreateLineChart error: {ex}");
                try { _ = _js.InvokeVoidAsync("__logBlazorError", ex.ToString()); } catch { }
            }
        }

        public async Task CreateMultiLineChart(string canvasId, string[] labels, object[] datasets)
        {
            try
            {
                await _js.InvokeVoidAsync("createMultiLineChart", canvasId, labels, datasets);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"ChartService.CreateMultiLineChart error: {ex}");
                try { _ = _js.InvokeVoidAsync("__logBlazorError", ex.ToString()); } catch { }
            }
        }

        public async Task CreateRadarChart(string canvasId, string[] labels, object[] datasets)
        {
            try
            {
                await _js.InvokeVoidAsync("createRadarChart", canvasId, labels, datasets);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"ChartService.CreateRadarChart error: {ex}");
                try { _ = _js.InvokeVoidAsync("__logBlazorError", ex.ToString()); } catch { }
            }
        }

        public async Task DestroyChart(string canvasId)
        {
            try
            {
                await _js.InvokeVoidAsync("destroyChart", canvasId);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"ChartService.DestroyChart error: {ex}");
                try { _ = _js.InvokeVoidAsync("__logBlazorError", ex.ToString()); } catch { }
            }
        }
        
    // Generic dataset builder for any tool
        public object[] BuildDatasets(IEnumerable<AssessmentResult> data,
                                    Func<AssessmentResult, int?> valueSelector,
                                    Func<AssessmentResult, string> groupBySelector)
        {
            var groups = data.GroupBy(groupBySelector).OrderBy(g => g.Key);
            var squadColors = new Dictionary<string, string>();
            string GetColor(string key)
            {
                if (!squadColors.ContainsKey(key))
                {
                    var hash = key.GetHashCode();
                    var hue = Math.Abs(hash % 360);
                    var saturation = 65;
                    var lightness = 50;
                    squadColors[key] = $"hsl({hue}, {saturation}%, {lightness}%)";
                }
                return squadColors[key];
            }
            return groups.Select(g =>
            {
                var ordered = g.OrderBy(r => r.Date).ToList();
                return new
                {
                    label = g.Key,
                    data = ordered.Select(valueSelector).Select(v => v ?? 0).ToArray(),
                    bordercolor = GetColor(g.Key),
                };
                }).ToArray();
            }
            // Optional helper to render any deep dive chart dynamically
            public async Task RenderDeepDiveChart(string canvasId, IEnumerable<AssessmentResult> data,
                                        Func<AssessmentResult, int?> valueSelector,
                                        Func<AssessmentResult, string> groupBySelector)
            {
                var labels = data.OrderBy(r => r.Date).Select(r => r.Date.ToString("dd MMM")).ToArray();
                var datasets = BuildDatasets(data, valueSelector, groupBySelector);

                await CreateMultiLineChart(canvasId, labels, datasets);
        }
    }
}