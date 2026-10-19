using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace IsaChatApp.Services;

/// <summary>
/// Sign-in against the team's Keycloak server.
///
/// How it fits together:
///   1. The sign-in page (Components/Pages/SignIn.razor) sends the email
///      address and password here. They go straight to Keycloak's token
///      endpoint; this app never stores the password.
///   2. If Keycloak accepts them, it sends back a signed access token
///      (plus a refresh token). Those tokens stay on the server, in memory,
///      under a random session ID.
///   3. The browser only gets an encrypted sign-in cookie holding the
///      person's ID, name, email, roles and that session ID.
///   4. When the chat sends a question, ChatWidget asks this service for
///      a current access token for the session and passes it to the
///      pipeline, where the access_control stage checks it.
///
/// Sign-in is by EMAIL ADDRESS, never by name, so it works the same for
/// every chapter in every country. Passwords can contain any characters.
///
/// Keycloak settings live in appsettings.json under "Keycloak" -- see
/// docs/sign-in-setup.md for what the Keycloak client needs.
///
/// Prototype limits: tokens are kept in memory, so restarting the app
/// signs everyone out of the chat (they just sign in again).
/// </summary>
public class KeycloakSignInService
{
    /// <summary>Claim holding the server-side session ID (links the cookie to the stored tokens).</summary>
    public const string SessionClaim = "isa_session";

    /// <summary>Claim holding the name Isa uses to greet the person (their preferred form of address).</summary>
    public const string GreetingClaim = "isa_greeting";

    /// <summary>Claim holding the person's preferred language code, e.g. "ja-JP".</summary>
    public const string LocaleClaim = "locale";

    /// <summary>
    /// Claim holding the person's ID in the directory, e.g. "P-0001".
    /// Only chapter people have one (staff test accounts don't). The
    /// onboarding journey card uses it to look up the person's role and
    /// day of term (Components/Shared/JourneyCard.razor).
    /// </summary>
    public const string PersonIdClaim = "person_id";

    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly ILogger<KeycloakSignInService> _log;
    private readonly ConcurrentDictionary<string, StoredTokens> _sessions = new();

    public KeycloakSignInService(
        IHttpClientFactory httpFactory,
        IConfiguration config,
        ILogger<KeycloakSignInService> log)
    {
        _http = httpFactory.CreateClient("keycloak");
        _http.Timeout = TimeSpan.FromSeconds(10);
        _config = config;
        _log = log;
    }

    private string Authority => (_config["Keycloak:Authority"] ?? "").TrimEnd('/');
    private string ClientId => _config["Keycloak:ClientId"] ?? "";
    private string? ClientSecret =>
        string.IsNullOrWhiteSpace(_config["Keycloak:ClientSecret"]) ? null : _config["Keycloak:ClientSecret"];

    private string TokenEndpoint => $"{Authority}/protocol/openid-connect/token";
    private string LogoutEndpoint => $"{Authority}/protocol/openid-connect/logout";

    // ---- Sign in ---------------------------------------------------------

    public async Task<SignInResult> SignInAsync(string email, string password, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(Authority) || string.IsNullOrEmpty(ClientId))
        {
            _log.LogWarning("Sign-in attempted but Keycloak:Authority or Keycloak:ClientId is not set in appsettings.json.");
            return new SignInResult(SignInOutcome.NotConfigured);
        }

        var form = BaseForm();
        form["grant_type"] = "password";
        form["username"] = email.Trim();
        form["password"] = password;
        form["scope"] = "openid profile email";

        TokenResponse? tokens;
        try
        {
            using var response = await _http.PostAsync(TokenEndpoint, new FormUrlEncodedContent(form), ct);

            if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized)
            {
                var error = await ReadErrorAsync(response, ct);

                // invalid_grant = wrong email/password, unknown user, or a
                // disabled account. The person sees one general message
                // either way, so the page never reveals which emails exist.
                if (error.Error == "invalid_grant")
                {
                    _log.LogInformation("Sign-in refused by Keycloak: {Description}", error.Description);
                    return new SignInResult(SignInOutcome.WrongCredentials);
                }

                // Anything else (unauthorized_client, invalid_client, ...)
                // means the Keycloak client is set up differently than this
                // app expects. See docs/sign-in-setup.md.
                _log.LogWarning("Keycloak rejected the sign-in request: {Error} - {Description}. Check the client settings in docs/sign-in-setup.md.",
                    error.Error, error.Description);
                return new SignInResult(SignInOutcome.NotConfigured);
            }

