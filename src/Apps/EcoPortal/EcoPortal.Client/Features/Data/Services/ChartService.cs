using EcoData.Ui.Interop;

namespace EcoPortal.Client.Features.Data.Services;

public sealed class ChartService(IJavascriptSafeInterop js) : IChartService
{
    public async ValueTask<IChartInstance> CreateTimeSeriesAsync(string elementId, TimeSeriesChartConfig config)
    {
        await js.InvokeVoidAsync("chartService.createTimeSeries", elementId, config);
        return new ChartInstance(js, elementId);
    }
}

internal sealed class ChartInstance(IJavascriptSafeInterop js, string elementId) : IChartInstance
{
    private bool _disposed;

    public string ElementId { get; } = elementId;

    public async ValueTask UpdateTimeSeriesAsync(IReadOnlyList<TimeSeries> series)
    {
        if (_disposed) return;
        await js.InvokeVoidAsync("chartService.updateTimeSeries", ElementId, series);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        await js.InvokeVoidAsync("chartService.dispose", ElementId);
    }
}
