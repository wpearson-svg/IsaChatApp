using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace IsaChatApp.Services;

/// <summary>
/// T5 — Access Control adapter (pass-through).
/// The only code that knows about Keycloak.
/// If the client switches identity systems later, only this file changes.
/// </summary>
public interface IAccessControlAdapter
{
    /// <summary>Validate token. Returns userId + roles, or null if invalid.</summary>
    Task<AccessIdentity?> ValidateAsync(string? bearerToken);
}

public record AccessIdentity(string UserId, IReadOnlyList<string> Roles, IReadOnlyDictionary<string, string>? Profile = null);

public class KeycloakAccessControlAdapter : IAccessControlAdapter
{
    private readonly IConfiguration _config;
    private readonly ILogger<KeycloakAccessControlAdapter> _log;
    private readonly HttpClient _http;
    private readonly bool _devBypass;

    // Keycloak's public signing keys (its JWKS), kept so tokens can be checked without asking Keycloak every time.
    private JsonWebKeySet? _keys;
    private DateTimeOffset _keysFetched = DateTimeOffset.MinValue;
    private DateTimeOffset _keysTried = DateTimeOffset.MinValue;
    private readonly SemaphoreSlim _keysLock = new(1, 1);
    private static readonly TimeSpan KeysMaxAge = TimeSpan.FromHours(1);          // refresh at least hourly
    private static readonly TimeSpan KeysRetryGap = TimeSpan.FromSeconds(30);     // but never hammer Keycloak

    // The clients whose tokens Isa accepts (the token's "azp"). Keycloak:AllowedClients overrides this list.
    private static readonly string[] DefaultClients = { "isa-chat", "isa-web", "isa-android" };

    // Claims the Keycloak mappers add to the token (see directory/build_keycloak_realm.py).
    private static readonly string[] ProfileClaims =
    {
        "person_id", "chapter_id", "chapter_role", "permission", "term_start", "term_end",
        "years_as_leader", "language", "formality", "form_of_address", "display_name", "email"
    };

    public KeycloakAccessControlAdapter(
        IConfiguration config,
        IHttpClientFactory httpFactory,
        ILogger<KeycloakAccessControlAdapter> log)
    {
        _config = config;
        _log = log;
        _http = httpFactory.CreateClient("keycloak");
        // Dev only: set AccessControl:DevBypass=true in appsettings.Development.json
        // MUST be false in the Friday demo build.
        _devBypass = config.GetValue("AccessControl:DevBypass", false);
        if (_devBypass)
            _log.LogWarning("AccessControl DevBypass is ON — do not use in demo build");
    }

    public async Task<AccessIdentity?> ValidateAsync(string? bearerToken)
    {
        var keys = await GetKeysAsync(bearerToken);
        if (_devBypass)
        {
            // Dev only: a real signed-in token is still used (so roles and profile can be tested);
            // with no valid token the chat answers as "dev-user". An INVALID token is never treated as valid.
            return ValidateToken(bearerToken, keys) ?? new AccessIdentity("dev-user", new[] { "chat_user" });
        }
        return ValidateToken(bearerToken, keys);
    }

    // The signing keys for this token. Fetches Keycloak's keys the first time, refreshes them hourly, and
    // refreshes at once (at most every 30 seconds) when a token names a key we do not have, which is what
    // happens after Keycloak makes new keys. Never throws: with no keys, no token is accepted.
    private async Task<JsonWebKeySet?> GetKeysAsync(string? bearerToken)
    {
        var now = DateTimeOffset.UtcNow;
        var kid = TryReadKeyId(bearerToken);
        bool Fresh() => _keys is not null && now - _keysFetched < KeysMaxAge
                        && (kid is null || _keys.Keys.Any(k => k.Kid == kid));
        if (Fresh()) return _keys;

        await _keysLock.WaitAsync();
        try
        {
            if (Fresh()) return _keys;
            if (now - _keysTried < KeysRetryGap) return _keys;   // tried a moment ago: use what we have
            _keysTried = now;

            var authority = (_config["Keycloak:Authority"] ?? "").TrimEnd('/');
            if (string.IsNullOrEmpty(authority))
            {
                _log.LogWarning("Keycloak:Authority is not set, so tokens cannot be checked.");
                return _keys;
            }
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var json = await _http.GetStringAsync($"{authority}/protocol/openid-connect/certs", timeout.Token);
            _keys = new JsonWebKeySet(json);
            _keysFetched = DateTimeOffset.UtcNow;
            return _keys;
        }
        catch (Exception ex)
        {
            _log.LogWarning("Could not get Keycloak's signing keys ({Type}); tokens that need them will be refused.", ex.GetType().Name);
            return _keys;
        }
        finally { _keysLock.Release(); }
    }