            if (!response.IsSuccessStatusCode)
            {
                _log.LogWarning("Keycloak sign-in returned HTTP {Status}.", (int)response.StatusCode);
                return new SignInResult(SignInOutcome.ServiceUnavailable);
            }

            tokens = await response.Content.ReadFromJsonAsync<TokenResponse>(ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _log.LogWarning("Could not reach Keycloak at {Endpoint}: {Message}", TokenEndpoint, ex.Message);
            return new SignInResult(SignInOutcome.ServiceUnavailable);
        }

        if (string.IsNullOrEmpty(tokens?.AccessToken))
        {
            _log.LogWarning("Keycloak sign-in response had no access token.");
            return new SignInResult(SignInOutcome.ServiceUnavailable);
        }

        var sessionId = Guid.NewGuid().ToString("N");
        _sessions[sessionId] = StoredTokens.From(tokens);
        return new SignInResult(SignInOutcome.Success, BuildPrincipal(tokens.AccessToken, sessionId));
    }

    // ---- Tokens for the chat --------------------------------------------

    /// <summary>
    /// A current access token for this session, refreshed with Keycloak
    /// first if it is about to expire. Null if the person isn't signed in,
    /// the app was restarted, or Keycloak ended the session.
    /// </summary>
    public async Task<string?> GetAccessTokenAsync(string? sessionId, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(sessionId) || !_sessions.TryGetValue(sessionId, out var stored))
            return null;

        if (stored.AccessExpiresUtc > DateTime.UtcNow.AddSeconds(30))
            return stored.AccessToken;

        if (string.IsNullOrEmpty(stored.RefreshToken) || stored.RefreshExpiresUtc <= DateTime.UtcNow)
        {
            _sessions.TryRemove(sessionId, out _);
            return null;
        }

        var form = BaseForm();
        form["grant_type"] = "refresh_token";
        form["refresh_token"] = stored.RefreshToken;

        try
        {
            using var response = await _http.PostAsync(TokenEndpoint, new FormUrlEncodedContent(form), ct);
            if (!response.IsSuccessStatusCode)
            {
                _sessions.TryRemove(sessionId, out _);
                return null;
            }

            var tokens = await response.Content.ReadFromJsonAsync<TokenResponse>(ct);
            if (string.IsNullOrEmpty(tokens?.AccessToken))
            {
                _sessions.TryRemove(sessionId, out _);
                return null;
            }

            _sessions[sessionId] = StoredTokens.From(tokens);
            return tokens.AccessToken;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _log.LogWarning("Could not refresh the Keycloak token: {Message}", ex.Message);
            return null;
        }
    }

    // ---- Sign out --------------------------------------------------------

    /// <summary>Forgets the session's tokens and asks Keycloak to end the session too.</summary>
    public async Task SignOutAsync(string? sessionId, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(sessionId) || !_sessions.TryRemove(sessionId, out var stored))
            return;

        if (string.IsNullOrEmpty(stored.RefreshToken))
            return;

        var form = BaseForm();
        form["refresh_token"] = stored.RefreshToken;

        try
        {
            using var _ = await _http.PostAsync(LogoutEndpoint, new FormUrlEncodedContent(form), ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            // Best effort: the person is signed out of Isa either way.
            _log.LogInformation("Keycloak logout call failed: {Message}", ex.Message);
        }
    }

    // ---- Helpers -----------------------------------------------------------

    private Dictionary<string, string> BaseForm()
    {
        var form = new Dictionary<string, string> { ["client_id"] = ClientId };
        if (ClientSecret is not null) form["client_secret"] = ClientSecret;
        return form;
    }

    /// <summary>
    /// Turns the Keycloak access token into the claims kept in the sign-in
    /// cookie. Reads these token claims (the custom ones are optional and
    /// come from Keycloak user attributes -- see docs/sign-in-setup.md):
    ///   sub, email, name, preferred_username, locale   (Keycloak standard)
    ///   display_name     full name as written, in the person's own script
    ///   form_of_address  how Isa should greet them, e.g. "田中様", "Frau Müller"
    /// </summary>
    private static ClaimsPrincipal BuildPrincipal(string accessToken, string sessionId)
    {
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
        string? Get(string type) =>
            jwt.Claims.FirstOrDefault(c => c.Type == type && !string.IsNullOrWhiteSpace(c.Value))?.Value;

        var email = Get("email") ?? Get("preferred_username") ?? "";
        var displayName = Get("display_name") ?? Get("name") ?? email;
        var greeting = Get("form_of_address") ?? displayName;

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, jwt.Subject ?? email),
            new(ClaimTypes.Name, displayName),
            new(ClaimTypes.Email, email),
            new(GreetingClaim, greeting),
            new(SessionClaim, sessionId),
        };

        var locale = Get("locale");
        if (locale is not null) claims.Add(new Claim(LocaleClaim, locale));

        var personId = Get("person_id");
        if (personId is not null) claims.Add(new Claim(PersonIdClaim, personId));

        // Keycloak puts roles inside realm_access.roles and
        // resource_access.<client>.roles.
        var roles = new HashSet<string>(StringComparer.Ordinal);
        foreach (var c in jwt.Claims.Where(c => c.Type is "realm_access" or "resource_access"))
            CollectRoles(c.Value, roles);
        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme,
            ClaimTypes.Name, ClaimTypes.Role);
        return new ClaimsPrincipal(identity);
    }

    private static void CollectRoles(string json, HashSet<string> roles)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            Walk(doc.RootElement);
        }
        catch (JsonException)
        {
            // Not JSON -- no roles to read.
        }

        void Walk(JsonElement el)
        {
            if (el.ValueKind != JsonValueKind.Object) return;
            foreach (var prop in el.EnumerateObject())
            {
                if (prop.Name == "roles" && prop.Value.ValueKind == JsonValueKind.Array)
                {
                    foreach (var r in prop.Value.EnumerateArray())
                        if (r.ValueKind == JsonValueKind.String) roles.Add(r.GetString()!);
                }
                else
                {
                    Walk(prop.Value);
                }
            }
        }
    }

    private static async Task<(string? Error, string? Description)> ReadErrorAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            var body = await response.Content.ReadFromJsonAsync<ErrorResponse>(ct);
            return (body?.Error, body?.Description);
        }
        catch (Exception)
        {
            return (null, null);
        }
    }

    private sealed record StoredTokens(
        string AccessToken,
        DateTime AccessExpiresUtc,
        string? RefreshToken,
        DateTime RefreshExpiresUtc)
    {
        public static StoredTokens From(TokenResponse t) => new(
            t.AccessToken!,
            DateTime.UtcNow.AddSeconds(t.ExpiresIn),
            t.RefreshToken,
            // Keycloak sends 0 when the refresh token has no fixed expiry.
            t.RefreshExpiresIn > 0 ? DateTime.UtcNow.AddSeconds(t.RefreshExpiresIn) : DateTime.MaxValue);
    }

    private sealed class TokenResponse
    {
        [JsonPropertyName("access_token")] public string? AccessToken { get; set; }
        [JsonPropertyName("expires_in")] public int ExpiresIn { get; set; }
        [JsonPropertyName("refresh_token")] public string? RefreshToken { get; set; }
        [JsonPropertyName("refresh_expires_in")] public int RefreshExpiresIn { get; set; }
    }

    private sealed class ErrorResponse
    {
        [JsonPropertyName("error")] public string? Error { get; set; }
        [JsonPropertyName("error_description")] public string? Description { get; set; }
    }
}

public enum SignInOutcome
{
    Success,
    WrongCredentials,
    ServiceUnavailable,
    NotConfigured
}

public record SignInResult(SignInOutcome Outcome, ClaimsPrincipal? Principal = null);
