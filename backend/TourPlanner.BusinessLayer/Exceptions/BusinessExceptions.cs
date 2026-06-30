namespace TourPlanner.BusinessLayer.Exceptions;

/// <summary>
/// Thrown when an entity cannot be found by id.
/// The controller layer translates this into HTTP 404.
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}

/// <summary>
/// Thrown for business-rule violations or invalid input that DTO validation
/// cannot catch on its own. Controller layer translates this into HTTP 400.
/// </summary>
public class ValidationException : Exception
{
    public ValidationException(string message) : base(message) { }
}

/// <summary>
/// Thrown when an attempt to create an entity collides with a uniqueness
/// constraint (e.g. duplicate email). Controller layer translates into HTTP 409.
/// </summary>
public class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}
