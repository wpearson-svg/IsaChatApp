using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using IsaChatApp.Services.Orchestrator;

namespace IsaChatApp.Services;

public interface IAccessControlAdapter
{
    Task<AccessIdentity?> ValidateAsync(string? bearerToken);
}

public record AccessIdentity(string UserId, IReadOnlyList<string> Roles);

public class KeycloakAccessControlAdapter : IAccessControlAdapter
{
    private readonly IConfiguration _config;
    private readonly ILogger<KeycloakAccessControlAdapter> _log;
    private readonly bool _devBypass;

    public KeycloakAccessControlAdapter(IConfiguration config, ILogger<KeycloakAccessControlAdapter> log)
    {
        _config = config;
        _log = log;
        _devBypass = config.GetValue("AccessControl:DevBypass", false);
    }

    public Task<AccessIdentity?> ValidateAsync(string? bearerToken)
    {
        if (_devBypass)
        {
            _log.LogWarning("AccessControl DevBypass is ON — turn OFF for Friday demo");
            return Task.FromResult<AccessIdentity?>(
                new AccessIdentity("dev-user", new[] { "chapter_leader" }));
        }

        if (string.IsNullOrWhiteSpace(bearerToken))
            return Task.FromResult<AccessIdentity?>(null);

        var token = bearerToken.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? bearerToken["Bearer ".Length..]
            : bearerToken;

        try
        {
            var handler = new JwtSecurityTokenHandler();
            if (!handler.CanReadToken(token))
                return Task.FromResult<AccessIdentity?>(null);

            var jwt = handler.ReadJwtToken(token);

            if (jwt.ValidTo < DateTime.UtcNow)
            {
                _log.LogInformation("Token expired");
                return Task.FromResult<AccessIdentity?>(null);
            }

            var issuer = _config["Keycloak:Authority"];
            if (!string.IsNullOrEmpty(issuer) &&
                !string.Equals(jwt.Issuer, issuer, StringComparison.OrdinalIgnoreCase))
            {
                _log.LogInformation("Token issuer mismatch: {Issuer}", jwt.Issuer);
                return Task.FromResult<AccessIdentity?>(null);
            }

            var userId = jwt.Subject
                ?? jwt.Claims.FirstOrDefault(c => c.Type == "preferred_username")?.Value
                ?? jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                return Task.FromResult<AccessIdentity?>(null);

            var roles = jwt.Claims
                .Where(c => c.Type is "role" or "roles" || c.Type == ClaimTypes.Role)
                .Select(c => c.Value)
                .Distinct()
                .ToList();

            return Task.FromResult<AccessIdentity?>(new AccessIdentity(userId, roles));
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Token validation failed");
            return Task.FromResult<AccessIdentity?>(null);
        }
    }
}

/// <summary>Pipeline stage wrapper for access_control.</summary>
public class AccessControlStage : IStage
{
    public string Name => "access_control";
    private readonly IAccessControlAdapter _adapter;
    public string? IncomingToken { get; set; }

    public AccessControlStage(IAccessControlAdapter adapter) => _adapter = adapter;

    public async Task<StageResult> RunAsync(QueryContext ctx)
    {
        var identity = await _adapter.ValidateAsync(IncomingToken);
        if (identity is null)
        {
            return new StageResult(StageOutcome.Stop, "Not signed in or invalid token", "refused");
        }

        ctx.UserId = identity.UserId;
        ctx.Roles = identity.Roles;

        // Never put the token in the log
        return new StageResult(
            StageOutcome.Pass,
            null,
            $"user={identity.UserId}; roles=[{string.Join(",", identity.Roles)}]");
    }
}
