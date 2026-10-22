using Microsoft.AspNetCore.Components.Authorization;

namespace IsaChatApp.Services;

/// <summary>
/// Whether the signed-in person holds the Keycloak role "dashboard_dev" (the team).
/// Those people always get Backstage (Config and the Developer Dashboard), whatever role
/// is picked on /isa-config. Chapter leaders do not hold the role, so they see no Backstage.
/// </summary>
public class DevAccessService
{
    public const string DevRole = "dashboard_dev";

    private readonly IAccessControlAdapter _access;
    private readonly KeycloakSignInService _signIn;

    public DevAccessService(IAccessControlAdapter access, KeycloakSignInService signIn)
    {
        _access = access;
        _signIn = signIn;
    }

    public async Task<bool> HasDevRoleAsync(Task<AuthenticationState>? authState)
    {
        try
        {
            if (authState is null) return false;
            var user = (await authState).User;
            var sessionId = user.FindFirst(KeycloakSignInService.SessionClaim)?.Value;
            var token = await _signIn.GetAccessTokenAsync(sessionId);
            if (token is null) return false;
            var identity = await _access.ValidateAsync($"Bearer {token}");
            return identity is not null && identity.Roles.Contains(DevRole);
        }
        catch { return false; }   // no role found means no Backstage; never break the page
    }
}
