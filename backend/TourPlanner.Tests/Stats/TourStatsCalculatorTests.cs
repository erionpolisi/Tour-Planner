using NUnit.Framework;
using TourPlanner.BusinessLayer.Services.Stats;
using TourPlanner.Domain;

namespace TourPlanner.Tests.Stats;

/// <summary>
/// Unit tests for the computed-attribute calculator that produces the
/// <em>popularity</em> and <em>child-friendliness</em> values persisted on
/// every tour. These attributes are read-only from the client's perspective;
/// the calculator is the single source of truth so the tests here also
/// document the exact bucket boundaries.
/// </summary>
[TestFixture]
public class TourStatsCalculatorTests
{
    // ----------------------------------------------------------------
    //  Popularity
    // ----------------------------------------------------------------

    [Test]
    public void Popularity_NullLogs_ReturnsZero()
    {
        Assert.That(TourStatsCalculator.Popularity(null!), Is.EqualTo(0));
    }

    [Test]
    public void Popularity_EmptyList_ReturnsZero()
    {
        Assert.That(TourStatsCalculator.Popularity(Array.Empty<TourLog>()), Is.EqualTo(0));
    }

    [TestCase(0, "not tried")]
    [TestCase(1, "some interest")]
    [TestCase(2, "some interest")]
    [TestCase(3, "popular")]
    [TestCase(5, "popular")]
    [TestCase(6, "very popular")]
    [TestCase(100, "very popular")]
    public void PopularityLabel_MatchesBoundaries(int count, string expected)
    {
        Assert.That(TourStatsCalculator.PopularityLabel(count), Is.EqualTo(expected));
    }

    [Test]
    public void Popularity_ReturnsLogCount_Exactly()
    {
        var logs = new[]
        {
            MakeLog(Difficulty.Easy,   TimeSpan.FromMinutes(30)),
            MakeLog(Difficulty.Medium, TimeSpan.FromMinutes(60)),
            MakeLog(Difficulty.Hard,   TimeSpan.FromMinutes(90)),
        };
        Assert.That(TourStatsCalculator.Popularity(logs), Is.EqualTo(3));
    }

    // ----------------------------------------------------------------
    //  Child-friendliness — endpoints
    // ----------------------------------------------------------------

    [Test]
    public void ChildFriendliness_NoLogs_UsesNeutralDefault()
    {
        // No logs → difficulty & duration components fall back to 0.5.
        // With a short (5 km) distance the score should still be well above 50.
        var score = TourStatsCalculator.ChildFriendlinessScore(
            tourDistanceMeters: 5_000,
            logs: Array.Empty<TourLog>());

        Assert.That(score, Is.EqualTo((int)Math.Round((0.4 * 0.5 + 0.3 * 0.5 + 0.3 * 1.0) * 100)));
    }

    [Test]
    public void ChildFriendliness_PerfectlyFriendly_ScoresAt100()
    {
        // Easy logs, 30-min average duration, 1 km tour → all three components
        // hit 1.0 → score 100.
        var logs = new[]
        {
            MakeLog(Difficulty.Easy, TimeSpan.FromMinutes(30)),
            MakeLog(Difficulty.Easy, TimeSpan.FromMinutes(30)),
        };

        var score = TourStatsCalculator.ChildFriendlinessScore(1_000, logs);

        Assert.That(score, Is.EqualTo(100));
    }

    [Test]
    public void ChildFriendliness_UtterlyHostile_ScoresAtZero()
    {
        // Hard logs, 10-hour average duration, 100 km tour → all three
        // components collapse to 0 → score 0.
        var logs = new[]
        {
            MakeLog(Difficulty.Hard, TimeSpan.FromHours(10)),
            MakeLog(Difficulty.Hard, TimeSpan.FromHours(10)),
        };

        var score = TourStatsCalculator.ChildFriendlinessScore(100_000, logs);

        Assert.That(score, Is.EqualTo(0));
    }

    [Test]
    public void ChildFriendliness_MediumEverything_LandsInOkBucket()
    {
        var logs = new[]
        {
            MakeLog(Difficulty.Medium, TimeSpan.FromHours(3)),
        };

        var score = TourStatsCalculator.ChildFriendlinessScore(20_000, logs);

        Assert.That(score, Is.InRange(TourStatsCalculator.OkChildFriendlinessThreshold,
                                       TourStatsCalculator.GreatChildFriendlinessThreshold - 1));
        Assert.That(TourStatsCalculator.ChildFriendlinessLabel(score),
            Is.EqualTo("ok for children"));
    }

    [TestCase(0,   "not suitable for children")]
    [TestCase(33,  "not suitable for children")]
    [TestCase(34,  "ok for children")]
    [TestCase(66,  "ok for children")]
    [TestCase(67,  "great for children")]
    [TestCase(100, "great for children")]
    public void ChildFriendlinessLabel_MatchesBoundaries(int score, string expected)
    {
        Assert.That(TourStatsCalculator.ChildFriendlinessLabel(score), Is.EqualTo(expected));
    }

    // ----------------------------------------------------------------
    //  Child-friendliness — component monotonicity
    // ----------------------------------------------------------------

    [Test]
    public void ChildFriendliness_LongerDuration_YieldsLowerScore()
    {
        var shortLog = new[] { MakeLog(Difficulty.Easy, TimeSpan.FromMinutes(30)) };
        var longLog = new[] { MakeLog(Difficulty.Easy, TimeSpan.FromHours(5)) };

        var shortScore = TourStatsCalculator.ChildFriendlinessScore(1_000, shortLog);
        var longScore = TourStatsCalculator.ChildFriendlinessScore(1_000, longLog);

        Assert.That(shortScore, Is.GreaterThan(longScore));
    }

    [Test]
    public void ChildFriendliness_LongerDistance_YieldsLowerScore()
    {
        var logs = new[] { MakeLog(Difficulty.Easy, TimeSpan.FromMinutes(30)) };

        var shortDistance = TourStatsCalculator.ChildFriendlinessScore(1_000, logs);
        var longDistance = TourStatsCalculator.ChildFriendlinessScore(80_000, logs);

        Assert.That(shortDistance, Is.GreaterThan(longDistance));
    }

    [Test]
    public void ChildFriendliness_HarderLogs_YieldLowerScore()
    {
        var easyLogs = new[] { MakeLog(Difficulty.Easy, TimeSpan.FromMinutes(60)) };
        var hardLogs = new[] { MakeLog(Difficulty.Hard, TimeSpan.FromMinutes(60)) };

        var easyScore = TourStatsCalculator.ChildFriendlinessScore(10_000, easyLogs);
        var hardScore = TourStatsCalculator.ChildFriendlinessScore(10_000, hardLogs);

        Assert.That(easyScore, Is.GreaterThan(hardScore));
    }

    // ----------------------------------------------------------------
    //  helpers
    // ----------------------------------------------------------------

    private static TourLog MakeLog(Difficulty d, TimeSpan duration) => new()
    {
        Id = Guid.NewGuid(),
        LoggedAt = DateTime.UtcNow,
        Difficulty = d,
        Duration = duration,
        TotalDistance = 0,
        Rating = 3,
        Comment = null,
    };
}
