using EcoData.Sensors.Database;
using EcoData.Sensors.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace EcoData.Sensors.DataAccess.Resolvers;

public sealed class ParameterResolver(IDbContextFactory<SensorsDbContext> contextFactory)
{
    public async Task<ParameterLookup> LoadLookupAsync(
        Guid? sourceId,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var parameters = await context
            .Parameters.AsNoTracking()
            .Where(p => p.SourceId == sourceId || p.SourceId == null)
            .ToListAsync(cancellationToken);

        return new ParameterLookup(sourceId, parameters);
    }

    public async Task<ResolvedReading> ResolveAsync(
        Guid? sourceId,
        string rawCode,
        string rawUnit,
        double rawValue,
        DateTimeOffset recordedAt,
        CancellationToken cancellationToken = default
    )
    {
        var lookup = await LoadLookupAsync(sourceId, cancellationToken);
        return lookup.Resolve(rawCode, rawUnit, rawValue, recordedAt);
    }
}

public sealed class ParameterLookup
{
    private readonly Dictionary<string, Parameter> _bySourceCode;
    private readonly Dictionary<string, Parameter> _byGlobalCode;

    public ParameterLookup(Guid? sourceId, IReadOnlyList<Parameter> parameters)
    {
        SourceId = sourceId;
        _bySourceCode = parameters
            .Where(p => p.SourceId == sourceId)
            .ToDictionary(p => p.Code, StringComparer.OrdinalIgnoreCase);
        _byGlobalCode = parameters
            .Where(p => p.SourceId == null)
            .ToDictionary(p => p.Code, StringComparer.OrdinalIgnoreCase);
    }

    public Guid? SourceId { get; }

    public ResolvedReading Resolve(
        string rawCode,
        string rawUnit,
        double rawValue,
        DateTimeOffset recordedAt
    )
    {
        if (
            !_bySourceCode.TryGetValue(rawCode, out var parameter)
            && !_byGlobalCode.TryGetValue(rawCode, out parameter)
        )
        {
            return ResolvedReading.Unresolved(rawCode, rawUnit, rawValue, recordedAt);
        }

        var canonical = parameter.ValueShape switch
        {
            ValueShape.CumulativeSinceReset => throw new NotSupportedException(
                "CumulativeSinceReset values require stateful resolution; not implemented yet."
            ),
            _ => rawValue * parameter.UnitFactor + parameter.UnitOffset,
        };

        return new ResolvedReading(
            PhenomenonId: parameter.PhenomenonId,
            ParameterId: parameter.Id,
            CanonicalValue: canonical,
            RawCode: rawCode,
            RawUnit: rawUnit,
            RawValue: rawValue,
            RecordedAt: recordedAt
        );
    }
}

public readonly record struct ResolvedReading(
    Guid? PhenomenonId,
    Guid? ParameterId,
    double? CanonicalValue,
    string RawCode,
    string RawUnit,
    double RawValue,
    DateTimeOffset RecordedAt
)
{
    public static ResolvedReading Unresolved(
        string rawCode,
        string rawUnit,
        double rawValue,
        DateTimeOffset recordedAt
    ) => new(null, null, null, rawCode, rawUnit, rawValue, recordedAt);
}
