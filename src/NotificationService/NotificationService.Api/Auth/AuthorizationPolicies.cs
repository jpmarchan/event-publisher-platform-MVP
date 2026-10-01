using Microsoft.AspNetCore.Authorization;

namespace NotificationService.Api.Auth;

public static class AuthorizationPolicies
{
    public const string NotificationsRead = "notifications:read";

    public static AuthorizationBuilder AddNotificationPolicies(this AuthorizationBuilder builder) => builder
        .AddPolicy(NotificationsRead, policy => policy
            .RequireAuthenticatedUser()
            .RequireScope(NotificationsRead));

    // El claim scope es una lista separada por espacios.
    public static AuthorizationPolicyBuilder RequireScope(this AuthorizationPolicyBuilder builder, string scope) =>
        builder.RequireAssertion(context => context.User
            .FindAll("scope")
            .SelectMany(claim => claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Contains(scope));
}
