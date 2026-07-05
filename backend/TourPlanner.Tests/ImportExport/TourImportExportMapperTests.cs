using NUnit.Framework;
using TourPlanner.API.Dtos.ImportExport;
using TourPlanner.API.Mappers;
using TourPlanner.BusinessLayer.Services.ImportExport;
using TourPlanner.Domain;

namespace TourPlanner.Tests.ImportExport;

/// <summary>
/// Unit tests for <see cref="TourImportExportMapper"/>.
/// Covers the unit conversion (km ↔ meters, minutes ↔ seconds / TimeSpan),
/// enum-string round-trip, defaulting of missing values (Status → "planned"),
/// and the ImportSummary → DTO projection.
/// </summary>
[TestFixture]
public class TourImportExportMapperTests
{
    private static Tour SampleTour() => new()
    {
        Id = Guid.NewGuid(),
        Name = "Wachau",
        Description = "desc",
        From = "Melk",
        To = "Krems",
        TransportType = TransportType.Cycling,
        Status = TourStatus.Completed,
        Distance = 35_000,           // 35 km
        Duration = 3_600,            // 60 min
        Color = "#8b5cf6",
        ImageUrl = null,
    };

    private static TourLog SampleLog(Guid tourId) => new()
    {
        Id = Guid.NewGuid(),
        TourId = tourId,
        LoggedAt = new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc),
        Comment = "Sunny",
        Difficulty = Difficulty.Medium,
        TotalDistance = 12_500,       // 12.5 km
        Duration = TimeSpan.FromMinutes(45),
        Rating = 4,
    };

    // ---------------------------------------------------------------
    //  Domain → DTO
    // ---------------------------------------------------------------

    [Test]
    public void ToBundle_MapsTourAndLogs_WithUnitConversion()
    {
        var t = SampleTour();
        t.Logs.Add(SampleLog(t.Id));

        var bundle = TourImportExportMapper.ToBundle(new[] { t });

        Assert.That(bundle.Version, Is.EqualTo(1));
        Assert.That(bundle.Producer, Is.EqualTo("TourPlanner"));
        Assert.That(bundle.Tours, Has.Count.EqualTo(1));

        var td = bundle.Tours[0];
        Assert.That(td.Name, Is.EqualTo("Wachau"));
        Assert.That(td.TransportType, Is.EqualTo("cycling"));
        Assert.That(td.Status, Is.EqualTo("completed"));
        Assert.That(td.Distance, Is.EqualTo(35.0));       // meters → km
        Assert.That(td.Duration, Is.EqualTo(60));         // seconds → minutes
        Assert.That(td.Logs, Has.Count.EqualTo(1));

        var ld = td.Logs[0];
        Assert.That(ld.Difficulty, Is.EqualTo("medium"));
        Assert.That(ld.TotalDistance, Is.EqualTo(12.5));
        Assert.That(ld.Duration, Is.EqualTo(45));
        Assert.That(ld.Rating, Is.EqualTo(4));
        Assert.That(ld.LoggedAt.Kind, Is.EqualTo(DateTimeKind.Utc));
    }

    // ---------------------------------------------------------------
    //  DTO → Domain
    // ---------------------------------------------------------------

    [Test]
    public void ToDomain_UnitConversion_RoundTripsWithinRounding()
    {
        var bundle = new TourExportBundleDto
        {
            Tours = new List<TourExportItemDto>
            {
                new()
                {
                    Name = "Test",
                    From = "A",
                    To = "B",
                    TransportType = "walking",
                    Distance = 12.345,       // km → 12345 m
                    Duration = 90,           // min → 5400 s
                    Status = "planned",
                    Logs = new List<TourLogExportItemDto>
                    {
                        new()
                        {
                            Difficulty = "easy",
                            TotalDistance = 5.0,
                            Duration = 30,
                            Rating = 3,
                            LoggedAt = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc),
                        },
                    },
                },
            },
        };

        var tours = TourImportExportMapper.ToDomain(bundle);

        Assert.That(tours, Has.Count.EqualTo(1));
        var t = tours[0];
        Assert.That(t.TransportType, Is.EqualTo(TransportType.Walking));
        Assert.That(t.Status, Is.EqualTo(TourStatus.Planned));
        Assert.That(t.Distance, Is.EqualTo(12345.0));
        Assert.That(t.Duration, Is.EqualTo(5400));
        Assert.That(t.Logs, Has.Count.EqualTo(1));

        var l = t.Logs[0];
        Assert.That(l.Difficulty, Is.EqualTo(Difficulty.Easy));
        Assert.That(l.TotalDistance, Is.EqualTo(5000.0));
        Assert.That(l.Duration, Is.EqualTo(TimeSpan.FromMinutes(30)));
        Assert.That(l.Rating, Is.EqualTo(3));
        Assert.That(l.LoggedAt.Kind, Is.EqualTo(DateTimeKind.Utc));
    }

    [Test]
    public void ToDomain_MissingStatus_DefaultsToPlanned()
    {
        var dto = new TourExportItemDto
        {
            Name = "n", From = "a", To = "b",
            TransportType = "driving", Distance = 1, Duration = 1,
            Status = null,
        };

        var t = TourImportExportMapper.ToDomain(dto);

        Assert.That(t.Status, Is.EqualTo(TourStatus.Planned));
    }

    [Test]
    public void ToDomain_InvalidTransportType_ProducesUnknown_ForServiceToReject()
    {
        var dto = new TourExportItemDto
        {
            Name = "n", From = "a", To = "b",
            TransportType = "teleport", Distance = 1, Duration = 1,
        };

        var t = TourImportExportMapper.ToDomain(dto);

        // Deliberately does NOT throw — the service's ValidateForImport surfaces the failure
        // as a per-tour ImportFailure instead of aborting the whole batch.
        Assert.That(t.TransportType, Is.EqualTo(TransportType.Unknown));
    }

    [Test]
    public void ToDomain_UnknownStatus_FallsBackToPlanned()
    {
        var dto = new TourExportItemDto
        {
            Name = "n", From = "a", To = "b",
            TransportType = "cycling", Distance = 1, Duration = 1,
            Status = "totally-broken",
        };

        var t = TourImportExportMapper.ToDomain(dto);

        Assert.That(t.Status, Is.EqualTo(TourStatus.Planned));
    }

    // ---------------------------------------------------------------
    //  ImportSummary → DTO
    // ---------------------------------------------------------------

    [Test]
    public void ToDto_ImportSummary_MapsAllFields()
    {
        var summary = new ImportSummary(
            Imported: 2,
            Total: 3,
            Errors: new[]
            {
                new ImportFailure(2, "Bad tour", "Transport type is invalid or missing."),
            });

        var dto = TourImportExportMapper.ToDto(summary);

        Assert.That(dto.Imported, Is.EqualTo(2));
        Assert.That(dto.Total, Is.EqualTo(3));
        Assert.That(dto.Errors, Has.Count.EqualTo(1));
        Assert.That(dto.Errors[0].Index, Is.EqualTo(2));
        Assert.That(dto.Errors[0].TourName, Is.EqualTo("Bad tour"));
        Assert.That(dto.Errors[0].Message, Does.Contain("Transport type"));
    }
}
