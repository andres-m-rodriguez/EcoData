namespace EcoData.Common.Pagination;

public abstract record CursorParameters(int PageSize = 20, Guid? Cursor = null);
