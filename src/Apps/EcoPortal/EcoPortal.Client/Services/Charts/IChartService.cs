namespace EcoPortal.Client.Services.Charts;

public interface IChartService
{
    ValueTask<IChartInstance> CreateTimeSeriesAsync(string elementId, TimeSeriesChartConfig config);
    ValueTask<IChartInstance> CreateBarAsync(string elementId, BarChartConfig config);
    ValueTask<IChartInstance> CreatePieAsync(string elementId, PieChartConfig config);
}

public interface IChartInstance : IAsyncDisposable
{
    string ElementId { get; }

    ValueTask UpdateTimeSeriesAsync(IReadOnlyList<TimeSeries> series);
    ValueTask UpdateBarAsync(IReadOnlyList<string> categories, IReadOnlyList<BarSeries> series);
    ValueTask UpdatePieAsync(IReadOnlyList<PieSlice> slices);
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

public sealed record BarSeries(string Name, IReadOnlyList<double> Values);

public sealed record BarChartConfig(
    IReadOnlyList<string> Categories,
    IReadOnlyList<BarSeries> Series,
    int Height = 320,
    bool Horizontal = false,
    bool Stacked = false,
    string? YAxisTitle = null,
    string? ValueFormat = null,
    IReadOnlyList<string>? Colors = null
);

public sealed record PieSlice(string Label, double Value);

public sealed record PieChartConfig(
    IReadOnlyList<PieSlice> Slices,
    int Height = 320,
    bool Donut = false,
    string? ValueFormat = null,
    IReadOnlyList<string>? Colors = null
);
