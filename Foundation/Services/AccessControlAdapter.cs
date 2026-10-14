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

public record AccessIdentity(string UserId, IReadOnlyList<string> Roles);

public class KeycloakAccessControlAdapter : IAccessControlAdapter
{
    private readonly IConfiguration _config;
    private readonly ILogger<KeycloakAccessControlAdapter> _log;
    private readonly HttpClient _http;
    private readonly bool _devBypass;

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
    }

    public async Task<AccessIdentity?> ValidateAsync(string? bearerToken)
    {
        if (_devBypass)
        {
            _log.LogWarning("AccessControl DevBypass is ON — do not use in demo build");
            return new AccessIdentity("dev-user", new[] { "chapter_leader" });
        }

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
            var realmAccess = jwt.Claims.FirstOrDefault(c => c.Type == "realm_access")?.Value;
            foreach (var c in jwt.Claims.Where(c => c.Type == "role" || c.Type == ClaimTypes.Role))
                roles.Add(c.Value);

            // Also check resource_access / realm roles claim arrays if present as JSON
            var roleClaims = jwt.Claims.Where(c => c.Type == "roles").Select(c => c.Value);
            roles.AddRange(roleClaims);

            return new AccessIdentity(userId, roles.Distinct().ToList());
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Token validation failed");
            return null;
        }
    }
}

/// <summary>Stage wrapper so access_control appears in the pipeline and event log.</summary>
public class AccessControlStage : Orchestrator.IStage
{
    public string Name => "access_control";
    private readonly IAccessControlAdapter _adapter;

    // Token is set by the endpoint before the pipeline runs
    public string? IncomingToken { get; set; }

    public AccessControlStage(IAccessControlAdapter adapter)
    {
        _adapter = adapter;
    }

    public async Task<Orchestrator.StageResult> RunAsync(Orchestrator.QueryContext ctx)
    {
        var identity = await _adapter.ValidateAsync(IncomingToken);
        if (identity is null)
        {
            return new Orchestrator.StageResult(
                Orchestrator.StageOutcome.Stop,
                "Not signed in or invalid token",
                "refused"
            );
        }

        ctx.UserId = identity.UserId;
        ctx.Roles = identity.Roles;

        // Never put the token in the log — only user id + roles
        return new Orchestrator.StageResult(
            Orchestrator.StageOutcome.Pass,
            null,
            $"user={identity.UserId}; roles=[{string.Join(",", identity.Roles)}]"
        );
    }
}
