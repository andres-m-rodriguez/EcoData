using System.Runtime.CompilerServices;
using EcoData.Wildlife.Contracts;
using EcoData.Wildlife.Contracts.Dtos;
using EcoData.Wildlife.Contracts.Parameters;
using EcoData.Wildlife.DataAccess.Interfaces;
using EcoData.Wildlife.Database;
using EcoData.Wildlife.Database.Models;
using Microsoft.EntityFrameworkCore;
using OneOf;
using OneOf.Types;

namespace EcoData.Wildlife.DataAccess.Repositories;

public sealed class SightingRepository(IDbContextFactory<WildlifeDbContext> contextFactory)
    : ISightingRepository
{
    public async Task<OneOf<SightingDto, NotFound>> CreateAsync(
        Guid organizationId,
        Guid reporterUserId,
        string reporterDisplayName,
        SightingDtoForCreate dto,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        if (!await context.Species.AnyAsync(species => species.Id == dto.SpeciesId, cancellationToken))
            return new NotFound();

        var now = DateTimeOffset.UtcNow;
        var sighting = new Sighting
        {
            Id = Guid.CreateVersion7(),
            OrganizationId = organizationId,
            SpeciesId = dto.SpeciesId,
            ReporterUserId = reporterUserId,
            ReporterDisplayName = reporterDisplayName,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            MunicipalityId = dto.MunicipalityId,
            // Npgsql only writes offset-zero values to timestamptz.
            ObservedAtUtc = dto.ObservedAtUtc.ToUniversalTime(),
            IndividualCount = dto.IndividualCount,
            Status = SightingStatus.Pending,
            ReviewedByUserId = null,
            ReviewedByDisplayName = null,
            ReviewedAtUtc = null,
            ReviewReason = null,
            CreatedAtUtc = now,
        };
        context.Sightings.Add(sighting);

        if (!string.IsNullOrWhiteSpace(dto.Note))
        {
            context.SightingNotes.Add(
                new SightingNote
                {
                    Id = Guid.CreateVersion7(),
                    SightingId = sighting.Id,
                    AuthorUserId = reporterUserId,
                    AuthorDisplayName = reporterDisplayName,
                    Text = dto.Note.Trim(),
                    CreatedAtUtc = now,
                }
            );
        }

        await context.SaveChangesAsync(cancellationToken);

        return await context
            .Sightings.Where(created => created.Id == sighting.Id)
            .Select(created => new SightingDto(
                created.Id,
                created.OrganizationId,
                created.SpeciesId,
                created.Species!.CommonName,
                created.Species.ScientificName,
                created.Latitude,
                created.Longitude,
                created.MunicipalityId,
                created.ObservedAtUtc,
                created.IndividualCount,
                created.Status,
                created.ReporterUserId,
                created.ReporterDisplayName,
                created.ReviewedByDisplayName,
                created.ReviewedAtUtc,
                created.ReviewReason,
                created.CreatedAtUtc,
                created
                    .Notes.OrderBy(note => note.Id)
                    .Select(note => new SightingNoteDto(
                        note.Id,
                        note.AuthorUserId,
                        note.AuthorDisplayName,
                        note.Text,
                        note.CreatedAtUtc
                    ))
                    .ToList(),
                created
                    .Images.OrderBy(image => image.Id)
                    .Select(image => new SightingImageDto(
                        image.Id,
                        image.ContentType,
                        image.SizeBytes,
                        image.UploadedByDisplayName,
                        image.CreatedAtUtc
                    ))
                    .ToList()
            ))
            .FirstAsync(cancellationToken);
    }

    public async IAsyncEnumerable<SightingDto> GetMineAsync(
        Guid reporterUserId,
        SightingParameters parameters,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var query = context.Sightings.Where(sighting => sighting.ReporterUserId == reporterUserId);

        if (parameters.Status is { } status)
        {
            query = query.Where(sighting => sighting.Status == status);
        }

        if (parameters.SpeciesId is { } speciesId)
        {
            query = query.Where(sighting => sighting.SpeciesId == speciesId);
        }

        if (parameters.Cursor is { } cursor)
        {
            query = query.Where(sighting => sighting.Id < cursor);
        }

        await foreach (
            var sighting in query
                .OrderByDescending(sighting => sighting.Id)
                .Take(parameters.PageSize + 1)
                .Select(sighting => new SightingDto(
                    sighting.Id,
                    sighting.OrganizationId,
                    sighting.SpeciesId,
                    sighting.Species!.CommonName,
                    sighting.Species.ScientificName,
                    sighting.Latitude,
                    sighting.Longitude,
                    sighting.MunicipalityId,
                    sighting.ObservedAtUtc,
                    sighting.IndividualCount,
                    sighting.Status,
                    sighting.ReporterUserId,
                    sighting.ReporterDisplayName,
                    sighting.ReviewedByDisplayName,
                    sighting.ReviewedAtUtc,
                    sighting.ReviewReason,
                    sighting.CreatedAtUtc,
                    sighting
                        .Notes.OrderBy(note => note.Id)
                        .Select(note => new SightingNoteDto(
                            note.Id,
                            note.AuthorUserId,
                            note.AuthorDisplayName,
                            note.Text,
                            note.CreatedAtUtc
                        ))
                        .ToList(),
                    sighting
                        .Images.OrderBy(image => image.Id)
                        .Select(image => new SightingImageDto(
                            image.Id,
                            image.ContentType,
                            image.SizeBytes,
                            image.UploadedByDisplayName,
                            image.CreatedAtUtc
                        ))
                        .ToList()
                ))
                .AsAsyncEnumerable()
                .WithCancellation(cancellationToken)
        )
        {
            yield return sighting;
        }
    }

    public async Task<OneOf<SightingNoteDto, NotFound>> AddNoteAsync(
        Guid sightingId,
        Guid authorUserId,
        string authorDisplayName,
        SightingNoteDtoForCreate dto,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        if (!await context.Sightings.AnyAsync(sighting => sighting.Id == sightingId, cancellationToken))
            return new NotFound();

        var note = new SightingNote
        {
            Id = Guid.CreateVersion7(),
            SightingId = sightingId,
            AuthorUserId = authorUserId,
            AuthorDisplayName = authorDisplayName,
            Text = dto.Text.Trim(),
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
        context.SightingNotes.Add(note);
        await context.SaveChangesAsync(cancellationToken);

        return new SightingNoteDto(
            note.Id,
            note.AuthorUserId,
            note.AuthorDisplayName,
            note.Text,
            note.CreatedAtUtc
        );
    }

    public async Task<(Guid OrganizationId, Guid ReporterUserId)?> GetOwnerAsync(
        Guid sightingId,
        CancellationToken cancellationToken = default
    )
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var owner = await context
            .Sightings.Where(sighting => sighting.Id == sightingId)
            .Select(sighting => new { sighting.OrganizationId, sighting.ReporterUserId })
            .FirstOrDefaultAsync(cancellationToken);

        return owner is null ? null : (owner.OrganizationId, owner.ReporterUserId);
    }
}
