using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using TourPlanner.BusinessLayer.Exceptions;
using TourPlanner.BusinessLayer.Services;
using TourPlanner.DataAccessLayer.Repositories;
using TourPlanner.Domain;

namespace TourPlanner.Tests.Scoping;

/// <summary>
/// Unit tests for the per-user data isolation on <see cref="TourService"/>.
///
/// The service is the boundary that stamps <see cref="Tour.UserId"/> on
/// create/update and forwards every read/write to the repository under the
/// caller's user id. These tests verify:
///   * the repository is always called with the caller's user id (never the
///     request body's UserId — that field cannot be forged),
///   * <see cref="NotFoundException"/> is raised when the caller asks for a
///     tour that belongs to a different user (i.e. the repo returns null),
///   * update / delete flow through the same scoping guard.
/// </summary>
[TestFixture]
public class TourServiceScopingTests
{
    private static readonly Guid Alice = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid Bob = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    private Mock<ITourRepository> _repo = null!;
    private TourService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _repo = new Mock<ITourRepository>(MockBehavior.Strict);
        _service = new TourService(_repo.Object, NullLogger<TourService>.Instance);
    }

    private static Tour AliceTour(string name = "Alice's tour") => new()
    {
        Id = Guid.NewGuid(),
        UserId = Alice,
        Name = name,
        From = "A", To = "B",
        TransportType = TransportType.Cycling,
        Status = TourStatus.Planned,
    };

    // ----------------------------------------------------------------
    //  Read scoping
    // ----------------------------------------------------------------

    [Test]
    public async Task GetAllAsync_ForwardsOwnerIdToRepository()
    {
        _repo.Setup(r => r.GetAllAsync(Alice))
            .ReturnsAsync(new List<Tour> { AliceTour() })
            .Verifiable();

        var result = await _service.GetAllAsync(Alice);

        _repo.Verify();
        Assert.That(result, Has.Count.EqualTo(1));
    }

    [Test]
    public void GetByIdAsync_UnownedTour_ThrowsNotFound()
    {
        // The repository already filters by owner, so it returns null when
        // the tour exists but belongs to somebody else. The service must
        // translate that into NotFound so Bob cannot even learn Alice's id exists.
        var alicesTourId = Guid.NewGuid();
        _repo.Setup(r => r.GetByIdAsync(Bob, alicesTourId))
            .ReturnsAsync((Tour?)null);

        Assert.That(
            async () => await _service.GetByIdAsync(Bob, alicesTourId),
            Throws.TypeOf<NotFoundException>());
    }

    [Test]
    public async Task GetByIdAsync_OwnTour_ReturnsIt()
    {
        var t = AliceTour();
        _repo.Setup(r => r.GetByIdAsync(Alice, t.Id))
            .ReturnsAsync(t);

        var result = await _service.GetByIdAsync(Alice, t.Id);

        Assert.That(result, Is.SameAs(t));
    }

    // ----------------------------------------------------------------
    //  Create — ownership cannot be forged from the request body
    // ----------------------------------------------------------------

    [Test]
    public async Task CreateAsync_ForcesUserId_FromParameterNotBody()
    {
        var incoming = AliceTour();
        incoming.UserId = Bob; // caller tries to smuggle a different owner
        Tour? captured = null;
        _repo.Setup(r => r.AddAsync(It.IsAny<Tour>()))
            .Callback<Tour>(t => captured = t)
            .Returns(Task.CompletedTask);

        await _service.CreateAsync(Alice, incoming);

        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.UserId, Is.EqualTo(Alice));
    }

    // ----------------------------------------------------------------
    //  Update — scoped + ownership pinned again
    // ----------------------------------------------------------------

    [Test]
    public void UpdateAsync_UnownedTour_ThrowsNotFound()
    {
        var alicesTourId = Guid.NewGuid();
        _repo.Setup(r => r.GetByIdAsync(Bob, alicesTourId))
            .ReturnsAsync((Tour?)null);

        Assert.That(
            async () => await _service.UpdateAsync(Bob, alicesTourId, _ => { }),
            Throws.TypeOf<NotFoundException>());

        _repo.Verify(r => r.UpdateAsync(It.IsAny<Tour>()), Times.Never);
    }

    [Test]
    public async Task UpdateAsync_ReassertsOwnership_EvenIfApplyChangesMutatesUserId()
    {
        var t = AliceTour();
        _repo.Setup(r => r.GetByIdAsync(Alice, t.Id)).ReturnsAsync(t);
        _repo.Setup(r => r.UpdateAsync(t)).Returns(Task.CompletedTask);

        await _service.UpdateAsync(Alice, t.Id, entity =>
        {
            entity.UserId = Bob; // malicious change
        });

        Assert.That(t.UserId, Is.EqualTo(Alice));
    }

    // ----------------------------------------------------------------
    //  Delete
    // ----------------------------------------------------------------

    [Test]
    public void DeleteAsync_UnownedTour_ThrowsNotFound()
    {
        var alicesTourId = Guid.NewGuid();
        _repo.Setup(r => r.DeleteAsync(Bob, alicesTourId)).ReturnsAsync(false);

        Assert.That(
            async () => await _service.DeleteAsync(Bob, alicesTourId),
            Throws.TypeOf<NotFoundException>());
    }

    [Test]
    public async Task DeleteAsync_OwnTour_Deletes()
    {
        var id = Guid.NewGuid();
        _repo.Setup(r => r.DeleteAsync(Alice, id)).ReturnsAsync(true);

        await _service.DeleteAsync(Alice, id);

        _repo.Verify(r => r.DeleteAsync(Alice, id), Times.Once);
    }
}
