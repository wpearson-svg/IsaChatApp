namespace IsaChatApp.Services.Auth;

/// <summary>
/// Auth helpers for ISA login flow (Sprint 1 / Foundation).
/// T7A: reusable functions + error handling.
/// </summary>
public class AuthService
{
    private readonly ILogger<AuthService> _log;

    public AuthService(ILogger<AuthService> log) => _log = log;

    /// <summary>Normalize email for lookup. Returns null if invalid.</summary>
    public string? NormalizeEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return null;

        var cleaned = email.Trim().ToLowerInvariant();
        if (!cleaned.Contains('@') || cleaned.Length < 5)
        {
            _log.LogWarning("[{Time}] WARN Invalid email format", DateTime.UtcNow.ToString("O"));
            return null;
        }
        return cleaned;
    }

    /// <summary>Build a safe generic login-failure message (never leak whether user exists).</summary>
    public string GetLoginFailureMessage() => "Invalid username or password.";

    /// <summary>Validate password is non-empty and within length limits.</summary>
    public bool IsPasswordAcceptable(string? password, out string? error)
    {
        error = null;
        if (string.IsNullOrEmpty(password))
        {
            error = "Password is required.";
            return false;
        }
        if (password.Length > 128)
        {
            error = "Password is too long.";
            return false;
        }
        return true;
    }
}
