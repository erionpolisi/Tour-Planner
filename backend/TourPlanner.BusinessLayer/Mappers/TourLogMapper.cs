using TourPlanner.BusinessLayer.Dtos.TourLogs;
using TourPlanner.Domain;

namespace TourPlanner.BusinessLayer.Mappers;

public static class TourLogMapper
{
    /// <summary>
    /// Maps a log entity to a DTO. Requires the parent tour name to be supplied
    /// separately, because the entity itself doesn't carry the name redundantly.
    /// </summary>
    public static TourLogDto ToDto(TourLog entity, string tourName) => new()
    {
        Id = entity.Id,
        TourId = entity.TourId,
        TourName = tourName,
        LoggedAt = entity.LoggedAt,
        Comment = entity.Comment,
        Difficulty = entity.Difficulty.ToString().ToLowerInvariant(),
        TotalDistance = Math.Round(entity.TotalDistance / 1000.0, 2),  // m → km
        Duration = (int)entity.Duration.TotalMinutes,                   // TimeSpan → minutes
        Rating = entity.Rating,
    };

    public static TourLog FromCreateDto(CreateTourLogDto dto) => new()
    {
        Id = Guid.NewGuid(),
        TourId = dto.TourId,
        LoggedAt = dto.LoggedAt.ToUniversalTime(),
        Comment = dto.Comment,
        Difficulty = ParseDifficulty(dto.Difficulty),
        TotalDistance = dto.TotalDistance * 1000.0,
        Duration = TimeSpan.FromMinutes(dto.Duration),
        Rating = dto.Rating,
    };

    public static void ApplyUpdate(TourLog entity, UpdateTourLogDto dto)
    {
        entity.LoggedAt = dto.LoggedAt.ToUniversalTime();
        entity.Comment = dto.Comment;
        entity.Difficulty = ParseDifficulty(dto.Difficulty);
        entity.TotalDistance = dto.TotalDistance * 1000.0;
        entity.Duration = TimeSpan.FromMinutes(dto.Duration);
        entity.Rating = dto.Rating;
    }

    private static Difficulty ParseDifficulty(string value) =>
        Enum.TryParse<Difficulty>(value, ignoreCase: true, out var result) && result != Difficulty.Unknown
            ? result
            : throw new ArgumentException($"Invalid difficulty: '{value}'.");
}
