using BuildingManager.Api.Reports;
using BuildingManager.Core.Enums;
using BuildingManager.Orleans.Interfaces;
using ClosedXML.Excel;
using FluentAssertions;
using Xunit;

namespace BuildingManager.Tests.Reports;

/// <summary>
/// The report generators take grain DTOs and return bytes, so they can be
/// exercised directly: PDFs must be real PDFs, the Excel must round-trip
/// through ClosedXML with the units listed.
/// </summary>
public class ReportGenerationTests
{
    private static readonly BuildingSummary Building = new()
    {
        Id = "b1", Name = "SZ Bulevar 7", Street = "Bulevar oslobodjenja", StreetNumber = "7", City = "Novi Sad"
    };

    private static AssemblyDetails Assembly(SessionType session = SessionType.First) => new()
    {
        Id = Guid.NewGuid(),
        BuildingId = "b1",
        Date = new DateTime(2026, 3, 12, 17, 0, 0, DateTimeKind.Utc),
        Location = "Building entrance hall",
        SessionType = session,
        Status = AssemblyStatus.Completed,
        CompletedAt = new DateTime(2026, 3, 12, 19, 30, 0, DateTimeKind.Utc),
        EligibleVotes = 100m,
        PresentVotes = 62m,
        QuorumPercentage = 51m,
        QuorumAchieved = true,
        Attendance = new List<AttendanceInfo>
        {
            new() { UnitNumber = "1", Type = AttendanceType.InPerson, AttendeeName = "Ana Anic", VotingShare = 40m },
            new() { UnitNumber = "2", Type = AttendanceType.Proxy, AttendeeName = "Pero Peric",
                ProxyHolderName = "Ana Anic", VotingShare = 22m }
        },
        Agenda = new List<AgendaItemInfo>
        {
            new() { Order = 1, Title = "Report on last year", RequiresVote = false,
                Description = "Financial report presented." },
            new() { Order = 2, Title = "Fee for 2026", VotingType = VotingType.Simple, RequiresVote = true,
                IsFinalized = true, Result = VoteResult.Passed, VotesFor = 62m, VotesAgainst = 0m, VotesAbstain = 0m }
        },
        Notes = "The assembly ended amicably."
    };

    private static MonthPaymentStatus FeeStatus() => new()
    {
        Year = 2026, Month = 10, FeeName = "Standard",
        Units = new List<UnitPaymentStatus>
        {
            new() { UnitNumber = "1", AreaM2 = 100m, Expected = 1200m, Paid = 1200m, Standing = PaymentStanding.Paid },
            new() { UnitNumber = "2", AreaM2 = 50m, Expected = 700m, Paid = 300m, Standing = PaymentStanding.OneMonthBehind },
            new() { UnitNumber = "3", AreaM2 = 80m, Expected = 1000m, Paid = 0m, Standing = PaymentStanding.TwoOrMoreMonthsBehind }
        }
    };

    [Fact]
    public void Assembly_Notice_And_Minutes_Are_Valid_Pdfs()
    {
        var generator = new AssemblyReportGenerator();

        foreach (var pdf in new[] { generator.NoticePdf(Building, Assembly()), generator.MinutesPdf(Building, Assembly()) })
        {
            pdf.Should().NotBeNullOrEmpty();
            Signature(pdf).Should().Be("%PDF-");
            pdf.Length.Should().BeGreaterThan(1_000, "a real document, not an empty shell");
        }
    }

    [Fact]
    public void Minutes_For_A_Repeated_Session_Are_Still_A_Pdf()
    {
        var pdf = new AssemblyReportGenerator().MinutesPdf(Building, Assembly(SessionType.Repeated));
        Signature(pdf).Should().Be("%PDF-");
    }

    [Fact]
    public void Fee_Status_Pdf_Is_A_Valid_Pdf()
    {
        var pdf = new FinanceReportGenerator().FeeStatusPdf(Building, FeeStatus());
        Signature(pdf).Should().Be("%PDF-");
        pdf.Length.Should().BeGreaterThan(1_000);
    }

    [Fact]
    public void Fee_Status_Excel_Lists_Every_Unit_And_The_Totals()
    {
        var bytes = new FinanceReportGenerator().FeeStatusExcel(Building, FeeStatus());

        using var workbook = new XLWorkbook(new MemoryStream(bytes));
        var sheet = workbook.Worksheets.First();

        sheet.Name.Should().StartWith("Fee status");
        sheet.Cell(1, 1).GetString().Should().StartWith("Fee status —");
        sheet.Cell(4, 1).GetString().Should().Be("Unit");

        sheet.Cell(5, 1).GetString().Should().Be("1");
        sheet.Cell(5, 5).GetString().Should().Be("Paid");
        sheet.Cell(6, 5).GetString().Should().Be("1 month behind");
        sheet.Cell(7, 5).GetString().Should().Be("2+ months behind");

        sheet.Cell(8, 1).GetString().Should().Be("Total");
        sheet.Cell(8, 3).GetValue<decimal>().Should().Be(2900m); // 1200 + 700 + 1000
        sheet.Cell(8, 4).GetValue<decimal>().Should().Be(1500m); // min(1200,1200) + min(300,700) + min(0,1000)
    }

    /// <summary>PDFs start with "%PDF-", the file signature.</summary>
    private static string Signature(byte[] pdf) =>
        System.Text.Encoding.ASCII.GetString(pdf, 0, 5);
}
