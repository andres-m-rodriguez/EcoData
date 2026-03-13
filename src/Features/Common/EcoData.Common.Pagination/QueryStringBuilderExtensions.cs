using EcoData.Common.Http.Helpers;

namespace EcoData.Common.Pagination;

public static class QueryStringBuilderExtensions
{
    public static QueryStringBuilder AddCursorParameters(
        this QueryStringBuilder builder,
        CursorParameters parameters
    )
    {
        return builder.Add("pageSize", parameters.PageSize).Add("cursor", parameters.Cursor);
    }
}
