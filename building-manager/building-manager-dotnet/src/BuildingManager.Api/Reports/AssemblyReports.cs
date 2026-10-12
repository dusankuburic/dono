using BuildingManager.Core.Enums;
using BuildingManager.Orleans.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BuildingManager.Api.Reports;

/// <summary>
/// PDF documents for owners' assemblies: the notice (UC-007, printed for
/// physical posting) and the minutes (UC-006). Plain generators — they take
/// grain DTOs and return bytes, so tests can call them directly.
/// </summary>
public class AssemblyReportGenerator
{
    static AssemblyReportGenerator() => QuestPDF.Settings.License = LicenseType.Community;

    public byte[] NoticePdf(BuildingSummary building, AssemblyDetails assembly)
    {
        return Document.Create(container =>
        {
            Page(container, $"Notice of owners' assembly — {building.Name}", building, page =>
            {
                page.Column(column =>
                {
                    column.Item().Text(
                        $"An owners' assembly of the residential community {building.Name} will be held:");
                    column.Spacing(12);

                    Meta(column, "Date and time", $"{assembly.Date:dd.MM.yyyy HH:mm} (UTC)");
                    Meta(column, "Location", assembly.Location);
                    Meta(column, "Session", SessionLabel(assembly.SessionType));
                    column.Spacing(12);

                    column.Item().Text("Agenda").FontSize(12).SemiBold();
                    foreach (var item in assembly.Agenda.OrderBy(a => a.Order))
                    {
                        column.Item().Text($"{item.Order}. {item.Title}");
                        if (!string.IsNullOrWhiteSpace(item.Description))
                        {
                            column.Item().PaddingLeft(16).Text(item.Description.Trim()).FontSize(9).FontColor(Colors.Grey.Darken2);
                        }
                    }
                    column.Spacing(12);

                    column.Item().Text(
                        "All owners may attend in person, through a representative with a written proxy, " +
                        "or cast a written vote in advance, per the Law on Housing of the Republic of Serbia.")
                        .FontSize(9).FontColor(Colors.Grey.Darken2);
                    column.Spacing(24);

                    column.Item().Text("Posted on the building's notice board on: ______________________");
                    column.Item().PaddingTop(6).Text("(signed by the manager)").FontSize(9).FontColor(Colors.Grey.Darken2);
                });
            });
        }).GeneratePdf();
    }

