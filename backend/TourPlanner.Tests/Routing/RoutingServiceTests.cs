using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using TourPlanner.BusinessLayer.Exceptions;
using TourPlanner.BusinessLayer.Services.Routing;

namespace TourPlanner.Tests.Routing;

/// <summary>
/// Unit tests for <see cref="RoutingService"/> — the layer that talks to
/// Nominatim and OpenRouteService. HTTP is intercepted via
/// <see cref="StubHttpMessageHandler"/>; nothing hits the network.
/// </summary>
[TestFixture]
public sealed class RoutingServiceTests
{
    private const string NominatimBase = "https://nominatim.test";
    private const string OrsBase = "https://ors.test";
    private const string ApiKey = "eyJvcmciOiJ0ZXN0In0="; // base64 with '=' to exercise the header fix
    private const string UserAgent = "TourPlanner.Tests/1.0";

    private StubHttpMessageHandler _handler = null!;
    private HttpClient _http = null!;
    private RoutingService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _handler = new StubHttpMessageHandler();
        _http = new HttpClient(_handler);
        _service = BuildService(_http);
    }

    [TearDown]
    public void TearDown() => _http.Dispose();

    private static RoutingService BuildService(HttpClient http, string apiKey = ApiKey)
    {
        var options = Options.Create(new RoutingOptions
        {
            NominatimBaseUrl = NominatimBase,
            OpenRouteServiceBaseUrl = OrsBase,
            OpenRouteServiceApiKey = apiKey,
            UserAgent = UserAgent,
        });
        return new RoutingService(http, options, NullLogger<RoutingService>.Instance);
    }

    // -----------------------------------------------------------------
    // SearchAsync
    // -----------------------------------------------------------------

    [Test]
    public async Task SearchAsync_EmptyQuery_ReturnsEmptyAndSkipsHttp()
    {
        var result = await _service.SearchAsync("", limit: 5);

        Assert.That(result, Is.Empty);
        Assert.That(_handler.Requests, Is.Empty, "must not call Nominatim for an empty query");
    }

    [Test]
    public async Task SearchAsync_WhitespaceQuery_ReturnsEmpty()
    {
        var result = await _service.SearchAsync("   ", limit: 5);

        Assert.That(result, Is.Empty);
        Assert.That(_handler.Requests, Is.Empty);
    }

    [Test]
    public async Task SearchAsync_HappyPath_MapsHits()
    {
        _handler.WhenUrlContains("/search", HttpStatusCode.OK, """
            [
                {"display_name":"Vienna, Austria","lat":"48.2082","lon":"16.3738"},
                {"display_name":"Salzburg, Austria","lat":"47.8095","lon":"13.0550"}
            ]
            """);

        var hits = await _service.SearchAsync("Vienna", limit: 5);

        Assert.That(hits, Has.Count.EqualTo(2));
        Assert.That(hits[0].DisplayName, Is.EqualTo("Vienna, Austria"));
        Assert.That(hits[0].Lat, Is.EqualTo(48.2082).Within(0.0001));
        Assert.That(hits[0].Lng, Is.EqualTo(16.3738).Within(0.0001));
        Assert.That(hits[1].DisplayName, Is.EqualTo("Salzburg, Austria"));
    }

    [Test]
    public async Task SearchAsync_ClampsLimitToUpperBound()
    {
        _handler.WhenUrlContains("/search", HttpStatusCode.OK, "[]");

        await _service.SearchAsync("q", limit: 999);

        Assert.That(_handler.Requests.Single().RequestUri!.ToString(), Does.Contain("limit=20"));
    }

    [Test]
    public async Task SearchAsync_ClampsLimitToLowerBound()
    {
        _handler.WhenUrlContains("/search", HttpStatusCode.OK, "[]");

        await _service.SearchAsync("q", limit: -3);

        Assert.That(_handler.Requests.Single().RequestUri!.ToString(), Does.Contain("limit=1"));
    }

    [Test]
    public async Task SearchAsync_EscapesQueryString()
    {
        _handler.WhenUrlContains("/search", HttpStatusCode.OK, "[]");

        await _service.SearchAsync("Wien, Österreich", limit: 3);

        // Use AbsoluteUri — Uri.ToString() unescapes "safe" characters for display.
        var url = _handler.Requests.Single().RequestUri!.AbsoluteUri;
        Assert.That(url, Does.Contain("q=Wien%2C%20%C3%96sterreich"));
    }

    [Test]
    public async Task SearchAsync_SendsUserAgentHeader()
    {
        _handler.WhenUrlContains("/search", HttpStatusCode.OK, "[]");

        await _service.SearchAsync("q", limit: 1);

        var ua = _handler.Requests.Single().Headers.UserAgent.ToString();
        Assert.That(ua, Does.Contain("TourPlanner.Tests"));
    }

    [Test]
    public async Task SearchAsync_ServerError_ReturnsEmpty()
    {
        _handler.WhenUrlContains("/search", HttpStatusCode.InternalServerError, "");

        var result = await _service.SearchAsync("q", limit: 1);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task SearchAsync_NetworkFailure_ReturnsEmpty()
    {
        _handler.Throw = new HttpRequestException("network down");

        var result = await _service.SearchAsync("q", limit: 1);

        Assert.That(result, Is.Empty);
    }

    // -----------------------------------------------------------------
    // GeocodeOneAsync
    // -----------------------------------------------------------------

    [Test]
    public async Task GeocodeOneAsync_ReturnsFirstHit()
    {
        _handler.WhenUrlContains("/search", HttpStatusCode.OK, """
            [{"display_name":"Salzburg","lat":"47.8","lon":"13.05"}]
            """);

        var hit = await _service.GeocodeOneAsync("Salzburg");

        Assert.That(hit, Is.Not.Null);
        Assert.That(hit!.DisplayName, Is.EqualTo("Salzburg"));
        Assert.That(hit.Lat, Is.EqualTo(47.8).Within(0.001));
        Assert.That(_handler.Requests.Single().RequestUri!.ToString(), Does.Contain("limit=1"));
    }

    [Test]
    public async Task GeocodeOneAsync_NoHits_ReturnsNull()
    {
        _handler.WhenUrlContains("/search", HttpStatusCode.OK, "[]");

        var hit = await _service.GeocodeOneAsync("Nowhere");

        Assert.That(hit, Is.Null);
    }

    // -----------------------------------------------------------------
    // ReverseGeocodeAsync
    // -----------------------------------------------------------------

    [Test]
    public async Task ReverseGeocodeAsync_BuildsCompactAddressFromParts()
    {
        _handler.WhenUrlContains("/reverse", HttpStatusCode.OK, """
            {
                "display_name":"1, Stephansplatz, Wien, Österreich",
                "address":{
                    "road":"Stephansplatz",
                    "house_number":"1",
                    "postcode":"1010",
                    "city":"Wien",
                    "country":"Österreich"
                }
            }
            """);

        var label = await _service.ReverseGeocodeAsync(48.2082, 16.3738);

        Assert.That(label, Is.EqualTo("Stephansplatz 1, 1010 Wien, Österreich"));
    }

    [Test]
    public async Task ReverseGeocodeAsync_UsesDisplayNameWhenAddressMissing()
    {
        _handler.WhenUrlContains("/reverse", HttpStatusCode.OK, """
            {"display_name":"Somewhere in Austria"}
            """);

        var label = await _service.ReverseGeocodeAsync(48.0, 16.0);

        Assert.That(label, Is.EqualTo("Somewhere in Austria"));
    }

    [Test]
    public async Task ReverseGeocodeAsync_ServerError_FallsBackToCoords()
    {
        _handler.WhenUrlContains("/reverse", HttpStatusCode.BadGateway, "");

        var label = await _service.ReverseGeocodeAsync(48.2082, 16.3738);

        Assert.That(label, Is.EqualTo("48.2082, 16.3738"));
    }

    [Test]
    public async Task ReverseGeocodeAsync_SendsLatLonQuery()
    {
        _handler.WhenUrlContains("/reverse", HttpStatusCode.OK, "{}");

        await _service.ReverseGeocodeAsync(48.2082, 16.3738);

        var url = _handler.Requests.Single().RequestUri!.ToString();
        Assert.That(url, Does.Contain("lat=48.2082"));
        Assert.That(url, Does.Contain("lon=16.3738"));
    }

    // -----------------------------------------------------------------
    // RouteAsync
    // -----------------------------------------------------------------

    [Test]
    public void RouteAsync_UnknownTransport_ThrowsValidation()
    {
        Assert.ThrowsAsync<ValidationException>(async () =>
            await _service.RouteAsync(new Coord(0, 0), new Coord(1, 1), "teleport"));
        Assert.That(_handler.Requests, Is.Empty);
    }

    [Test]
    public void RouteAsync_MissingApiKey_ThrowsInvalidOperation()
    {
        var svc = BuildService(_http, apiKey: "");

        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await svc.RouteAsync(new Coord(0, 0), new Coord(1, 1), "driving"));
        Assert.That(_handler.Requests, Is.Empty);
    }

    [Test]
    public async Task RouteAsync_HappyPath_ParsesDistanceDurationAndFlipsPath()
    {
        _handler.WhenUrlContains("/v2/directions/driving-car/geojson", HttpStatusCode.OK, """
            {
                "features":[{
                    "geometry":{"coordinates":[[16.3738,48.2082],[13.0550,47.8095]]},
                    "properties":{"summary":{"distance":295678.4,"duration":10860}}
                }]
            }
            """);

        var route = await _service.RouteAsync(
            new Coord(48.2082, 16.3738), new Coord(47.8095, 13.0550), "driving");

        Assert.That(route, Is.Not.Null);
        Assert.That(route!.DistanceKm, Is.EqualTo(295.7).Within(0.05));
        Assert.That(route.DurationMinutes, Is.EqualTo(181));
        Assert.That(route.DurationLabel, Is.EqualTo("3h 01m"));
        Assert.That(route.Path, Has.Count.EqualTo(2));
        // Response was [lng,lat] — service must flip back to (Lat, Lng)
        Assert.That(route.Path[0].Lat, Is.EqualTo(48.2082).Within(0.0001));
        Assert.That(route.Path[0].Lng, Is.EqualTo(16.3738).Within(0.0001));
    }

    [Test]
    public async Task RouteAsync_SendsAuthorizationHeaderWithBase64Key()
    {
        _handler.WhenUrlContains("/v2/directions/", HttpStatusCode.OK, """
            {"features":[{"geometry":{"coordinates":[]},"properties":{"summary":{"distance":0,"duration":0}}}]}
            """);

        await _service.RouteAsync(new Coord(0, 0), new Coord(1, 1), "driving");

        var authHeader = _handler.Requests.Single().Headers.GetValues("Authorization").Single();
        Assert.That(authHeader, Is.EqualTo(ApiKey),
            "the base64 key (which contains '=') must be sent verbatim without token validation");
    }

    [Test]
    public async Task RouteAsync_PostsCoordinatesInLngLatOrder()
    {
        _handler.WhenUrlContains("/v2/directions/", HttpStatusCode.OK, """
            {"features":[{"geometry":{"coordinates":[]},"properties":{"summary":{"distance":0,"duration":0}}}]}
            """);

        await _service.RouteAsync(
            new Coord(48.2082, 16.3738), new Coord(47.8095, 13.0550), "driving");

        var body = _handler.RequestBodies.Single();
        Assert.That(body, Is.Not.Null.And.Not.Empty);
        using var doc = JsonDocument.Parse(body!);
        var coords = doc.RootElement.GetProperty("coordinates");
        Assert.That(coords.GetArrayLength(), Is.EqualTo(2));
        // first coord = [lng, lat] = [16.3738, 48.2082]
        Assert.That(coords[0][0].GetDouble(), Is.EqualTo(16.3738).Within(0.0001));
        Assert.That(coords[0][1].GetDouble(), Is.EqualTo(48.2082).Within(0.0001));
        Assert.That(coords[1][0].GetDouble(), Is.EqualTo(13.0550).Within(0.0001));
        Assert.That(coords[1][1].GetDouble(), Is.EqualTo(47.8095).Within(0.0001));
    }

    [TestCase("driving", "driving-car")]
    [TestCase("cycling", "cycling-regular")]
    [TestCase("walking", "foot-walking")]
    public async Task RouteAsync_MapsTransportToOrsProfileInUrl(string transport, string profile)
    {
        _handler.WhenUrlContains($"/{profile}/geojson", HttpStatusCode.OK, """
            {"features":[{"geometry":{"coordinates":[]},"properties":{"summary":{"distance":0,"duration":0}}}]}
            """);

        await _service.RouteAsync(new Coord(0, 0), new Coord(1, 1), transport);

        Assert.That(_handler.Requests.Single().RequestUri!.ToString(), Does.Contain(profile));
    }

    [Test]
    public async Task RouteAsync_TransportTypeIsCaseInsensitive()
    {
        _handler.WhenUrlContains("/v2/directions/driving-car/", HttpStatusCode.OK, """
            {"features":[{"geometry":{"coordinates":[]},"properties":{"summary":{"distance":0,"duration":0}}}]}
            """);

        var route = await _service.RouteAsync(new Coord(0, 0), new Coord(1, 1), "DRIVING");

        Assert.That(route, Is.Not.Null);
    }

    [Test]
    public async Task RouteAsync_UpstreamError_ReturnsNull()
    {
        _handler.WhenUrlContains("/v2/directions/", HttpStatusCode.BadRequest, """
            {"error":{"code":2010,"message":"Could not find routable point"}}
            """);

        var route = await _service.RouteAsync(new Coord(0, 0), new Coord(0.001, 0.001), "driving");

        Assert.That(route, Is.Null);
    }

    [Test]
    public async Task RouteAsync_NetworkFailure_ReturnsNull()
    {
        _handler.Throw = new HttpRequestException("timeout");

        var route = await _service.RouteAsync(new Coord(0, 0), new Coord(1, 1), "driving");

        Assert.That(route, Is.Null);
    }

    [Test]
    public async Task RouteAsync_EmptyFeatureList_ReturnsNull()
    {
        _handler.WhenUrlContains("/v2/directions/", HttpStatusCode.OK, """
            {"features":[]}
            """);

        var route = await _service.RouteAsync(new Coord(0, 0), new Coord(1, 1), "driving");

        Assert.That(route, Is.Null);
    }

    [TestCase(0, "0h 00m")]
    [TestCase(59, "0h 59m")]
    [TestCase(60, "1h 00m")]
    [TestCase(125, "2h 05m")]
    [TestCase(3661, "61h 01m")]
    public async Task RouteAsync_FormatsDurationAsHoursMinutes(int minutes, string expected)
    {
        // RoutingService rounds seconds→minutes, so pass minutes*60 as the ORS duration.
        var seconds = minutes * 60;
        var payload =
            "{\"features\":[{" +
            "\"geometry\":{\"coordinates\":[]}," +
            "\"properties\":{\"summary\":{\"distance\":0,\"duration\":" + seconds + "}}" +
            "}]}";
        _handler.WhenUrlContains("/v2/directions/", HttpStatusCode.OK, payload);

        var route = await _service.RouteAsync(new Coord(0, 0), new Coord(1, 1), "driving");

        Assert.That(route, Is.Not.Null);
        Assert.That(route!.DurationLabel, Is.EqualTo(expected));
    }
}
