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

    // Claims the Keycloak mappers add to the token (see directory/build_keycloak_realm.py).
    private static readonly string[] ProfileClaims =
    {
        "person_id", "chapter_id", "chapter_role", "permission", "term_start", "term_end",
        "years_as_leader", "language", "formality", "form_of_address"
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
        if (_devBypass)
        {
            // Dev only: a real signed-in token is still used (so roles and profile can be tested);
            // with no valid token the chat answers as "dev-user".
            return ValidateToken(bearerToken) ?? new AccessIdentity("dev-user", new[] { "chat_user" });
        }
        return ValidateToken(bearerToken);
    }

    private AccessIdentity? ValidateToken(string? bearerToken)
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

            // For Foundation: validate structure + expiry.
            // Full signature validation against Keycloak JWKS can be tightened in Pipeline Built.
            var jwt = handler.ReadJwtToken(token);

            if (jwt.ValidTo < DateTime.UtcNow)
            {
                _log.LogInformation("Token expired");
                return null;
            }

            var issuer = _config["Keycloak:Authority"]; // e.g. http://localhost:8080/realms/isa
            if (!string.IsNullOrEmpty(issuer) &&
                !string.Equals(jwt.Issuer, issuer, StringComparison.OrdinalIgnoreCase))
            {
                _log.LogInformation("Token issuer mismatch: {Issuer}", jwt.Issuer);
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
                "Your account does not have access to this assistant.",
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
