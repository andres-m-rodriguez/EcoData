// Chart Service - thin wrapper around ApexCharts. Manages instances by element ID.
window.chartService = {
    instances: new Map(),

    _toMs: function (isoOrDate) {
        if (typeof isoOrDate === 'number') return isoOrDate;
        const t = Date.parse(isoOrDate);
        return Number.isFinite(t) ? t : 0;
    },

    _toApexTimeSeries: function (series) {
        return (series || []).map(s => ({
            name: s.name,
            data: (s.points || []).map(p => ({
                x: window.chartService._toMs(p.at),
                y: p.value
            }))
        }));
    },

    _toApexBarSeries: function (series) {
        return (series || []).map(s => ({
            name: s.name,
            data: s.values || []
        }));
    },

    _formatString: function (fmt) {
        // Lightweight format support: pass-through for ApexCharts default if null,
        // else build a function. Currently we only use it for tooltip y-formatting.
        return fmt || null;
    },

    _baseOptions: function (height, colors) {
        const opts = {
            chart: {
                height: height || 320,
                toolbar: { show: false },
                animations: { enabled: true, speed: 300 },
                fontFamily: 'Inter, system-ui, sans-serif',
                background: 'transparent'
            },
            grid: {
                borderColor: 'rgba(15, 23, 42, 0.08)',
                strokeDashArray: 3
            },
            tooltip: {
                theme: 'light',
                x: { show: true }
            },
            legend: {
                position: 'bottom',
                fontSize: '12px',
                markers: { size: 6 }
            }
        };
        if (colors && colors.length) opts.colors = colors;
        return opts;
    },

    _yaxis: function (yAxisTitle) {
        const y = { labels: { style: { fontSize: '11px' } } };
        if (yAxisTitle) {
            y.title = { text: yAxisTitle, style: { fontSize: '12px', fontWeight: 500 } };
        }
        return y;
    },

    createTimeSeries: function (elementId, config) {
        if (this.instances.has(elementId)) this.dispose(elementId);

        const el = document.getElementById(elementId);
        if (!el || typeof ApexCharts === 'undefined') return;

        const base = this._baseOptions(config.height, config.colors);

        const options = {
            ...base,
            chart: { ...base.chart, type: 'area' },
            series: this._toApexTimeSeries(config.series),
            stroke: {
                curve: config.smooth === false ? 'straight' : 'smooth',
                width: 2
            },
            fill: config.area
                ? { type: 'gradient', gradient: { shadeIntensity: 1, opacityFrom: 0.35, opacityTo: 0, stops: [0, 90, 100] } }
                : { type: 'solid', opacity: 0 },
            dataLabels: { enabled: false },
            xaxis: {
                type: 'datetime',
                labels: { datetimeUTC: false, style: { fontSize: '11px' } },
                axisBorder: { show: false },
                axisTicks: { show: false }
            },
            yaxis: this._yaxis(config.yAxisTitle),
            tooltip: {
                ...base.tooltip,
                x: { format: 'MMM dd, HH:mm' }
            }
        };

        const chart = new ApexCharts(el, options);
        chart.render();
        this.instances.set(elementId, { chart: chart, kind: 'timeSeries' });
    },

    createBar: function (elementId, config) {
        if (this.instances.has(elementId)) this.dispose(elementId);

        const el = document.getElementById(elementId);
        if (!el || typeof ApexCharts === 'undefined') return;

        const base = this._baseOptions(config.height, config.colors);

        const options = {
            ...base,
            chart: { ...base.chart, type: 'bar', stacked: !!config.stacked },
            series: this._toApexBarSeries(config.series),
            plotOptions: {
                bar: {
                    horizontal: !!config.horizontal,
                    borderRadius: 4,
                    columnWidth: '60%',
                    dataLabels: { position: 'top' }
                }
            },
            dataLabels: { enabled: false },
            xaxis: {
                categories: config.categories || [],
                labels: { style: { fontSize: '11px' } },
                axisBorder: { show: false },
                axisTicks: { show: false }
            },
            yaxis: this._yaxis(config.yAxisTitle)
        };

        const chart = new ApexCharts(el, options);
        chart.render();
        this.instances.set(elementId, { chart: chart, kind: 'bar' });
    },

    createPie: function (elementId, config) {
        if (this.instances.has(elementId)) this.dispose(elementId);

        const el = document.getElementById(elementId);
        if (!el || typeof ApexCharts === 'undefined') return;

        const slices = config.slices || [];
        const base = this._baseOptions(config.height, config.colors);

        const options = {
            ...base,
            chart: { ...base.chart, type: config.donut ? 'donut' : 'pie' },
            series: slices.map(s => s.value),
            labels: slices.map(s => s.label),
            dataLabels: {
                enabled: true,
                style: { fontSize: '11px', fontWeight: 500 }
            },
            stroke: { width: 2, colors: ['#fff'] },
            legend: { ...base.legend, position: 'right' }
        };
        if (config.donut) {
            options.plotOptions = {
                pie: { donut: { size: '64%', labels: { show: true, total: { show: true, label: 'Total' } } } }
            };
        }

        const chart = new ApexCharts(el, options);
        chart.render();
        this.instances.set(elementId, { chart: chart, kind: 'pie' });
    },

    updateTimeSeries: function (elementId, series) {
        const inst = this.instances.get(elementId);
        if (!inst) return;
        inst.chart.updateSeries(this._toApexTimeSeries(series), true);
    },

    updateBar: function (elementId, categories, series) {
        const inst = this.instances.get(elementId);
        if (!inst) return;
        inst.chart.updateOptions({
            xaxis: { categories: categories || [] },
            series: this._toApexBarSeries(series)
        }, false, true);
    },

    updatePie: function (elementId, slices) {
        const inst = this.instances.get(elementId);
        if (!inst) return;
        inst.chart.updateOptions({
            labels: (slices || []).map(s => s.label),
            series: (slices || []).map(s => s.value)
        }, false, true);
    },

    dispose: function (elementId) {
        const inst = this.instances.get(elementId);
        if (!inst) return;
        try { inst.chart.destroy(); } catch (e) { /* ignore */ }
        this.instances.delete(elementId);
    }
};
