using EcoData.Common.Pagination;
using Microsoft.AspNetCore.Components.Web.Virtualization;

namespace EcoData.Common.Pagination.Blazor;

public sealed class CursorPaginationState<TItem, TParams>(Func<TItem, Guid> keySelector)
    : IDisposable
    where TParams : CursorParameters
{
    private readonly Func<TItem, Guid> _keySelector = keySelector;
    private readonly List<TItem> _cachedItems = [];
    private readonly SemaphoreSlim _fetchLock = new(1, 1);
    private Guid? _lastCursor;
    private bool _hasMoreItems = true;
    private bool _disposed;
    private int _generation;

    public IReadOnlyList<TItem> CachedItems => _cachedItems;

    public async ValueTask<ItemsProviderResult<TItem>> ProvideItemsAsync(
        ItemsProviderRequest request,
        Func<TParams> parametersFactory,
        Func<TParams, CancellationToken, IAsyncEnumerable<TItem>> fetchAsync
    )
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var startIndex = request.StartIndex;
        var endIndex = startIndex + request.Count;
        var currentGeneration = _generation;

        // Only fetch if we need more items
        if (_hasMoreItems && _cachedItems.Count < endIndex)
        {
            await _fetchLock.WaitAsync(request.CancellationToken);
            try
            {
                // Re-check conditions after acquiring lock
                if (_generation != currentGeneration)
                    return CreateResult(startIndex, request.Count);

                // Fetch pages until we have enough items
                while (_hasMoreItems && _cachedItems.Count < endIndex)
                {
                    var cursorToFetch = _lastCursor;
                    var baseParams = parametersFactory();

                    // Use larger page size for first fetch (2x), then normal size
                    var isFirstFetch = cursorToFetch is null;
                    var pageSize = isFirstFetch ? baseParams.PageSize * 2 : baseParams.PageSize;
                    var parameters = CreateParametersWithCursor(baseParams, cursorToFetch, pageSize);

                    var fetchedCount = 0;
                    await foreach (var item in fetchAsync(parameters, request.CancellationToken))
                    {
                        _cachedItems.Add(item);
                        _lastCursor = _keySelector(item);
                        fetchedCount++;
                    }

                    // No more items if we got fewer than requested
                    if (fetchedCount < pageSize)
                    {
                        _hasMoreItems = false;
                    }
                }
            }
            finally
            {
                _fetchLock.Release();
            }
        }

        return CreateResult(startIndex, request.Count);
    }

    private ItemsProviderResult<TItem> CreateResult(int startIndex, int count)
    {
        var availableItems = _cachedItems.Skip(startIndex).Take(count).ToList();
        var totalCount = _hasMoreItems ? _cachedItems.Count + 1 : _cachedItems.Count;
        return new ItemsProviderResult<TItem>(availableItems, totalCount);
    }

    public void Reset()
    {
        _generation++;
        _cachedItems.Clear();
        _lastCursor = null;
        _hasMoreItems = true;
    }

    public void UpdateItem(TItem updatedItem)
    {
        var id = _keySelector(updatedItem);
        var index = _cachedItems.FindIndex(item => _keySelector(item).Equals(id));
        if (index >= 0)
        {
            _cachedItems[index] = updatedItem;
        }
    }

    public void UpdateItem(Guid id, Func<TItem, TItem> updater)
    {
        var index = _cachedItems.FindIndex(item => _keySelector(item).Equals(id));
        if (index >= 0)
        {
            _cachedItems[index] = updater(_cachedItems[index]);
        }
    }

    public bool RemoveItem(Guid id)
    {
        var index = _cachedItems.FindIndex(item => _keySelector(item).Equals(id));
        if (index >= 0)
        {
            _cachedItems.RemoveAt(index);
            return true;
        }
        return false;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _fetchLock.Dispose();
            _disposed = true;
        }
    }

    private static TParams CreateParametersWithCursor(TParams baseParams, Guid? cursor, int pageSize)
    {
        var type = typeof(TParams);
        var constructor = type.GetConstructors().FirstOrDefault();

        if (constructor is null)
            throw new InvalidOperationException(
                $"Type {type.Name} must have a public constructor."
            );

        var parameters = constructor.GetParameters();
        var args = new object?[parameters.Length];

        for (var i = 0; i < parameters.Length; i++)
        {
            var param = parameters[i];
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
            else if (param.Name?.Equals("pageSize", StringComparison.OrdinalIgnoreCase) == true)
            {
                args[i] = pageSize;
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
