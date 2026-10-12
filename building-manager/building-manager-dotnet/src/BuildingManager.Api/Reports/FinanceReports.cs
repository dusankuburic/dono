using BuildingManager.Orleans.Interfaces;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BuildingManager.Api.Reports;

public class FinanceReportGenerator
{
    static FinanceReportGenerator() => QuestPDF.Settings.License = LicenseType.Community;

    public byte[] FeeStatusPdf(BuildingSummary building, MonthPaymentStatus status)
    {
        var due = status.Units.Sum(u => u.Expected);
        var paid = status.Units.Sum(u => Math.Min(u.Paid, u.Expected));

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(32);
                page.DefaultTextStyle(text => text.FontSize(9));

                page.Header().Column(header =>
                {
                    header.Item().Text($"Fee status — {MonthName(status.Year, status.Month)}").FontSize(15).SemiBold();
                    header.Item().Text($"{building.Name}, {building.Street} {building.StreetNumber}, {building.City}")
                        .FontSize(9).FontColor(Colors.Grey.Darken2);
                });

                page.Content().PaddingVertical(10).Column(column =>
                {
                    column.Item().Text(text =>
                    {
                        text.Span($"{paid:N2} RSD of {due:N2} RSD collected").SemiBold();
                        text.Span(status.FeeName is null
                            ? " — no fee was in force this month"
                            : $" — fee \"{status.FeeName}\"");
                    });
                    column.Spacing(10);

                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(4);
                        });

                        table.Header(header =>
                        {
                            foreach (var heading in Header())
                            {
                                header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text(heading).SemiBold();
                            }
                        });

                        foreach (var unit in status.Units)
                        {
                            Row(table, unit.UnitNumber, $"{unit.AreaM2:0.##}", $"{unit.Expected:N2}",
                                $"{unit.Paid:N2}", StandingLabel(unit));
                        }

                        Row(table, "Total", "", $"{due:N2}", $"{paid:N2}", "");
                    });
                });

                page.Footer().AlignRight()
                    .Text($"Generated {DateTime.UtcNow:dd.MM.yyyy} · Building Manager")
                    .FontSize(8).FontColor(Colors.Grey.Darken1);
            });
        }).GeneratePdf();
    }

    public byte[] FeeStatusExcel(BuildingSummary building, MonthPaymentStatus status)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add($"Fee status {status.Year}-{status.Month:D2}");

        sheet.Cell(1, 1).Value = $"Fee status — {MonthName(status.Year, status.Month)}";
        sheet.Cell(1, 1).Style.Font.SetBold().Font.SetFontSize(14);
        sheet.Cell(2, 1).Value = $"{building.Name}, {building.Street} {building.StreetNumber}, {building.City}";
        sheet.Cell(2, 1).Style.Font.SetFontColor(XLColor.Gray);

        var headerRow = 4;
        var headers = Header();
        for (var i = 0; i < headers.Length; i++)
        {
            var cell = sheet.Cell(headerRow, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.SetBold();
        }

        var row = headerRow + 1;
        foreach (var unit in status.Units)
        {
            sheet.Cell(row, 1).Value = unit.UnitNumber;
            sheet.Cell(row, 2).Value = unit.AreaM2;
            sheet.Cell(row, 3).Value = unit.Expected;
            sheet.Cell(row, 4).Value = unit.Paid;
            sheet.Cell(row, 5).Value = StandingLabel(unit);
            row++;
        }

        sheet.Cell(row, 1).Value = "Total";
        sheet.Cell(row, 1).Style.Font.SetBold();
        sheet.Cell(row, 3).Value = status.Units.Sum(u => u.Expected);
        sheet.Cell(row, 3).Style.Font.SetBold();
        sheet.Cell(row, 4).Value = status.Units.Sum(u => Math.Min(u.Paid, u.Expected));
        sheet.Cell(row, 4).Style.Font.SetBold();
        for (var column = 3; column <= 4; column++)
        {
            sheet.Range(headerRow + 1, column, row, column).Style.NumberFormat.Format = "#,##0.00";
        }

        sheet.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static string[] Header() => new[] { "Unit", "Area (m²)", "Due (RSD)", "Paid (RSD)", "Standing" };

    private static string StandingLabel(UnitPaymentStatus unit) => unit.Standing switch
    {
        PaymentStanding.Paid => "Paid",
        PaymentStanding.OneMonthBehind => "1 month behind",
        PaymentStanding.TwoOrMoreMonthsBehind => "2+ months behind",
        _ => "No fee"
    };

    internal static string MonthName(int year, int month) =>
        new DateTime(year, month, 1).ToString("MMMM yyyy", System.Globalization.CultureInfo.InvariantCulture);

    private static void Row(TableDescriptor table, params string[] cells)
    {
        foreach (var cell in cells)
        {
            table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten1).Padding(4).Text(cell);
        }
    }
}
