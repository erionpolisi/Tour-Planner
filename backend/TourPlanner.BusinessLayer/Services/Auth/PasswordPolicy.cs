namespace TourPlanner.BusinessLayer.Services.Auth;

/// <summary>
/// NIST SP 800-63B §5.1.1.2 requires new passwords to be checked against a list
/// of "commonly-used or expected values". A tiny embedded top-list is enough
/// for a coursework project; a production system should point this at haveibeen
/// pwned's k-anonymity API or a full-size breach corpus.
/// </summary>
public interface IPasswordPolicy
{
    /// <summary>
    /// Returns null when the candidate is acceptable, or a user-facing reason
    /// (safe to echo back through <c>ValidationException</c>) when it is not.
    /// </summary>
    string? Validate(string password, string? email = null, string? name = null);
}

/// <inheritdoc cref="IPasswordPolicy" />
public sealed class DefaultPasswordPolicy : IPasswordPolicy
{
    /// <summary>Minimum length. Aligns with the DTO annotation and the NIST floor of 8.</summary>
    public const int MinimumLength = 8;

    // Top ~150 passwords from the Have-I-Been-Pwned "most common" list (2024).
    // Stored lower-cased; comparison is case-insensitive.
    private static readonly HashSet<string> Common = new(StringComparer.OrdinalIgnoreCase)
    {
        "123456", "123456789", "qwerty", "password", "12345", "12345678", "111111",
        "1234567", "1234567890", "123123", "000000", "abc123", "1234", "password1",
        "iloveyou", "qwerty123", "1q2w3e4r", "admin", "qwertyuiop", "654321",
        "555555", "lovely", "7777777", "welcome", "888888", "princess", "dragon",
        "password123", "master", "hello", "freedom", "whatever", "qazwsx", "trustno1",
        "654321", "jordan23", "harley", "ranger", "iwantu", "jennifer", "hunter",
        "buster", "soccer", "baseball", "tigger", "charlie", "andrew", "michelle",
        "love", "sunshine", "jessica", "asshole", "6969", "pepper", "daniel",
        "access", "123456a", "654321", "joshua", "maggie", "starwars", "silver",
        "william", "dallas", "yankees", "123123123", "ashley", "666666", "hockey",
        "george", "letmein", "monkey", "abcdef", "abcd1234", "abcdefg", "shadow",
        "superman", "batman", "test", "test123", "passw0rd", "p@ssw0rd", "p@ssword",
        "pa$$word", "pa$$w0rd", "administrator", "root", "toor", "changeme",
        "default", "guest", "user", "user123", "login", "welcome1", "welcome123",
        "qwerty1", "qwerty12", "qazwsxedc", "1qaz2wsx", "zaq12wsx", "asdfgh",
        "asdfghjkl", "zxcvbnm", "azerty", "computer", "internet", "google",
        "facebook", "michael", "matthew", "jesus", "ninja", "mustang", "access14",
        "asdfjkl;", "loveme", "flower", "hello123", "hallo", "hallo123", "sommer",
        "winter", "winter2024", "summer2024", "spring2024", "autumn2024",
        "letmein1", "letmein2", "monkey123", "dragon123", "shadow123",
        "football", "football1", "baseball1", "basketball", "starwars1",
        "changeme1", "changeme123", "temp", "temp123", "temporary",
        "iloveyou1", "iloveyou2", "iloveyou123", "sunshine1", "princess1",
    };

    public string? Validate(string password, string? email = null, string? name = null)
    {
        if (string.IsNullOrWhiteSpace(password))
            return "Password must not be empty.";

        // Length check — enforced at the DTO level too, but re-check so callers that
        // bypass DTO validation (e.g. tests) still get the same guarantee.
        if (password.Length < MinimumLength)
            return $"Password must be at least {MinimumLength} characters long.";

        // Common-password check (NIST SP 800-63B §5.1.1.2).
        if (Common.Contains(password))
            return "This password is on the list of commonly-used passwords. Please choose something less predictable.";

        // Reject the trivial case where the password IS the email or the display name.
        if (!string.IsNullOrEmpty(email) && string.Equals(password, email, StringComparison.OrdinalIgnoreCase))
            return "Password must not match your email address.";
        if (!string.IsNullOrEmpty(name) && string.Equals(password, name, StringComparison.OrdinalIgnoreCase))
            return "Password must not match your display name.";

        return null;
    }
}
