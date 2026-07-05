namespace TourPlanner.BusinessLayer.Services.Routing;

/// <summary>
/// Configuration for the external map / routing providers.
/// Populated from the "Routing" section of appsettings + user-secrets.
/// The API key is a secret and MUST come from user-secrets (never checked in).
/// </summary>
public sealed class RoutingOptions
{
    public const string SectionName = "Routing";

    /// <summary>Base URL of the Nominatim geocoder.</summary>
    public string NominatimBaseUrl { get; set; } = "https://nominatim.openstreetmap.org";

    /// <summary>Base URL of OpenRouteService (v2 API).</summary>
    public string OpenRouteServiceBaseUrl { get; set; } = "https://api.openrouteservice.org";

    /// <summary>API key for OpenRouteService. Required at runtime.</summary>
    public string OpenRouteServiceApiKey { get; set; } = string.Empty;

    /// <summary>
    /// User-Agent header for outbound requests. Nominatim's TOS requires
    /// identifying the application; ORS is happy with anything reasonable.
    /// </summary>
    public string UserAgent { get; set; } = "TourPlanner/1.0 (university project)";
}
