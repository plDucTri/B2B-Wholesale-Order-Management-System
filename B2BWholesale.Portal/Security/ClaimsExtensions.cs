using System.Security.Claims;

namespace B2BWholesale.Portal.Security;

public static class ClaimsExtensions
{
    public const string AgentIdClaim = "AgentId";

    /// <summary>The Agent.AgentID of the logged-in agent (stored in the auth cookie at login).</summary>
    public static int GetAgentId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(AgentIdClaim);
        return int.TryParse(value, out var id)
            ? id
            : throw new InvalidOperationException("Logged-in user has no AgentId claim.");
    }
}
