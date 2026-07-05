using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using TourPlanner.API.Controllers;
using TourPlanner.API.Dtos.Tours;
using TourPlanner.BusinessLayer.Services;
using TourPlanner.BusinessLayer.Services.ImportExport;
using TourPlanner.Domain;

namespace TourPlanner.Tests.Search;

/// <summary>
/// Unit tests for the search-related actions on <see cref="ToursController"/>.
/// The service is mocked so these tests only verify DTO mapping, query-string
/// parameter handling, and that the controller correctly extracts the user id
/// from the JWT <c>sub</c> claim and forwards it to the service.
/// </summary>
[TestFixture]
public sealed class ToursControllerSearchTests
{
    private static readonly Guid Owner = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private Mock<ITourService> _service = null!;
    private Mock<ITourImportExportService> _importExport = null!;
    private ToursController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _service = new Mock<ITourService>(MockBehavior.Strict);
        _importExport = new Mock<ITourImportExportService>(MockBehavior.Strict);
        _controller = new ToursController(
            _service.Object,
            _importExport.Object,
            NullLogger<ToursController>.Instance);

        // Fake a signed-in user: the controller reads the JWT 'sub' claim.
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, Owner.ToString()),
        }, authenticationType: "Test");
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) },
        };
    }

    private static Tour SampleTour(string name = "Wachau valley") => new()
    {
        Id = Guid.NewGuid(),
        UserId = Owner,
        Name = name,
        Description = "A ride along the Danube",
        From = "Melk",
        To = "Krems",
        TransportType = TransportType.Cycling,
        Status = TourStatus.Planned,
        Distance = 35_000,
        Duration = 3_600,
    };

    [Test]
    public async Task Search_ReturnsOkWithMappedResults()
    {
        var tour = SampleTour();
        var log = new TourLog
        {
            Id = Guid.NewGuid(),
            TourId = tour.Id,
            LoggedAt = DateTime.UtcNow,
            Comment = "Sunny day",
            Difficulty = Difficulty.Easy,
            TotalDistance = 20_000,
            Duration = TimeSpan.FromMinutes(60),
            Rating = 4,
        };
        _service.Setup(s => s.SearchAsync(Owner, "Danube", 25, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TourSearchResult>
            {
                new(tour, MatchedInTour: true, MatchedLogs: new[] { log }),
            });

        var action = await _controller.Search("Danube", 25, CancellationToken.None);

        var ok = action.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        var payload = ok!.Value as List<TourSearchResultDto>;
        Assert.That(payload, Is.Not.Null);
        Assert.That(payload!, Has.Count.EqualTo(1));
        Assert.That(payload[0].Tour.Id, Is.EqualTo(tour.Id));
        Assert.That(payload[0].Tour.Name, Is.EqualTo("Wachau valley"));
        Assert.That(payload[0].MatchedInTour, Is.True);
        Assert.That(payload[0].MatchedLogs, Has.Count.EqualTo(1));
        Assert.That(payload[0].MatchedLogs[0].TourName, Is.EqualTo("Wachau valley"));
    }

    [Test]
    public async Task Search_EmptyServiceResult_ReturnsOkEmptyList()
    {
        _service.Setup(s => s.SearchAsync(Owner, It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TourSearchResult>());

        var action = await _controller.Search("nothing-matches", 50, CancellationToken.None);

        var ok = action.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        Assert.That(ok!.Value, Is.InstanceOf<List<TourSearchResultDto>>());
        Assert.That((List<TourSearchResultDto>)ok.Value!, Is.Empty);
    }

    [Test]
    public async Task Search_NullQueryString_ForwardsEmptyStringToService()
    {
        _service.Setup(s => s.SearchAsync(Owner, "", 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TourSearchResult>())
            .Verifiable();

        await _controller.Search(q: null!, limit: 10, ct: CancellationToken.None);

        _service.Verify();
    }

    [Test]
    public async Task Search_ForwardsLimitAndCancellationToService()
    {
        using var cts = new CancellationTokenSource();
        _service.Setup(s => s.SearchAsync(Owner, "hello", 12, cts.Token))
            .ReturnsAsync(new List<TourSearchResult>())
            .Verifiable();

        await _controller.Search("hello", 12, cts.Token);

        _service.Verify();
    }
}
