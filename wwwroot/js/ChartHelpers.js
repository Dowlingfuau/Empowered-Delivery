window.charts = window.charts || {};

function getCssVariable(name) {
    return getComputedStyle(document.documentElement)
        .getPropertyValue(name)
        .trim();
}
function destroyChart(id) {
    if (window.charts && window.charts[id]) {
        window.charts[id].destroy();
        delete window.charts[id];
    }
}
window.clearChart = (canvasId) => {
    destroyChart(canvasId);
};
function getChartOptions() {
    return {
        responsive: true,
        maintainAspectRatio: false,
        interaction: {
            mode: 'nearest',
            intersect: false
        },
        scales: {
            y: {
                min: 0,
                max: 100,
                grid: {
                    color: getCssVariable('--color-border-light')
                },
                ticks: {
                    color: getCssVariable('--text-secondary')
                }
            },
            x: {
                grid: { display: false },
                ticks: {
                    color: getCssVariable('--text-secondary')
                }
            }
        }
    };
}
/* Single Line Charts */
window.createLineChart = (canvasId, labels, data, label) => {
    destroyChart(canvasId);
    const el = document.getElementById(canvasId);
    if (!el) {
        console.warn("Chart canvas not found:", canvasId);
        return;
    }
    if (typeof Chart === 'undefined') {
        console.warn('Chart.js not loaded; skipping chart:', canvasId);
        return;
    }
    try {
        window.charts[canvasId] = new Chart(el, {
            type: 'line',
            data: {
                labels: labels,
                datasets: [{
                    label: label,
                    data: data,
                    borderColor: getCssVariable('--chart-1'),
                    borderWidth: 2,
                    tension: 0.3,
                    fill: false
                }]
            },
            options: getChartOptions()
        });
    } catch (e) {
        console.error('Failed to create line chart', e);
    }
};
/* Multi Line Charts */
window.createMultiLineChart = (canvasId, labels, datasets) => {
    destroyChart(canvasId);
    const el = document.getElementById(canvasId);
    if (!el) {
        console.warn("Chart canvas not found:", canvasId);
        return;
    }
    if (typeof Chart === 'undefined') {
        console.warn('Chart.js not loaded; skipping chart:', canvasId);
        return;
    }
    try {
        const formattedDatasets = (datasets || []).map((ds, index) => ({
            label: ds && ds.label ? ds.label : `series ${index+1}`,
            data: ds && ds.data ? ds.data : [],
            borderColor: getCssVariable(`--chart-${index+1}`),
            borderWidth: 2,
            tension: 0.3,
            fill: false
        }));
        window.charts[canvasId] = new Chart(el, {
            type: 'line',
            data: {
                labels: labels,
                datasets: formattedDatasets
            },
            options: {
                ...getChartOptions(),
                plugins: {
                    legend: {
                        display: true,
                        position: 'top'
                    }
                   
                }
            }
        });
    } catch (e) {
        console.error('Failed to create multi-line chart', e);
    }
};
/* Radar Charts */
window.createRadarChart = (canvasId, labels, datasets) => {

    destroyChart(canvasId);

    const el = document.getElementById(canvasId);

    if (!el) {
        console.warn("Radar canvas not found:", canvasId);
        return;
    }
    if (typeof Chart === 'undefined') {
        console.warn('Chart.js not loaded; skipping radar chart:', canvasId);
        return;
    }

    try {
        const formattedDatasets = (datasets || []).map((ds, index) => ({
            label: ds && ds.label ? ds.label : `series ${index+1}`,
            data: ds && ds.data ? ds.data : [],
            borderColor: getCssVariable(`--chart-${index+1}`),
            backgroundColor: getCssVariable(`--chart-${index+1}`) + "33",
            borderWidth: 2,
            pointRadius: 3,
            fill: true
        }));

        window.charts[canvasId] = new Chart(el, {
            type: 'radar',
            data: {
                labels: labels,
                datasets: formattedDatasets
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        display: true,
                        position: 'top'
                    }
                },
                scales: {
                    r: {
                        min: 0,
                        max: 5,
                        ticks: {
                            stepSize: 1,
                            color: getCssVariable('--text-secondary')
                        },
                        grid: {
                            color: getCssVariable('--color-border-light')
                        },
                        angleLines: {
                            color: getCssVariable('--color-border-light')
                        }
                    }
                }
            }
        });
    } catch (e) {
        console.error('Failed to create radar chart', e);
    }
};
