using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using NotificationService.Api.Auth;
using NotificationService.Api.Health;
using NotificationService.Api.Observability;
using NotificationService.Application;
using NotificationService.Application.Notifications.GetNotifications;
using NotificationService.Infrastructure;
using NotificationService.Infrastructure.Persistence;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {CorrelationId} {Message:lj}{NewLine}{Exception}"));

builder.Services.AddObservability(builder.Configuration, serviceName: "notificationservice-api");
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var oidcOptions = builder.Configuration.GetSection(OidcOptions.SectionName).Get<OidcOptions>()
    ?? throw new InvalidOperationException("Falta configurar la sección Oidc.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = oidcOptions.Authority;
        options.RequireHttpsMetadata = oidcOptions.RequireHttpsMetadata;
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = oidcOptions.Issuer,
            ValidAudience = oidcOptions.Audience,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorizationBuilder().AddNotificationPolicies();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "NotificationService API",
        Version = "v1",
        Description = "Consumer de EventCreated + endpoint de observabilidad (solo lectura)."
    });
    options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.OAuth2,
        Description = "Client Credentials en Keycloak (client_id: ops-monitor, secret de desarrollo: ops-monitor-dev-secret).",
        Flows = new OpenApiOAuthFlows
        {
            ClientCredentials = new OpenApiOAuthFlow
            {
                TokenUrl = new Uri($"{oidcOptions.Issuer}/protocol/openid-connect/token"),
                Scopes = new Dictionary<string, string>
                {
                    [AuthorizationPolicies.NotificationsRead] = "Consultar el log de notificaciones"
                }
            }
        }
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("oauth2", document)] = [AuthorizationPolicies.NotificationsRead]
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<NotificationServiceDbContext>();
    await dbContext.Database.MigrateAsync();
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthEndpoints();

app.MapGet("/notifications", async (ISender sender, CancellationToken cancellationToken) =>
{
    var result = await sender.Send(new GetNotificationsQuery(), cancellationToken);
    return Results.Ok(result);
}).RequireAuthorization(AuthorizationPolicies.NotificationsRead);

app.Run();
