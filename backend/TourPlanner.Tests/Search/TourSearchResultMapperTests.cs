using NUnit.Framework;
using TourPlanner.API.Mappers;
using TourPlanner.BusinessLayer.Services;
using TourPlanner.Domain;

namespace TourPlanner.Tests.Search;

/// <summary>
/// Unit tests for <see cref="TourSearchResultMapper"/>.
/// Verifies the DTO shape, unit conversion delegation, and the
/// "stitch the parent tour into the log so TourName is populated" behavior.
/// </summary>
[TestFixture]
public class TourSearchResultMapperTests
{
    private static Tour SampleTour(string name = "Wachau valley") => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        Description = "A ride along the Danube",
        From = "Melk",
        To = "Krems",
        TransportType = TransportType.Cycling,
        Status = TourStatus.Planned,
        Distance = 35_000,       // 35 km
        Duration = 3_600,        // 60 min
    };

    [Test]
    public void ToDto_MapsTourFields()
    {
        var tour = SampleTour();
        var result = new TourSearchResult(tour, MatchedInTour: true, MatchedLogs: Array.Empty<TourLog>());

        var dto = TourSearchResultMapper.ToDto(result);

        Assert.That(dto.Tour.Id, Is.EqualTo(tour.Id));
        Assert.That(dto.Tour.Name, Is.EqualTo("Wachau valley"));
        Assert.That(dto.Tour.Distance, Is.EqualTo(35.0)); // meters → km, rounded
        Assert.That(dto.Tour.Duration, Is.EqualTo(60));   // seconds → minutes
        Assert.That(dto.Tour.TransportType, Is.EqualTo("cycling"));
        Assert.That(dto.Tour.Status, Is.EqualTo("planned"));
        Assert.That(dto.MatchedInTour, Is.True);
        Assert.That(dto.MatchedLogs, Is.Empty);
    }

    [Test]
    public void ToDto_PopulatesTourNameOnMatchedLogs_WhenNavigationIsNull()
    {
        var tour = SampleTour();
        var log = new TourLog
        {
            Id = Guid.NewGuid(),
            TourId = tour.Id,
            LoggedAt = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc),
            Comment = "Nice sunset",
            Difficulty = Difficulty.Easy,
            TotalDistance = 12_500, // 12.5 km
            Duration = TimeSpan.FromMinutes(45),
            Rating = 5,
            Tour = null, // repository projection does not populate this
        };
        var result = new TourSearchResult(tour, MatchedInTour: false, MatchedLogs: new[] { log });

        var dto = TourSearchResultMapper.ToDto(result);

        Assert.That(dto.MatchedLogs, Has.Count.EqualTo(1));
        var logDto = dto.MatchedLogs[0];
        Assert.That(logDto.TourId, Is.EqualTo(tour.Id));
        Assert.That(logDto.TourName, Is.EqualTo("Wachau valley"));
        Assert.That(logDto.Difficulty, Is.EqualTo("easy"));
        Assert.That(logDto.TotalDistance, Is.EqualTo(12.5)); // m → km
        Assert.That(logDto.Duration, Is.EqualTo(45));        // TimeSpan → minutes
        Assert.That(logDto.Rating, Is.EqualTo(5));
    }

    [Test]
    public void ToDto_KeepsExistingTourNavigation_WhenAlreadyPopulated()
    {
        var tour = SampleTour();
        var otherTour = SampleTour("Some other tour");
        var log = new TourLog
        {
            Id = Guid.NewGuid(),
            TourId = tour.Id,
            LoggedAt = DateTime.UtcNow,
            Comment = "x",
            Difficulty = Difficulty.Hard,
            TotalDistance = 0,
            Duration = TimeSpan.Zero,
            Rating = 1,
            Tour = otherTour, // pretend the repo set it explicitly
        };
        var result = new TourSearchResult(tour, MatchedInTour: true, MatchedLogs: new[] { log });

        var dto = TourSearchResultMapper.ToDto(result);

        // The mapper only fills the navigation when it's null — the log's own Tour reference wins.
        Assert.That(dto.MatchedLogs[0].TourName, Is.EqualTo("Some other tour"));
    }

    [Test]
    public void ToDto_MultipleLogs_PreservesOrder()
    {
        var tour = SampleTour();
        var log1 = new TourLog { Id = Guid.NewGuid(), TourId = tour.Id, LoggedAt = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc), Comment = "first", Difficulty = Difficulty.Easy, Rating = 3, Duration = TimeSpan.FromMinutes(30) };
        var log2 = new TourLog { Id = Guid.NewGuid(), TourId = tour.Id, LoggedAt = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc), Comment = "second", Difficulty = Difficulty.Medium, Rating = 4, Duration = TimeSpan.FromMinutes(40) };

        var result = new TourSearchResult(tour, false, new[] { log1, log2 });

        var dto = TourSearchResultMapper.ToDto(result);

        Assert.That(dto.MatchedLogs.Select(l => l.Comment), Is.EqualTo(new[] { "first", "second" }));
    }
}