    private static string? TryReadKeyId(string? bearerToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(bearerToken)) return null;
            var t = bearerToken.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? bearerToken["Bearer ".Length..] : bearerToken;
            var h = new JwtSecurityTokenHandler();
            return h.CanReadToken(t) ? h.ReadJwtToken(t).Header.Kid : null;
        }
        catch { return null; }
    }

    // Checks the token's signature (against Keycloak's keys), expiry and issuer, and that it was issued to one of
    // Isa's own clients. Returns who it is, or null if anything is wrong. Fixed 28 October 2026: it used to read
    // the token without checking the signature, so a made-up token with the right issuer was accepted.
    private AccessIdentity? ValidateToken(string? bearerToken, JsonWebKeySet? keys)
    {
        if (string.IsNullOrWhiteSpace(bearerToken))
            return null;

        // Strip "Bearer " if present
        var token = bearerToken.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? bearerToken["Bearer ".Length..]
            : bearerToken;

        try
        {
            var handler = new JwtSecurityTokenHandler();
            if (!handler.CanReadToken(token))
                return null;

            if (keys is null || keys.Keys.Count == 0)
            {
                _log.LogInformation("No Keycloak signing keys available: token refused");
                return null;
            }

            var issuer = _config["Keycloak:Authority"]?.TrimEnd('/');   // e.g. http://localhost:8080/realms/isa
            // Keycloak puts the address the caller used into the token's "iss". Reaching it over Tailscale gives a
            // different address than localhost, so extra accepted issuers can be listed in Keycloak:AdditionalIssuers.
            // The signing keys are the same; the signature check above is unchanged.
            var issuers = new List<string>();
            if (!string.IsNullOrEmpty(issuer)) issuers.Add(issuer);
            foreach (var extra in _config.GetSection("Keycloak:AdditionalIssuers").Get<string[]>() ?? Array.Empty<string>())
                if (!string.IsNullOrWhiteSpace(extra)) issuers.Add(extra.Trim().TrimEnd('/'));
            var parameters = new TokenValidationParameters
            {
                RequireSignedTokens = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = keys.GetSigningKeys(),
                ValidAlgorithms = new[] { SecurityAlgorithms.RsaSha256 },   // Keycloak's default; refuses "none" and look-alike algorithms
                ValidateIssuer = !string.IsNullOrEmpty(issuer),
                ValidIssuers = issuers,
                ValidateAudience = false,   // Keycloak's "aud" is not the client; the client is checked as "azp" below
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(1),
            };
            handler.ValidateToken(token, parameters, out var validated);
            var jwt = (JwtSecurityToken)validated;

            var allowed = _config.GetSection("Keycloak:AllowedClients").Get<string[]>() is { Length: > 0 } fromConfig ? fromConfig : DefaultClients;
            var azp = jwt.Claims.FirstOrDefault(c => c.Type == "azp")?.Value;
            if (string.IsNullOrEmpty(azp) || !allowed.Contains(azp, StringComparer.OrdinalIgnoreCase))
            {
                _log.LogInformation("Token was issued to a client Isa does not accept: {Client}", azp ?? "(none)");
                return null;
            }

            var userId = jwt.Subject
                ?? jwt.Claims.FirstOrDefault(c => c.Type == "preferred_username")?.Value
                ?? jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                return null;

            // Realm roles often live under realm_access.roles
            var roles = new List<string>();
            foreach (var c in jwt.Claims.Where(c => c.Type == "role" || c.Type == ClaimTypes.Role))
                roles.Add(c.Value);

            // Also check resource_access / realm roles claim arrays if present as JSON
            var roleClaims = jwt.Claims.Where(c => c.Type == "roles").Select(c => c.Value);
            roles.AddRange(roleClaims);

            // Keycloak puts roles inside JSON objects:
            //   realm_access.roles and resource_access.<client>.roles
            foreach (var c in jwt.Claims.Where(c => c.Type == "realm_access" || c.Type == "resource_access"))
                roles.AddRange(ReadRolesFromJson(c.Value));

            var profile = new Dictionary<string, string>();
            foreach (var name in ProfileClaims)
            {
                var value = jwt.Claims.FirstOrDefault(c => c.Type == name)?.Value;
                if (!string.IsNullOrEmpty(value)) profile[name] = value;
            }

            return new AccessIdentity(userId, roles.Distinct().ToList(), profile);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Token validation failed");
            return null;
        }
    }

    private static IEnumerable<string> ReadRolesFromJson(string json)
    {
        var found = new List<string>();
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            void Walk(System.Text.Json.JsonElement el)
            {
                if (el.ValueKind != System.Text.Json.JsonValueKind.Object) return;
                foreach (var prop in el.EnumerateObject())
                {
                    if (prop.Name == "roles" && prop.Value.ValueKind == System.Text.Json.JsonValueKind.Array)
                        foreach (var r in prop.Value.EnumerateArray())
                        { if (r.ValueKind == System.Text.Json.JsonValueKind.String) found.Add(r.GetString()!); }
                    else Walk(prop.Value);
                }
            }
            Walk(doc.RootElement);
        }
        catch { /* not JSON -> no roles */ }
        return found;
    }
}

