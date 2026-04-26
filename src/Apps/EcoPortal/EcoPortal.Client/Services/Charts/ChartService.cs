using Microsoft.JSInterop;

namespace EcoPortal.Client.Services.Charts;

public sealed class ChartService(IJSRuntime js) : IChartService
{
    public async ValueTask<IChartInstance> CreateTimeSeriesAsync(string elementId, TimeSeriesChartConfig config)
    {
        await js.InvokeVoidAsync("chartService.createTimeSeries", elementId, config);
        return new ChartInstance(js, elementId);
    }

    public async ValueTask<IChartInstance> CreateBarAsync(string elementId, BarChartConfig config)
    {
        await js.InvokeVoidAsync("chartService.createBar", elementId, config);
        return new ChartInstance(js, elementId);
    }

    public async ValueTask<IChartInstance> CreatePieAsync(string elementId, PieChartConfig config)
    {
        await js.InvokeVoidAsync("chartService.createPie", elementId, config);
        return new ChartInstance(js, elementId);
    }
}

internal sealed class ChartInstance(IJSRuntime js, string elementId) : IChartInstance
{
    private bool _disposed;

    public string ElementId { get; } = elementId;

    public async ValueTask UpdateTimeSeriesAsync(IReadOnlyList<TimeSeries> series)
    {
        if (_disposed) return;
        await js.InvokeVoidAsync("chartService.updateTimeSeries", ElementId, series);
    }

    public async ValueTask UpdateBarAsync(IReadOnlyList<string> categories, IReadOnlyList<BarSeries> series)
    {
        if (_disposed) return;
        await js.InvokeVoidAsync("chartService.updateBar", ElementId, categories, series);
    }

    public async ValueTask UpdatePieAsync(IReadOnlyList<PieSlice> slices)
    {
        if (_disposed) return;
        await js.InvokeVoidAsync("chartService.updatePie", ElementId, slices);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        try
        {
            await js.InvokeVoidAsync("chartService.dispose", ElementId);
        }
        catch (JSDisconnectedException)
        {
            // app shutdown
        }
    }
}
