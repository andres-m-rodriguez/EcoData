namespace EcoPortal.Client.Features.Data.Services;

public interface IChartService
{
    ValueTask<IChartInstance> CreateTimeSeriesAsync(string elementId, TimeSeriesChartConfig config);
}

public interface IChartInstance : IAsyncDisposable
{
    string ElementId { get; }

    ValueTask UpdateTimeSeriesAsync(IReadOnlyList<TimeSeries> series);
}

public sealed record TimePoint(DateTimeOffset At, double Value);

public sealed record TimeSeries(string Name, IReadOnlyList<TimePoint> Points);

public sealed record TimeSeriesChartConfig(
    IReadOnlyList<TimeSeries> Series,
    int Height = 320,
    bool Smooth = true,
    bool Area = false,
    string? YAxisTitle = null,
    string? ValueFormat = null,
    IReadOnlyList<string>? Colors = null
);
