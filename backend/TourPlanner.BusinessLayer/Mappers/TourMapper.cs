using TourPlanner.BusinessLayer.Dtos.Tours;
using TourPlanner.Domain;

namespace TourPlanner.BusinessLayer.Mappers;

/// <summary>
/// Manual mapper between Tour entity and Tour DTOs.
/// Handles unit conversion (entity: meters/seconds, DTO: km/minutes)
/// and enum-to-string conversion (entity: enum, DTO: lowercase string).
/// </summary>
public static class TourMapper
{
    public static TourDto ToDto(Tour entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Description = entity.Description,
        From = entity.From,
        To = entity.To,
        TransportType = entity.TransportType.ToString().ToLowerInvariant(),
        Distance = Math.Round(entity.Distance / 1000.0, 2),  // meters → km
        Duration = entity.Duration / 60,                      // seconds → minutes
        Status = entity.Status.ToString().ToLowerInvariant(),
        Color = entity.Color,
        ImageUrl = entity.ImageUrl,
    };

    public static Tour FromCreateDto(CreateTourDto dto) => new()
    {
        Id = Guid.NewGuid(),
        Name = dto.Name,
        Description = dto.Description,
        From = dto.From,
        To = dto.To,
        TransportType = ParseTransportType(dto.TransportType),
        Distance = dto.Distance * 1000.0,     // km → meters
        Duration = dto.Duration * 60,         // minutes → seconds
        Status = TourStatus.Planned,          // new tours start as "planned"
        Color = dto.Color,
        ImageUrl = dto.ImageUrl,
    };

    /// <summary>Updates an existing tracked entity from a DTO (in-place).</summary>
    public static void ApplyUpdate(Tour entity, UpdateTourDto dto)
    {
        entity.Name = dto.Name;
        entity.Description = dto.Description;
        entity.From = dto.From;
        entity.To = dto.To;
        entity.TransportType = ParseTransportType(dto.TransportType);
        entity.Distance = dto.Distance * 1000.0;
        entity.Duration = dto.Duration * 60;
        entity.Status = ParseStatus(dto.Status);
        entity.Color = dto.Color;
        entity.ImageUrl = dto.ImageUrl;
    }

    private static TransportType ParseTransportType(string value) =>
        Enum.TryParse<TransportType>(value, ignoreCase: true, out var result) && result != TransportType.Unknown
            ? result
            : throw new ArgumentException($"Invalid transport type: '{value}'.");

    private static TourStatus ParseStatus(string value) =>
        Enum.TryParse<TourStatus>(value, ignoreCase: true, out var result) && result != TourStatus.Unknown
            ? result
            : throw new ArgumentException($"Invalid status: '{value}'.");
}
