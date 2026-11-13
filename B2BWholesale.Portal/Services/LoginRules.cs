using B2BWholesale.Portal.Models;
using B2BWholesale.Portal.Security;

namespace B2BWholesale.Portal.Services;

public enum LoginResult
{
    Success,
    InvalidCredentials,
    AccountLocked,
    NotAnAgent,
    NoAgentProfile
}

/// <summary>Pure login decision logic (no database, no HTTP) so it can be unit-tested.</summary>
public static class LoginRules
{
    public static LoginResult Evaluate(User? user, string password)
    {
        // Same result for "unknown user" and "wrong password" -> no user enumeration.
        if (user is null || !PasswordHasher.Verify(password, user.Password))
            return LoginResult.InvalidCredentials;

        if (user.IsLocked)
            return LoginResult.AccountLocked;

        if (!string.Equals(user.Role, "Agent", StringComparison.OrdinalIgnoreCase))
            return LoginResult.NotAnAgent;

        if (user.AgentID is null)
            return LoginResult.NoAgentProfile;

        return LoginResult.Success;
    }

    public static string Message(LoginResult r) => r switch
    {
        LoginResult.AccountLocked => "This account is locked. Please contact the administrator.",
        LoginResult.NotAnAgent => "Access denied: this portal is for Agent accounts only.",
        LoginResult.NoAgentProfile => "This account is not linked to an agent profile. Please contact the administrator.",
        _ => "Invalid username or password."
    };
}