    public byte[] MinutesPdf(BuildingSummary building, AssemblyDetails assembly)
    {
        return Document.Create(container =>
        {
            Page(container, $"Minutes of the owners' assembly — {building.Name}", building, page =>
            {
                page.Column(column =>
                {
                    column.Item().Text(
                        assembly.CompletedAt is { } completed
                            ? $"Held on {assembly.Date:dd.MM.yyyy HH:mm} (UTC), minutes closed {completed:dd.MM.yyyy}."
                            : $"Held on {assembly.Date:dd.MM.yyyy HH:mm} (UTC) — not yet closed.");
                    column.Spacing(10);

                    Meta(column, "Location", assembly.Location);
                    Meta(column, "Session", SessionLabel(assembly.SessionType));
                    if (assembly.QuorumAchieved is { } quorum)
                    {
                        Meta(column, "Attendance",
                            $"{assembly.PresentVotes:0.##} of {assembly.EligibleVotes:0.##} voting shares " +
                            $"(required {assembly.QuorumPercentage:0}%) — quorum " + (quorum ? "achieved" : "NOT achieved"));
                    }
                    column.Spacing(12);

                    column.Item().Text("Attendance").FontSize(12).SemiBold();
                    if (assembly.Attendance.Count == 0)
                    {
                        column.Item().Text("No attendance recorded.").FontColor(Colors.Grey.Darken1);
                    }
                    else
                    {
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(4);
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(2);
                            });
                            foreach (var (unit, attendee, type, proxy, share) in assembly.Attendance.Select(a =>
                                         (a.UnitNumber, a.AttendeeName ?? "", AttendanceLabel(a.Type),
                                             a.ProxyHolderName ?? "", a.VotingShare)))
                            {
                                Cell(table, unit).SemiBold();
                                Cell(table, attendee);
                                Cell(table, type);
                                Cell(table, proxy);
                                Cell(table, $"{share:0.##}");
                            }
                        });
                    }
                    column.Spacing(12);

                    column.Item().Text("Agenda and decisions").FontSize(12).SemiBold();
                    foreach (var item in assembly.Agenda.OrderBy(a => a.Order))
                    {
                        column.Item().PaddingTop(6).Text($"{item.Order}. {item.Title}").SemiBold();
                        if (!string.IsNullOrWhiteSpace(item.Description))
                        {
                            column.Item().PaddingLeft(12).Text(item.Description.Trim()).FontSize(9);
                        }
                        if (item.RequiresVote)
                        {
                            var decision = item.IsFinalized
                                ? $"{ResultLabel(item.Result)} ({VotingLabel(item.VotingType)}); " +
                                  $"for {item.VotesFor:0.##}, against {item.VotesAgainst:0.##}, abstained {item.VotesAbstain:0.##}"
                                : "Not decided";
                            column.Item().PaddingLeft(12).Text(decision).FontSize(9).FontColor(Colors.Grey.Darken2);
                        }
                        else
                        {
                            column.Item().PaddingLeft(12).Text("Informational item.").FontSize(9).FontColor(Colors.Grey.Darken2);
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(assembly.Notes))
                    {
                        column.Spacing(10);
                        column.Item().Text("Notes").FontSize(12).SemiBold();
                        column.Item().Text(assembly.Notes.Trim()).FontSize(9);
                    }

                    column.Spacing(28);
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Column(sign => { Signature(sign, "Meeting chair (manager)"); });
                        row.ConstantItem(48);
                        row.RelativeItem().Column(sign => { Signature(sign, "Minutes taker"); });
                    });
                });
            });
        }).GeneratePdf();
    }

    private static void Page(IDocumentContainer container, string title, BuildingSummary building,
        Action<IContainer> content)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(36);
            page.DefaultTextStyle(text => text.FontSize(10));

            page.Header().Column(header =>
            {
                header.Item().Text(title).FontSize(15).SemiBold();
                header.Item().Text($"{building.Street} {building.StreetNumber}, {building.City}")
                    .FontSize(9).FontColor(Colors.Grey.Darken2);
            });

            page.Content().PaddingVertical(10).Element(content);

            page.Footer().AlignCenter().Text(text =>
            {
                text.Span("Page ").FontSize(8).FontColor(Colors.Grey.Darken1);
                text.CurrentPageNumber().FontSize(8).FontColor(Colors.Grey.Darken1);
                text.Span(" of ").FontSize(8).FontColor(Colors.Grey.Darken1);
                text.TotalPages().FontSize(8).FontColor(Colors.Grey.Darken1);
                text.Span($" · generated {DateTime.UtcNow:dd.MM.yyyy}").FontSize(8).FontColor(Colors.Grey.Darken1);
            });
        });
    }

    private static void Meta(ColumnDescriptor column, string label, string value)
    {
        column.Item().Text(text =>
        {
            text.Span($"{label}: ").SemiBold();
            text.Span(value);
        });
    }

    private static TextBlockDescriptor Cell(TableDescriptor table, string text) =>
        table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten1).PaddingVertical(3).Text(text).FontSize(9);

    private static void Signature(ColumnDescriptor column, string role)
    {
        column.Item().PaddingTop(30).Text("____________________________");
        column.Item().Text(role).FontSize(9).FontColor(Colors.Grey.Darken2);
    }

    internal static string SessionLabel(SessionType session) =>
        session == SessionType.Repeated ? "Repeated session" : "First session";

    internal static string AttendanceLabel(AttendanceType type) => type switch
    {
        AttendanceType.InPerson => "In person",
        AttendanceType.Proxy => "Proxy",
        AttendanceType.Written => "Written in advance",
        AttendanceType.Electronic => "Electronic",
        _ => type.ToString()
    };

    internal static string ResultLabel(VoteResult result) => result switch
    {
        VoteResult.Passed => "ADOPTED",
        VoteResult.Failed => "REJECTED",
        _ => "pending"
    };

    internal static string VotingLabel(VotingType voting) => voting switch
    {
        VotingType.Unanimous => "unanimous",
        VotingType.TwoThirds => "two-thirds majority",
        _ => "simple majority"
    };
}
