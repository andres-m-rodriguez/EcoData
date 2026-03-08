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
    private readonly HashSet<Guid> _fetchedCursors = [];
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

        while (_hasMoreItems && _cachedItems.Count < endIndex)
        {
            // Check if generation changed (Reset was called)
            if (_generation != currentGeneration)
                break;

            await _fetchLock.WaitAsync(request.CancellationToken);
            try
            {
                // Re-check after acquiring lock
                if (_generation != currentGeneration)
                    break;

                if (!_hasMoreItems || _cachedItems.Count >= endIndex)
                    break;

                // Get cursor to fetch - must be done inside lock
                var cursorToFetch = _lastCursor;
                var cursorKey = cursorToFetch ?? Guid.Empty;

                // Skip if we've already fetched this cursor
                if (_fetchedCursors.Contains(cursorKey))
                    break;

                var baseParams = parametersFactory();
                var parameters = CreateParametersWithCursor(baseParams, cursorToFetch);

                var fetchedCount = 0;
                await foreach (var item in fetchAsync(parameters, request.CancellationToken))
                {
                    _cachedItems.Add(item);
                    _lastCursor = _keySelector(item);
                    fetchedCount++;
                }

                // Mark this cursor as fetched
                _fetchedCursors.Add(cursorKey);

                if (fetchedCount == 0)
                {
                    _hasMoreItems = false;
                }
            }
            finally
            {
                _fetchLock.Release();
            }
        }

        var availableItems = _cachedItems.Skip(startIndex).Take(request.Count).ToList();

        var totalCount = _hasMoreItems ? _cachedItems.Count + 1 : _cachedItems.Count;

        return new ItemsProviderResult<TItem>(availableItems, totalCount);
    }

    public void Reset()
    {
        _generation++;
        _cachedItems.Clear();
        _lastCursor = null;
        _hasMoreItems = true;
        _fetchedCursors.Clear();
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

    private static TParams CreateParametersWithCursor(TParams baseParams, Guid? cursor)
    {
        if (cursor == baseParams.Cursor)
            return baseParams;

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
