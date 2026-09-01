using System.Globalization;
using EcoData.Common.i18n;
using EcoData.Wildlife.Contracts.Dtos;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace FaunaFinder.Server.Reports;

public sealed class SpeciesReportDocument(
    SpeciesDtoForDetail species,
    byte[]? image,
    IReadOnlyList<FwsLinkDtoForDetail> links,
    IReadOnlyList<string> municipalities,
    SpeciesReportLabels labels
) : IDocument
{
    private const string Green = "#1F4D3A";
    private const string Muted = "#6B7280";
    private const string Line = "#E5E7EB";

    private readonly CultureInfo _culture = new(labels.Code);

    public DocumentMetadata GetMetadata() =>
        new() { Title = $"{labels.Title}: {species.ScientificName}" };

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.Letter);
            page.Margin(40);
            page.DefaultTextStyle(style => style.FontSize(10).FontColor("#111827"));

            page.Header().Element(ComposeHeader);
            page.Content().Element(ComposeContent);
            page.Footer().Element(ComposeFooter);
        });
    }

    private void ComposeHeader(IContainer container)
    {
        container
            .PaddingBottom(12)
            .BorderBottom(1)
            .BorderColor(Line)
            .Row(row =>
            {
                row.RelativeItem()
                    .Column(column =>
                    {
                        column.Item().Text("FaunaFinder").FontSize(9).FontColor(Muted);
                        column.Item().Text(labels.Title).FontSize(11).SemiBold().FontColor(Green);
                    });

                row.ConstantItem(180)
                    .AlignRight()
                    .Text($"{labels.GeneratedOn} {DateTime.UtcNow.ToString("d", _culture)}")
                    .FontSize(9)
                    .FontColor(Muted);
            });
    }

    private void ComposeContent(IContainer container)
    {
        container
            .PaddingTop(16)
            .Column(column =>
            {
                column.Spacing(18);

                column.Item().Element(ComposeTitle);
                column.Item().Element(ComposeFacts);
                column.Item().Element(ComposeMunicipalities);
                column.Item().Element(ComposeConservation);
                column.Item().Element(ComposeLocations);
            });
    }

    private void ComposeTitle(IContainer container)
    {
        var commonName = Resolve(species.CommonName, fallback: species.ScientificName);

        container.Row(row =>
        {
            row.RelativeItem()
                .Column(column =>
                {
                    column.Item().Text(commonName).FontSize(22).Bold().FontColor(Green);
                    if (!string.Equals(commonName, species.ScientificName, StringComparison.Ordinal))
                    {
                        column.Item().Text(species.ScientificName).FontSize(14).Italic().FontColor(Muted);
                    }

                    column
                        .Item()
                        .PaddingTop(6)
                        .Text(species.IsFauna ? labels.Fauna : labels.Flora)
                        .FontSize(10)
                        .FontColor(Muted);

                    if (species.Categories.Count > 0)
                    {
                        var categories = string.Join(
                            ", ",
                            species.Categories.Select(c => Resolve(c.Name, fallback: c.Code))
                        );
                        column
                            .Item()
                            .PaddingTop(2)
                            .Text($"{labels.Categories}: {categories}")
                            .FontSize(10)
                            .FontColor(Muted);
                    }
                });

            if (image is not null)
            {
                row.ConstantItem(180)
                    .PaddingLeft(16)
                    .Column(column =>
                    {
                        column.Item().Image(image).FitWidth();
                        if (!string.IsNullOrEmpty(species.ImageSourceUrl))
                        {
                            column
                                .Item()
                                .PaddingTop(4)
                                .Text($"{labels.ImageSource}: {species.ImageSourceUrl}")
                                .FontSize(7)
                                .FontColor(Muted);
                        }
                    });
            }
        });
    }

    private void ComposeFacts(IContainer container)
    {
        var facts = new List<(string Label, string Value)>
        {
            (labels.ElCode, species.ElCode),
            (labels.GlobalRank, species.GRank),
            (labels.StateRank, species.SRank),
            (labels.IucnStatus, species.IucnStatus?.ToString() ?? string.Empty),
            (labels.EndemicStatus, labels.EndemicStatuses[species.EndemicStatus]),
            (labels.Habitat, species.Habitat ?? string.Empty),
            (labels.LastObserved, species.LastObservedAtUtc?.ToString("d", _culture) ?? string.Empty),
        };

        container
            .Border(1)
            .BorderColor(Line)
            .Padding(12)
            .Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(120);
                    columns.RelativeColumn();
                    columns.ConstantColumn(120);
                    columns.RelativeColumn();
                });

                foreach (var (label, value) in facts.Where(f => !string.IsNullOrWhiteSpace(f.Value)))
                {
                    table.Cell().PaddingBottom(6).Text(label).FontSize(9).FontColor(Muted);
                    table.Cell().PaddingBottom(6).Text(value).SemiBold();
                }
            });
    }

    private void ComposeMunicipalities(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().Element(c => SectionHeading(c, labels.Municipalities, municipalities.Count));

            if (municipalities.Count == 0)
            {
                column.Item().Text(labels.NoneRecorded).FontColor(Muted);
                return;
            }

            column.Item().Text(string.Join(", ", municipalities));
        });
    }

    private void ComposeConservation(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().Element(c => SectionHeading(c, labels.ConservationActions, links.Count));

            if (links.Count == 0)
            {
                column.Item().Text(labels.NoneRecorded).FontColor(Muted);
                return;
            }

            column
                .Item()
                .Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(130);
                        columns.ConstantColumn(130);
                        columns.RelativeColumn();
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(HeaderCell).Text(labels.Practice);
                        header.Cell().Element(HeaderCell).Text(labels.Action);
                        header.Cell().Element(HeaderCell).Text(labels.Justification);
                    });

                    var ordered = links
                        .OrderBy(l => l.NrcsPractice.Code, StringComparer.Ordinal)
                        .ThenBy(l => l.FwsAction.Code, StringComparer.Ordinal);

                    foreach (var link in ordered)
                    {
                        table
                            .Cell()
                            .Element(BodyCell)
                            .Text(text =>
                            {
                                text.Span(link.NrcsPractice.Code).SemiBold();
                                text.Span($" {Resolve(link.NrcsPractice.Name)}");
                            });
                        table
                            .Cell()
                            .Element(BodyCell)
                            .Text(text =>
                            {
                                text.Span(link.FwsAction.Code).SemiBold();
                                text.Span($" {Resolve(link.FwsAction.Name)}");
                            });
                        table.Cell().Element(BodyCell).Text(Resolve(link.Justification)).FontSize(9);
                    }
                });
        });
    }

    private void ComposeLocations(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().Element(c => SectionHeading(c, labels.Locations, species.Locations.Count));

            if (species.Locations.Count == 0)
            {
                column.Item().Text(labels.NoneRecorded).FontColor(Muted);
                return;
            }

            column
                .Item()
                .Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(90);
                        columns.ConstantColumn(90);
                        columns.ConstantColumn(70);
                        columns.RelativeColumn();
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(HeaderCell).Text(labels.Latitude);
                        header.Cell().Element(HeaderCell).Text(labels.Longitude);
                        header.Cell().Element(HeaderCell).Text(labels.RadiusMeters);
                        header.Cell().Element(HeaderCell).Text(string.Empty);
                    });

                    foreach (var location in species.Locations)
                    {
                        table
                            .Cell()
                            .Element(BodyCell)
                            .Text(location.Latitude.ToString("F5", CultureInfo.InvariantCulture));
                        table
                            .Cell()
                            .Element(BodyCell)
                            .Text(location.Longitude.ToString("F5", CultureInfo.InvariantCulture));
                        table
                            .Cell()
                            .Element(BodyCell)
                            .Text(location.RadiusMeters.ToString("F0", CultureInfo.InvariantCulture));
                        table
                            .Cell()
                            .Element(BodyCell)
                            .Text(location.Description ?? string.Empty)
                            .FontColor(Muted);
                    }
                });
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container
            .PaddingTop(8)
            .BorderTop(1)
            .BorderColor(Line)
            .Row(row =>
            {
                row.RelativeItem().Text(species.ScientificName).FontSize(8).Italic().FontColor(Muted);
                row.RelativeItem()
                    .AlignRight()
                    .Text(text =>
                    {
                        text.DefaultTextStyle(style => style.FontSize(8).FontColor(Muted));
                        text.Span($"{labels.Page} ");
                        text.CurrentPageNumber();
                        text.Span($" {labels.Of} ");
                        text.TotalPages();
                    });
            });
    }

    private static void SectionHeading(IContainer container, string title, int count)
    {
        container
            .PaddingBottom(6)
            .BorderBottom(1)
            .BorderColor(Line)
            .Row(row =>
            {
                row.RelativeItem().Text(title).FontSize(12).SemiBold().FontColor(Green);
                row.ConstantItem(40)
                    .AlignRight()
                    .Text(count.ToString(CultureInfo.InvariantCulture))
                    .FontSize(9)
                    .FontColor(Muted);
            });
    }

    private static IContainer HeaderCell(IContainer container) =>
        container
            .PaddingVertical(4)
            .BorderBottom(1)
            .BorderColor(Green)
            .DefaultTextStyle(style => style.FontSize(9).SemiBold().FontColor(Green));

    private static IContainer BodyCell(IContainer container) =>
        container.PaddingVertical(4).PaddingRight(6).BorderBottom(1).BorderColor(Line);

    private string Resolve(IReadOnlyList<LocaleValue> values, string? fallback = null)
    {
        var match =
            values.FirstOrDefault(v => v.Code == labels.Code)
            ?? values.FirstOrDefault(v => v.Code == "en")
            ?? values.FirstOrDefault();
        return match?.Value ?? fallback ?? string.Empty;
    }
}
