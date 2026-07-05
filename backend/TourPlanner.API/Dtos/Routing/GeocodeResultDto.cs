namespace TourPlanner.API.Dtos.Routing;

/// <summary>A geographic point returned by the search / geocode endpoints.</summary>
public sealed record GeocodeResultDto(string DisplayName, double Lat, double Lng);
