using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;
using TourPlanner.API.Controllers;
using TourPlanner.API.Dtos.Routing;
using TourPlanner.BusinessLayer.Services.Routing;

namespace TourPlanner.Tests.Routing;

/// <summary>
/// Unit tests for <see cref="RoutingController"/>. The service layer is mocked
/// with Moq so these tests only exercise DTO mapping, status codes,
/// query defaults, and ModelState handling.
/// </summary>
[TestFixture]
public sealed class RoutingControllerTests
{
    private Mock<IRoutingService> _service = null!;
    private RoutingController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _service = new Mock<IRoutingService>(MockBehavior.Strict);
        _controller = new RoutingController(_service.Object, NullLogger<RoutingController>.Instance);
    }

    // -----------------------------------------------------------------
    // GET /search
    // -----------------------------------------------------------------

    [Test]
    public async Task Search_MapsHitsToDtos_AndPassesDefaultLimit()
    {
        _service.Setup(s => s.SearchAsync("Vienna", 6, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<GeocodeHit>
            {
                new("Vienna, Austria", 48.2, 16.3),
                new("Wien Meidling", 48.18, 16.33),
            });

        var actionResult = await _controller.Search("Vienna");

        var ok = actionResult.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        var dtos = ok!.Value as IReadOnlyList<GeocodeResultDto>;
        Assert.That(dtos, Is.Not.Null);
        Assert.That(dtos, Has.Count.EqualTo(2));
        Assert.That(dtos![0].DisplayName, Is.EqualTo("Vienna, Austria"));
        Assert.That(dtos[0].Lat, Is.EqualTo(48.2));
        Assert.That(dtos[0].Lng, Is.EqualTo(16.3));
        _service.VerifyAll();
    }

    [Test]
    public async Task Search_ForwardsCustomLimit()
    {
        _service.Setup(s => s.SearchAsync("q", 15, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<GeocodeHit>());

        await _controller.Search("q", limit: 15);

        _service.VerifyAll();
    }

    [Test]
    public async Task Search_NullQuery_ForwardsEmptyStringToService()
    {
        _service.Setup(s => s.SearchAsync(string.Empty, 6, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<GeocodeHit>());

        await _controller.Search(q: null!);

        _service.VerifyAll();
    }

    // -----------------------------------------------------------------
    // GET /geocode
    // -----------------------------------------------------------------

    [Test]
    public async Task Geocode_HitFound_Returns200WithDto()
    {
        _service.Setup(s => s.GeocodeOneAsync("Salzburg", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GeocodeHit("Salzburg, Austria", 47.8, 13.05));

        var result = await _controller.Geocode("Salzburg");

        Assert.That(result.Result, Is.Null.Or.Not.InstanceOf<NotFoundResult>());
        // ActionResult<T> with an implicit T return sits on .Value, not .Result.
        var dto = result.Value ?? (result.Result as ObjectResult)?.Value as GeocodeResultDto;
        Assert.That(dto, Is.Not.Null);
        Assert.That(dto!.DisplayName, Is.EqualTo("Salzburg, Austria"));
        Assert.That(dto.Lat, Is.EqualTo(47.8));
        Assert.That(dto.Lng, Is.EqualTo(13.05));
    }

    [Test]
    public async Task Geocode_NoHit_Returns404()
    {
        _service.Setup(s => s.GeocodeOneAsync("Nowhere", It.IsAny<CancellationToken>()))
            .ReturnsAsync((GeocodeHit?)null);

        var result = await _controller.Geocode("Nowhere");

        Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
    }

    // -----------------------------------------------------------------
    // GET /reverse
    // -----------------------------------------------------------------

    [Test]
    public async Task Reverse_ReturnsAnonymousDisplayName()
    {
        _service.Setup(s => s.ReverseGeocodeAsync(48.2082, 16.3738, It.IsAny<CancellationToken>()))
            .ReturnsAsync("1010 Wien, Österreich");

        var result = await _controller.Reverse(48.2082, 16.3738);

        var value = result.Value;
        Assert.That(value, Is.Not.Null);
        var displayName = value!.GetType().GetProperty("displayName",
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)?.GetValue(value) as string;
        Assert.That(displayName, Is.EqualTo("1010 Wien, Österreich"));
    }

    // -----------------------------------------------------------------
    // POST /route
    // -----------------------------------------------------------------

    [Test]
    public async Task Route_HappyPath_MapsRouteInfoToDto()
    {
        var path = new List<Coord> { new(48.2, 16.3), new(47.8, 13.05) };
        _service.Setup(s => s.RouteAsync(
                It.Is<Coord>(c => c.Lat == 48.2 && c.Lng == 16.3),
                It.Is<Coord>(c => c.Lat == 47.8 && c.Lng == 13.05),
                "driving",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RouteInfo(295.7, 181, "3h 01m", path));

        var req = new RouteRequestDto
        {
            From = new CoordinateDto { Lat = 48.2, Lng = 16.3 },
            To = new CoordinateDto { Lat = 47.8, Lng = 13.05 },
            TransportType = "driving",
        };

        var result = await _controller.Route(req);

        var ok = result.Result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        var dto = ok!.Value as RouteResultDto;
        Assert.That(dto, Is.Not.Null);
        Assert.That(dto!.DistanceKm, Is.EqualTo(295.7));
        Assert.That(dto.DurationMinutes, Is.EqualTo(181));
        Assert.That(dto.DurationLabel, Is.EqualTo("3h 01m"));
        Assert.That(dto.Path, Has.Count.EqualTo(2));
        Assert.That(dto.Path[0].Lat, Is.EqualTo(48.2));
        Assert.That(dto.Path[0].Lng, Is.EqualTo(16.3));
        _service.VerifyAll();
    }

    [Test]
    public async Task Route_ServiceReturnsNull_Returns404()
    {
        _service.Setup(s => s.RouteAsync(
                It.IsAny<Coord>(), It.IsAny<Coord>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RouteInfo?)null);

        var req = new RouteRequestDto
        {
            From = new CoordinateDto { Lat = 0, Lng = 0 },
            To = new CoordinateDto { Lat = 0.001, Lng = 0.001 },
            TransportType = "driving",
        };

        var result = await _controller.Route(req);

        Assert.That(result.Result, Is.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task Route_InvalidModelState_ReturnsValidationProblem()
    {
        _controller.ModelState.AddModelError("TransportType", "required");

        var req = new RouteRequestDto
        {
            From = new CoordinateDto { Lat = 0, Lng = 0 },
            To = new CoordinateDto { Lat = 1, Lng = 1 },
            TransportType = "driving",
        };

        var result = await _controller.Route(req);

        Assert.That(result.Result, Is.InstanceOf<ObjectResult>());
        var problem = ((ObjectResult)result.Result!).Value as ValidationProblemDetails;
        Assert.That(problem, Is.Not.Null);
        Assert.That(problem!.Errors, Contains.Key("TransportType"));
        _service.VerifyNoOtherCalls();
    }
}
