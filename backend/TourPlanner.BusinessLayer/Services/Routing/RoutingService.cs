using System.Globalization;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TourPlanner.BusinessLayer.Exceptions;

namespace TourPlanner.BusinessLayer.Services.Routing;

/// <summary>
/// Thin adapter around Nominatim (geocoding) and OpenRouteService (routing).
/// Uses a single injected HttpClient — configured via AddHttpClient in Program.cs.
/// </summary>
public sealed class RoutingService : IRoutingService
{
    // ORS transport profile names.
    // Keys are the values the frontend/business layer uses ("driving", "cycling", "walking").
    private static readonly IReadOnlyDictionary<string, string> OrsProfiles =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["driving"] = "driving-car",
            ["cycling"] = "cycling-regular",
            ["walking"] = "foot-walking",
        };

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly HttpClient _http;
    private readonly RoutingOptions _options;
    private readonly ILogger<RoutingService> _logger;

    public RoutingService(
        HttpClient http,
        IOptions<RoutingOptions> options,
        ILogger<RoutingService> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;

        // Nominatim requires a UA per its TOS; ORS doesn't mind.
        if (!_http.DefaultRequestHeaders.UserAgent.Any())
        {
            _http.DefaultRequestHeaders.UserAgent.ParseAdd(_options.UserAgent);
        }
    }

    // ---------------------------------------------------------------------
    // Nominatim: search / geocode / reverse
    // ---------------------------------------------------------------------

    public async Task<IReadOnlyList<GeocodeHit>> SearchAsync(
        string query, int limit, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query)) return Array.Empty<GeocodeHit>();
        limit = Math.Clamp(limit, 1, 20);

        var url = $"{_options.NominatimBaseUrl}/search"
                + $"?format=json&limit={limit}&addressdetails=0"
                + $"&q={Uri.EscapeDataString(query.Trim())}";

        _logger.LogInformation("Nominatim search q='{Query}' limit={Limit}", query, limit);

        var raw = await GetJsonAsync<NominatimSearchHit[]>(url, ct);
        if (raw is null) return Array.Empty<GeocodeHit>();

        return raw
            .Select(r => new GeocodeHit(r.DisplayName, ParseCoord(r.Lat), ParseCoord(r.Lon)))
            .ToList();
    }

    public async Task<GeocodeHit?> GeocodeOneAsync(string query, CancellationToken ct = default)
    {
        var hits = await SearchAsync(query, limit: 1, ct);
        return hits.Count > 0 ? hits[0] : null;
    }

    public async Task<string> ReverseGeocodeAsync(double lat, double lng, CancellationToken ct = default)
    {
        var fallback = $"{lat.ToString("F4", CultureInfo.InvariantCulture)}, " +
                       $"{lng.ToString("F4", CultureInfo.InvariantCulture)}";

        var url = $"{_options.NominatimBaseUrl}/reverse"
                + $"?format=json&zoom=18&addressdetails=1"
                + $"&lat={lat.ToString(CultureInfo.InvariantCulture)}"
                + $"&lon={lng.ToString(CultureInfo.InvariantCulture)}";

        _logger.LogInformation("Nominatim reverse lat={Lat} lng={Lng}", lat, lng);

        var raw = await GetJsonAsync<NominatimReverseResult>(url, ct);
        if (raw is null) return fallback;

        // Prefer a compact street/city/country address; fall back to display_name.
        var a = raw.Address ?? new Dictionary<string, string>();
        var street = new[] { a.GetValueOrDefault("road"), a.GetValueOrDefault("house_number") }
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToArray();
        var locality = a.GetValueOrDefault("city")
                    ?? a.GetValueOrDefault("town")
                    ?? a.GetValueOrDefault("village")
                    ?? a.GetValueOrDefault("municipality")
                    ?? a.GetValueOrDefault("hamlet")
                    ?? a.GetValueOrDefault("suburb");
        var postcode = a.GetValueOrDefault("postcode");
        var country = a.GetValueOrDefault("country");

        var parts = new List<string>();
        if (street.Length > 0) parts.Add(string.Join(' ', street));
        if (postcode is not null || locality is not null)
            parts.Add(string.Join(' ', new[] { postcode, locality }.Where(s => !string.IsNullOrWhiteSpace(s))));
        if (!string.IsNullOrWhiteSpace(country)) parts.Add(country);

        if (parts.Count > 0) return string.Join(", ", parts);
        return string.IsNullOrWhiteSpace(raw.DisplayName) ? fallback : raw.DisplayName.Trim();
    }

    // ---------------------------------------------------------------------
    // OpenRouteService: routing
    // ---------------------------------------------------------------------

    public async Task<RouteInfo?> RouteAsync(
        Coord from, Coord to, string transportType, CancellationToken ct = default)
    {
        if (!OrsProfiles.TryGetValue(transportType, out var profile))
        {
            throw new ValidationException(
                $"Unknown transport type '{transportType}'. Expected: driving, cycling or walking.");
        }

        if (string.IsNullOrWhiteSpace(_options.OpenRouteServiceApiKey))
        {
            // Server misconfiguration — surfaces as 500 to the client, which is correct.
            throw new InvalidOperationException(
                "OpenRouteService API key is not configured. " +
                "Set it via `dotnet user-secrets set \"Routing:OpenRouteServiceApiKey\" \"<your-key>\"` " +
                "in the TourPlanner.API project.");
        }

        // ORS wants [lng, lat] coordinate order.
        var body = new
        {
            coordinates = new[]
            {
                new[] { from.Lng, from.Lat },
                new[] { to.Lng, to.Lat },
            },
        };

        var url = $"{_options.OpenRouteServiceBaseUrl}/v2/directions/{profile}/geojson";
        using var req = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(body, options: JsonOpts),
        };
        // ORS keys are base64 (may contain '=' / '+' / '/'), so bypass token validation.
        req.Headers.TryAddWithoutValidation("Authorization", _options.OpenRouteServiceApiKey);
        req.Headers.Accept.ParseAdd("application/geo+json,application/json");

        _logger.LogInformation(
            "ORS route profile={Profile} from={FromLat},{FromLng} to={ToLat},{ToLng}",
            profile, from.Lat, from.Lng, to.Lat, to.Lng);

        try
        {
            using var res = await _http.SendAsync(req, ct);
            if (!res.IsSuccessStatusCode)
            {
                var errBody = await res.Content.ReadAsStringAsync(ct);
                _logger.LogWarning(
                    "ORS returned {Status}: {Body}",
                    (int)res.StatusCode, Truncate(errBody, 400));
                return null;
            }

            await using var stream = await res.Content.ReadAsStreamAsync(ct);
            var geoJson = await JsonSerializer.DeserializeAsync<OrsGeoJsonResponse>(stream, JsonOpts, ct);
            return MapOrsResponse(geoJson);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "ORS request failed");
            return null;
        }
    }

    private static RouteInfo? MapOrsResponse(OrsGeoJsonResponse? geoJson)
    {
        var feature = geoJson?.Features?.FirstOrDefault();
        var summary = feature?.Properties?.Summary;
        if (feature is null || summary is null) return null;

        var distanceKm = Math.Round(summary.Distance / 1000.0, 1);
        var durationMinutes = (int)Math.Round(summary.Duration / 60.0);

        // GeoJSON is [lng, lat]; convert to (Lat, Lng) for Leaflet.
        var path = (feature.Geometry?.Coordinates ?? Array.Empty<double[]>())
            .Where(c => c.Length >= 2)
            .Select(c => new Coord(c[1], c[0]))
            .ToList();

        return new RouteInfo(distanceKm, durationMinutes, FormatDuration(durationMinutes), path);
    }

    private static string FormatDuration(int totalMinutes)
    {
        var h = totalMinutes / 60;
        var m = totalMinutes % 60;
        return $"{h}h {m:D2}m";
    }

    // ---------------------------------------------------------------------
    // helpers
    // ---------------------------------------------------------------------

    private async Task<T?> GetJsonAsync<T>(string url, CancellationToken ct)
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Accept.ParseAdd("application/json");

            using var res = await _http.SendAsync(req, ct);
            if (!res.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "External call {Url} returned {Status}", url, (int)res.StatusCode);
                return default;
            }
            await using var stream = await res.Content.ReadAsStreamAsync(ct);
            return await JsonSerializer.DeserializeAsync<T>(stream, JsonOpts, ct);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "External call {Url} failed", url);
            return default;
        }
    }

    private static double ParseCoord(string s) =>
        double.Parse(s, CultureInfo.InvariantCulture);

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s[..max] + "...";

    // ---------------------------------------------------------------------
    // Nested DTO shapes for the JSON we consume (kept private to this file
    // so they don't leak out as public API).
    // ---------------------------------------------------------------------

    private sealed record NominatimSearchHit(
        [property: JsonPropertyName("display_name")] string DisplayName,
        [property: JsonPropertyName("lat")] string Lat,
        [property: JsonPropertyName("lon")] string Lon);

    private sealed record NominatimReverseResult(
        [property: JsonPropertyName("display_name")] string? DisplayName,
        [property: JsonPropertyName("address")] Dictionary<string, string>? Address);

    private sealed record OrsGeoJsonResponse(
        [property: JsonPropertyName("features")] List<OrsFeature>? Features);

    private sealed record OrsFeature(
        [property: JsonPropertyName("geometry")] OrsGeometry? Geometry,
        [property: JsonPropertyName("properties")] OrsProperties? Properties);

    private sealed record OrsGeometry(
        [property: JsonPropertyName("coordinates")] double[][]? Coordinates);

    private sealed record OrsProperties(
        [property: JsonPropertyName("summary")] OrsSummary? Summary);

    private sealed record OrsSummary(
        [property: JsonPropertyName("distance")] double Distance,
        [property: JsonPropertyName("duration")] double Duration);
}
