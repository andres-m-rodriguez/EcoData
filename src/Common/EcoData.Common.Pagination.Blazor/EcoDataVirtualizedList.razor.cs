using EcoData.Common.Pagination;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web.Virtualization;

namespace EcoData.Common.Pagination.Blazor;

public partial class EcoDataVirtualizedList<TItem, TParams> : ComponentBase
    where TParams : CursorParameters
{
    private readonly List<TItem> _cachedItems = [];
    private Guid? _lastCursor;
    private bool _hasMoreItems = true;
    private bool _isEmpty;
    private int _generation;

    [Parameter, EditorRequired]
    public RenderFragment<TItem> ItemTemplate { get; set; } = null!;

    [Parameter]
    public RenderFragment? SkeletonTemplate { get; set; }

    [Parameter]
    public RenderFragment? EmptyTemplate { get; set; }

    [Parameter]
    public RenderFragment<Func<Task>>? FilteredEmptyTemplate { get; set; }

    [Parameter, EditorRequired]
    public Func<TParams, CancellationToken, IAsyncEnumerable<TItem>> ItemsProvider { get; set; } = null!;

    [Parameter, EditorRequired]
    public Func<TParams> ParametersFactory { get; set; } = null!;

    [Parameter, EditorRequired]
    public Func<TItem, Guid> KeySelector { get; set; } = null!;

    [Parameter]
    public Func<TParams, bool>? HasActiveFilters { get; set; }

    [Parameter]
    public EventCallback OnClearFilters { get; set; }

    [Parameter]
    public float ItemSize { get; set; } = 65;

    [Parameter]
    public int OverscanCount { get; set; } = 3;

    public IReadOnlyList<TItem> CachedItems => _cachedItems;

    public void Refresh()
    {
        _generation++;
        _cachedItems.Clear();
        _lastCursor = null;
        _hasMoreItems = true;
        _isEmpty = false;
        StateHasChanged();
    }

    public void UpdateItem(TItem updatedItem)
    {
        var id = KeySelector(updatedItem);
        var index = _cachedItems.FindIndex(item => KeySelector(item).Equals(id));
        if (index >= 0)
        {
            _cachedItems[index] = updatedItem;
            StateHasChanged();
        }
    }

    public void UpdateItem(Guid id, Func<TItem, TItem> updater)
    {
        var index = _cachedItems.FindIndex(item => KeySelector(item).Equals(id));
        if (index >= 0)
        {
            _cachedItems[index] = updater(_cachedItems[index]);
            StateHasChanged();
        }
    }

    public bool RemoveItem(Guid id)
    {
        var index = _cachedItems.FindIndex(item => KeySelector(item).Equals(id));
        if (index >= 0)
        {
            _cachedItems.RemoveAt(index);
            StateHasChanged();
            return true;
        }
        return false;
    }

    private async ValueTask<ItemsProviderResult<TItem>> LoadItemsAsync(ItemsProviderRequest request)
    {
        var startIndex = request.StartIndex;
        var endIndex = startIndex + request.Count;
        var currentGeneration = _generation;

        while (_hasMoreItems && _cachedItems.Count < endIndex)
        {
            if (_generation != currentGeneration)
                return CreateResult(startIndex, request.Count);

            var parameters = ParametersFactory();
            var paramsWithCursor = CreateParametersWithCursor(parameters, _lastCursor);

            var fetchedCount = 0;
            await foreach (var item in ItemsProvider(paramsWithCursor, request.CancellationToken))
            {
                if (_generation != currentGeneration)
                    return CreateResult(startIndex, request.Count);

                _cachedItems.Add(item);
                _lastCursor = KeySelector(item);
                fetchedCount++;
            }

            if (fetchedCount < parameters.PageSize)
            {
                _hasMoreItems = false;
            }
        }

        var result = CreateResult(startIndex, request.Count);

        if (startIndex == 0 && result.TotalItemCount == 0 && !_isEmpty)
        {
            _isEmpty = true;
            StateHasChanged();
        }

        return result;
    }

    private ItemsProviderResult<TItem> CreateResult(int startIndex, int count)
    {
        var items = _cachedItems.Skip(startIndex).Take(count).ToList();
        var totalCount = _hasMoreItems ? _cachedItems.Count + 1 : _cachedItems.Count;
        return new ItemsProviderResult<TItem>(items, totalCount);
    }

    private async Task ClearFiltersAsync()
    {
        await OnClearFilters.InvokeAsync();
    }

    private static TParams CreateParametersWithCursor(TParams baseParams, Guid? cursor)
    {
        var type = typeof(TParams);
        var constructor = type.GetConstructors().FirstOrDefault()
            ?? throw new InvalidOperationException($"Type {type.Name} must have a public constructor.");

        var ctorParams = constructor.GetParameters();
        var args = new object?[ctorParams.Length];

        for (var i = 0; i < ctorParams.Length; i++)
        {
            var param = ctorParams[i];
            var prop = type.GetProperty(
                param.Name!,
                System.Reflection.BindingFlags.Public
                    | System.Reflection.BindingFlags.Instance
                    | System.Reflection.BindingFlags.IgnoreCase
            );

            if (param.Name?.Equals("cursor", StringComparison.OrdinalIgnoreCase) == true)
            {
                args[i] = cursor;
            }
            else if (prop is not null)
            {
                args[i] = prop.GetValue(baseParams);
            }
            else
            {
                args[i] = param.HasDefaultValue ? param.DefaultValue : null;
            }
        }

        return (TParams)constructor.Invoke(args);
    }
}
