using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using TourPlanner.BusinessLayer.Exceptions;
using TourPlanner.BusinessLayer.Services;
using TourPlanner.DataAccessLayer.Repositories;
using TourPlanner.Domain;

namespace TourPlanner.Tests.Stats;

/// <summary>
/// Verifies that <see cref="TourLogService"/> keeps <see cref="Tour.Popularity"/>
/// and <see cref="Tour.ChildFriendliness"/> in sync every time a log is
/// created, updated, or deleted. This is what makes the persisted stats
/// (and therefore the full-text-search index that includes them) reliable.
/// </summary>
[TestFixture]
public class TourLogServiceStatsTests
{
    private static readonly Guid Owner = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    private Mock<ITourLogRepository> _logs = null!;
    private Mock<ITourRepository> _tours = null!;
    private TourLogService _service = null!;
    private Tour _tour = null!;

    [SetUp]
    public void SetUp()
    {
        _tour = new Tour
        {
            Id = Guid.NewGuid(),
            UserId = Owner,
            Name = "T",
            From = "A", To = "B",
            TransportType = TransportType.Walking,
            Status = TourStatus.Planned,
            Distance = 5_000,       // 5 km — friendly baseline
        };

        _logs = new Mock<ITourLogRepository>(MockBehavior.Strict);
        _tours = new Mock<ITourRepository>(MockBehavior.Strict);
        _service = new TourLogService(_logs.Object, _tours.Object, NullLogger<TourLogService>.Instance);
    }

    private static TourLog MakeLog(Guid tourId, Difficulty d, TimeSpan duration) => new()
    {
        Id = Guid.NewGuid(),
        TourId = tourId,
        LoggedAt = DateTime.UtcNow,
        Difficulty = d,
        Duration = duration,
        Rating = 3,
        TotalDistance = 0,
    };

    // ----------------------------------------------------------------
    //  CreateAsync
    // ----------------------------------------------------------------

    [Test]
    public async Task CreateAsync_UpdatesTourStats_AfterInsert()
    {
        // Two existing logs (easy, short) → new tour stats after we add a third.
        var existing1 = MakeLog(_tour.Id, Difficulty.Easy, TimeSpan.FromMinutes(30));
        var existing2 = MakeLog(_tour.Id, Difficulty.Easy, TimeSpan.FromMinutes(30));
        var newLog = MakeLog(_tour.Id, Difficulty.Easy, TimeSpan.FromMinutes(30));

        _tours.Setup(r => r.GetByIdAsync(Owner, _tour.Id)).ReturnsAsync(_tour);
        _logs.Setup(r => r.AddAsync(newLog)).Returns(Task.CompletedTask);
        _logs.Setup(r => r.GetForTourAsync(_tour.Id))
            .ReturnsAsync(new List<TourLog> { existing1, existing2, newLog });
        _logs.Setup(r => r.GetByIdAsync(Owner, newLog.Id)).ReturnsAsync(newLog);
        _tours.Setup(r => r.UpdateAsync(_tour)).Returns(Task.CompletedTask);

        await _service.CreateAsync(Owner, newLog);

        Assert.That(_tour.Popularity, Is.EqualTo(3));
        Assert.That(_tour.ChildFriendliness, Is.EqualTo(100));
        _tours.Verify(r => r.UpdateAsync(_tour), Times.Once);
    }

    [Test]
    public void CreateAsync_UnownedTour_ThrowsNotFound_AndSkipsStatsRefresh()
    {
        var log = MakeLog(_tour.Id, Difficulty.Easy, TimeSpan.FromMinutes(30));
        _tours.Setup(r => r.GetByIdAsync(Owner, _tour.Id)).ReturnsAsync((Tour?)null);

        Assert.That(
            async () => await _service.CreateAsync(Owner, log),
            Throws.TypeOf<NotFoundException>());

        _logs.Verify(r => r.AddAsync(It.IsAny<TourLog>()), Times.Never);
        _tours.Verify(r => r.UpdateAsync(It.IsAny<Tour>()), Times.Never);
    }

    // ----------------------------------------------------------------
    //  UpdateAsync
    // ----------------------------------------------------------------

    [Test]
    public async Task UpdateAsync_RecomputesStats_AfterMutation()
    {
        // Start with a hard 5-hour log → the stats are low. Update it to easy
        // 30 min and the child-friendliness should climb.
        var log = MakeLog(_tour.Id, Difficulty.Hard, TimeSpan.FromHours(5));
        log.Tour = _tour;

        _logs.Setup(r => r.GetByIdAsync(Owner, log.Id)).ReturnsAsync(log);
        _logs.Setup(r => r.UpdateAsync(log)).Returns(Task.CompletedTask);
        _logs.Setup(r => r.GetForTourAsync(_tour.Id))
            .ReturnsAsync(new List<TourLog> { log });
        _tours.Setup(r => r.UpdateAsync(_tour)).Returns(Task.CompletedTask);

        await _service.UpdateAsync(Owner, log.Id, l =>
        {
            l.Difficulty = Difficulty.Easy;
            l.Duration = TimeSpan.FromMinutes(30);
        });

        Assert.That(_tour.Popularity, Is.EqualTo(1));
        // Perfect: easy + 30 min + 5 km → 100.
        Assert.That(_tour.ChildFriendliness, Is.EqualTo(100));
    }

    // ----------------------------------------------------------------
    //  DeleteAsync
    // ----------------------------------------------------------------

    [Test]
    public async Task DeleteAsync_RecomputesStats_WithRemainingLogs()
    {
        _tour.Popularity = 2;
        _tour.ChildFriendliness = 80;

        var log = MakeLog(_tour.Id, Difficulty.Easy, TimeSpan.FromMinutes(30));
        log.Tour = _tour;

        _logs.Setup(r => r.GetByIdAsync(Owner, log.Id)).ReturnsAsync(log);
        _logs.Setup(r => r.DeleteAsync(Owner, log.Id)).ReturnsAsync(true);
        // After delete there is one remaining log.
        var remaining = MakeLog(_tour.Id, Difficulty.Easy, TimeSpan.FromMinutes(60));
        _logs.Setup(r => r.GetForTourAsync(_tour.Id))
            .ReturnsAsync(new List<TourLog> { remaining });
        _tours.Setup(r => r.UpdateAsync(_tour)).Returns(Task.CompletedTask);

        await _service.DeleteAsync(Owner, log.Id);

        Assert.That(_tour.Popularity, Is.EqualTo(1));
        Assert.That(_tour.ChildFriendliness, Is.EqualTo(100));
    }
}
