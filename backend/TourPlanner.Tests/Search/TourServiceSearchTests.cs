using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using TourPlanner.BusinessLayer.Services;
using TourPlanner.DataAccessLayer.Repositories;
using TourPlanner.Domain;

namespace TourPlanner.Tests.Search;

/// <summary>
/// Unit tests for <see cref="TourService.SearchAsync"/>.
/// Repository is mocked — these tests verify the business-layer contract
/// (short-circuit on empty query, trim, mapping to <c>TourSearchResult</c>,
/// cancellation propagation). Real PostgreSQL FTS is covered by the smoke test.
/// </summary>
[TestFixture]
public class TourServiceSearchTests
{
    private Mock<ITourRepository> _repo = null!;
    private TourService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<ITourRepository>(MockBehavior.Strict);
        _service = new TourService(_repo.Object, NullLogger<TourService>.Instance);
    }

    private static Tour SampleTour(string name = "Wachau valley") => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        From = "Melk",
        To = "Krems",
        TransportType = TransportType.Cycling,
        Status = TourStatus.Planned,
    };

    private static TourLog SampleLog(Guid tourId, string? comment = "Great weather") => new()
    {
        Id = Guid.NewGuid(),
        TourId = tourId,
        LoggedAt = DateTime.UtcNow,
        Comment = comment,
        Difficulty = Difficulty.Medium,
        TotalDistance = 25_000,
        Duration = TimeSpan.FromMinutes(90),
        Rating = 4,
    };

    // ----------------------------------------------------------------
    //  Empty / whitespace query short-circuits
    // ----------------------------------------------------------------

    [TestCase("")]
    [TestCase("   ")]
    [TestCase("\t\n")]
    public async Task SearchAsync_EmptyOrWhitespace_ReturnsEmptyWithoutHittingRepo(string query)
    {
        var result = await _service.SearchAsync(query, limit: 50);

        Assert.That(result, Is.Empty);
        _repo.Verify(
            r => r.SearchAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task SearchAsync_NullQuery_ReturnsEmptyWithoutHittingRepo()
    {
        var result = await _service.SearchAsync(null!, limit: 50);

        Assert.That(result, Is.Empty);
        _repo.Verify(
            r => r.SearchAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ----------------------------------------------------------------
    //  Trimming
    // ----------------------------------------------------------------

    [Test]
    public async Task SearchAsync_TrimsWhitespaceBeforeCallingRepo()
    {
        _repo.Setup(r => r.SearchAsync("wachau", 25, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TourSearchHit>())
            .Verifiable();

        await _service.SearchAsync("   wachau   ", limit: 25);

        _repo.Verify(
            r => r.SearchAsync("wachau", 25, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ----------------------------------------------------------------
    //  Mapping DAL hit → BL result
    // ----------------------------------------------------------------

    [Test]
    public async Task SearchAsync_MapsRepositoryHits_OneToOne()
    {
        var tour1 = SampleTour("Wachau valley");
        var tour2 = SampleTour("Alpine crossing");
        var log = SampleLog(tour2.Id);

        _repo.Setup(r => r.SearchAsync("valley", 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TourSearchHit>
            {
                new(tour1, MatchedInTour: true, MatchedLogs: Array.Empty<TourLog>()),
                new(tour2, MatchedInTour: false, MatchedLogs: new[] { log }),
            });

        var result = await _service.SearchAsync("valley", 50);

        Assert.That(result, Has.Count.EqualTo(2));

        Assert.That(result[0].Tour, Is.SameAs(tour1));
        Assert.That(result[0].MatchedInTour, Is.True);
        Assert.That(result[0].MatchedLogs, Is.Empty);

        Assert.That(result[1].Tour, Is.SameAs(tour2));
        Assert.That(result[1].MatchedInTour, Is.False);
        Assert.That(result[1].MatchedLogs, Has.Count.EqualTo(1));
        Assert.That(result[1].MatchedLogs[0], Is.SameAs(log));
    }

    [Test]
    public async Task SearchAsync_EmptyRepositoryResult_ReturnsEmptyList()
    {
        _repo.Setup(r => r.SearchAsync("obscure", It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TourSearchHit>());

        var result = await _service.SearchAsync("obscure", limit: 50);

        Assert.That(result, Is.Empty);
    }

    // ----------------------------------------------------------------
    //  Passthrough of limit + cancellation token
    // ----------------------------------------------------------------

    [Test]
    public async Task SearchAsync_ForwardsLimitToRepository()
    {
        _repo.Setup(r => r.SearchAsync("foo", 7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TourSearchHit>())
            .Verifiable();

        await _service.SearchAsync("foo", limit: 7);

        _repo.Verify();
    }

    [Test]
    public async Task SearchAsync_ForwardsCancellationTokenToRepository()
    {
        using var cts = new CancellationTokenSource();
        _repo.Setup(r => r.SearchAsync("foo", 10, cts.Token))
            .ReturnsAsync(new List<TourSearchHit>())
            .Verifiable();

        await _service.SearchAsync("foo", limit: 10, ct: cts.Token);

        _repo.Verify();
    }

    [Test]
    public void SearchAsync_PropagatesCancellation()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        _repo.Setup(r => r.SearchAsync(It.IsAny<string>(), It.IsAny<int>(), cts.Token))
            .ThrowsAsync(new OperationCanceledException(cts.Token));

        Assert.That(
            async () => await _service.SearchAsync("foo", 10, cts.Token),
            Throws.InstanceOf<OperationCanceledException>());
    }
}
