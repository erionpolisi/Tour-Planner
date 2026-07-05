using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using TourPlanner.BusinessLayer.Services.ImportExport;
using TourPlanner.DataAccessLayer.Repositories;
using TourPlanner.Domain;

namespace TourPlanner.Tests.ImportExport;

/// <summary>
/// Unit tests for <see cref="TourImportExportService"/>.
/// Repository is mocked; these tests cover:
///   * export composes tours + logs from the repository unchanged
///   * import assigns fresh IDs and re-links logs
///   * import is best-effort (invalid entries don't abort the batch)
///   * validation catches enum/range violations at the business-layer boundary
/// </summary>
[TestFixture]
public class TourImportExportServiceTests
{
    private Mock<ITourRepository> _repo = null!;
    private TourImportExportService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<ITourRepository>(MockBehavior.Strict);
        _service = new TourImportExportService(
            _repo.Object,
            NullLogger<TourImportExportService>.Instance);
    }

    private static Tour ValidTour(string name = "Vienna loop") => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        Description = "desc",
        From = "Vienna",
        To = "Salzburg",
        TransportType = TransportType.Cycling,
        Status = TourStatus.Planned,
        Distance = 300_000,
        Duration = 10_800,
    };

    private static TourLog ValidLog(Guid tourId) => new()
    {
        Id = Guid.NewGuid(),
        TourId = tourId,
        LoggedAt = DateTime.UtcNow,
        Comment = "great",
        Difficulty = Difficulty.Medium,
        TotalDistance = 20_000,
        Duration = TimeSpan.FromMinutes(60),
        Rating = 4,
    };

    // ----------------------------------------------------------------
    //  Export
    // ----------------------------------------------------------------

    [Test]
    public async Task ExportAllAsync_ReturnsTours_FromRepository()
    {
        var tour = ValidTour();
        tour.Logs.Add(ValidLog(tour.Id));
        _repo.Setup(r => r.GetAllWithLogsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Tour> { tour });

        var result = await _service.ExportAllAsync();

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Name, Is.EqualTo("Vienna loop"));
        Assert.That(result[0].Logs, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task ExportAllAsync_ForwardsCancellationToken()
    {
        using var cts = new CancellationTokenSource();
        _repo.Setup(r => r.GetAllWithLogsAsync(cts.Token))
            .ReturnsAsync(new List<Tour>())
            .Verifiable();

        await _service.ExportAllAsync(cts.Token);

        _repo.Verify();
    }

    // ----------------------------------------------------------------
    //  Import happy path
    // ----------------------------------------------------------------

    [Test]
    public async Task ImportAsync_EmptyList_ReturnsZeros_AndDoesNotHitRepo()
    {
        var summary = await _service.ImportAsync(Array.Empty<Tour>());

        Assert.That(summary.Total, Is.EqualTo(0));
        Assert.That(summary.Imported, Is.EqualTo(0));
        Assert.That(summary.Errors, Is.Empty);
        _repo.Verify(r => r.AddAsync(It.IsAny<Tour>()), Times.Never);
    }

    [Test]
    public async Task ImportAsync_ValidTours_AllSaved()
    {
        var t1 = ValidTour("A");
        var t2 = ValidTour("B");
        _repo.Setup(r => r.AddAsync(It.IsAny<Tour>())).Returns(Task.CompletedTask);

        var summary = await _service.ImportAsync(new[] { t1, t2 });

        Assert.That(summary.Imported, Is.EqualTo(2));
        Assert.That(summary.Total, Is.EqualTo(2));
        Assert.That(summary.Errors, Is.Empty);
        _repo.Verify(r => r.AddAsync(It.IsAny<Tour>()), Times.Exactly(2));
    }

    [Test]
    public async Task ImportAsync_AssignsFreshId_AndRelinksLogs()
    {
        var original = ValidTour();
        var originalId = original.Id;
        var log = ValidLog(originalId);
        var originalLogId = log.Id;
        original.Logs.Add(log);

        Tour? captured = null;
        _repo.Setup(r => r.AddAsync(It.IsAny<Tour>()))
            .Callback<Tour>(t => captured = t)
            .Returns(Task.CompletedTask);

        await _service.ImportAsync(new[] { original });

        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.Id, Is.Not.EqualTo(originalId));
        Assert.That(captured.Logs, Has.Count.EqualTo(1));
        Assert.That(captured.Logs[0].Id, Is.Not.EqualTo(originalLogId));
        Assert.That(captured.Logs[0].TourId, Is.EqualTo(captured.Id));
        Assert.That(captured.Logs[0].Tour, Is.Null); // repo will re-hydrate via cascade
    }

    // ----------------------------------------------------------------
    //  Import validation — best-effort behavior
    // ----------------------------------------------------------------

    [Test]
    public async Task ImportAsync_InvalidTour_SkippedButOthersImported()
    {
        var good = ValidTour("Good");
        var bad = ValidTour("Bad");
        bad.TransportType = TransportType.Unknown; // trips ValidateForImport

        _repo.Setup(r => r.AddAsync(It.Is<Tour>(t => t.Name == "Good")))
            .Returns(Task.CompletedTask);

        var summary = await _service.ImportAsync(new[] { good, bad });

        Assert.That(summary.Imported, Is.EqualTo(1));
        Assert.That(summary.Total, Is.EqualTo(2));
        Assert.That(summary.Errors, Has.Count.EqualTo(1));
        Assert.That(summary.Errors[0].Index, Is.EqualTo(1));
        Assert.That(summary.Errors[0].TourName, Is.EqualTo("Bad"));
        Assert.That(summary.Errors[0].Message, Does.Contain("transport"));
        _repo.Verify(r => r.AddAsync(It.IsAny<Tour>()), Times.Once);
    }

    [TestCase("")]
    [TestCase("   ")]
    public async Task ImportAsync_MissingName_Rejected(string emptyName)
    {
        var t = ValidTour();
        t.Name = emptyName;

        var summary = await _service.ImportAsync(new[] { t });

        Assert.That(summary.Imported, Is.EqualTo(0));
        Assert.That(summary.Errors, Has.Count.EqualTo(1));
        Assert.That(summary.Errors[0].Message, Does.Contain("name"));
    }

    [Test]
    public async Task ImportAsync_InvalidRating_Rejected()
    {
        var t = ValidTour();
        var log = ValidLog(t.Id);
        log.Rating = 7; // out of 1..5
        t.Logs.Add(log);

        var summary = await _service.ImportAsync(new[] { t });

        Assert.That(summary.Imported, Is.EqualTo(0));
        Assert.That(summary.Errors[0].Message, Does.Contain("rating"));
    }

    [Test]
    public async Task ImportAsync_NegativeDistance_Rejected()
    {
        var t = ValidTour();
        t.Distance = -1;

        var summary = await _service.ImportAsync(new[] { t });

        Assert.That(summary.Imported, Is.EqualTo(0));
        Assert.That(summary.Errors[0].Message, Does.Contain("distance"));
    }

    [Test]
    public async Task ImportAsync_ForwardsCancellationToken()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.That(
            async () => await _service.ImportAsync(new[] { ValidTour() }, cts.Token),
            Throws.InstanceOf<OperationCanceledException>());
    }
}
