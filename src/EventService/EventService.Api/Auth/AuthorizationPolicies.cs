using Microsoft.AspNetCore.Authorization;

namespace EventService.Api.Auth;

public static class AuthorizationPolicies
{
    public const string EventsRead = "events:read";
    public const string EventsWrite = "events:write";

    public const string AdminRole = "Admin";

    public static AuthorizationBuilder AddEventPolicies(this AuthorizationBuilder builder) => builder
        .AddPolicy(EventsRead, policy => policy
            .RequireAuthenticatedUser()
            .RequireScope(EventsRead))
        .AddPolicy(EventsWrite, policy => policy
            .RequireAuthenticatedUser()
            .RequireScope(EventsWrite)
            .RequireRole(AdminRole));

    // El claim scope es una lista separada por espacios.
    public static AuthorizationPolicyBuilder RequireScope(this AuthorizationPolicyBuilder builder, string scope) =>
        builder.RequireAssertion(context => context.User
            .FindAll("scope")
            .SelectMany(claim => claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Contains(scope));
}