/// <summary>Stage wrapper so access_control appears in the pipeline and event log.</summary>
public class AccessControlStage : Orchestrator.IStage
{
    public string Name => "access_control";
    private readonly IAccessControlAdapter _adapter;

    public AccessControlStage(IAccessControlAdapter adapter)
    {
        _adapter = adapter;
    }

    public async Task<Orchestrator.StageResult> RunAsync(Orchestrator.QueryContext ctx)
    {
        var identity = await _adapter.ValidateAsync(ctx.IncomingToken);
        if (identity is null)
        {
            ctx.AuthFailed = true;
            return new Orchestrator.StageResult(
                Orchestrator.StageOutcome.Stop,
                "Not signed in or invalid token",
                "refused"
            );
        }

        ctx.UserId = identity.UserId;
        ctx.Roles = identity.Roles;
        ctx.Profile = identity.Profile ?? new Dictionary<string, string>();

        // Signed in is not enough: the account must hold the chat_user role.
        if (!identity.Roles.Contains("chat_user"))
        {
            ctx.AccessDenied = true;
            return new Orchestrator.StageResult(
                Orchestrator.StageOutcome.Stop,
                IsaChatApp.Services.ShowTheWork.Wording.NoAccess,
                $"user={identity.UserId}; roles=[{string.Join(",", identity.Roles)}]; reason=no_chat_user_role"
            );
        }

        // Never put the token in the log — only user id + roles
        return new Orchestrator.StageResult(
            Orchestrator.StageOutcome.Pass,
            null,
            $"user={identity.UserId}; roles=[{string.Join(",", identity.Roles)}]" +
            (ctx.Profile.TryGetValue("chapter_role", out var cr) ? $"; chapter_role={cr}" : "") +
            (ctx.Profile.TryGetValue("permission", out var perm) ? $"; permission={perm}" : "")
        );
    }
}
