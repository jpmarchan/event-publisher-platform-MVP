using System.Threading.RateLimiting;
using EventService.Api.Auth;
using EventService.Api.Events;
using EventService.Api.Health;
using EventService.Api.Middleware;
using EventService.Api.Observability;
using EventService.Application;
using EventService.Infrastructure;
using EventService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {CorrelationId} {Message:lj}{NewLine}{Exception}"));

builder.Services.Configure<OidcOptions>(builder.Configuration.GetSection(OidcOptions.SectionName));
var oidcOptions = builder.Configuration.GetSection(OidcOptions.SectionName).Get<OidcOptions>()
    ?? throw new InvalidOperationException("Falta configurar la sección Oidc.");

builder.Services.AddObservability(builder.Configuration, serviceName: "eventservice-api");
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = oidcOptions.Authority;
        options.RequireHttpsMetadata = oidcOptions.RequireHttpsMetadata;
        // Mantiene los nombres originales de los claims (sub, scope, roles).
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = oidcOptions.Issuer,
            ValidAudience = oidcOptions.Audience,
            NameClaimType = "preferred_username",
            RoleClaimType = "roles",
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorizationBuilder().AddEventPolicies();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy(EventsEndpoints.CreateEventRateLimiterPolicy, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            // Límite por usuario; sin usuario, por IP.
            partitionKey: httpContext.User.FindFirst("sub")?.Value
                ?? httpContext.Connection.RemoteIpAddress?.ToString()
                ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
    options.OnRejected = (context, _) =>
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            context.HttpContext.Response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString();
        return ValueTask.CompletedTask;
    };
});

const string FrontendCorsPolicy = "frontend-dev";
builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy => policy
        .WithOrigins("http://localhost:5173")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .WithExposedHeaders(EventsEndpoints.IdempotentReplayedHeader, "Retry-After", CorrelationIdMiddleware.HeaderName));
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "EventService API",
        Version = "v1",
        Description = "Gestión de eventos y zonas."
    });
    options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.OAuth2,
        Description = "Login OIDC en Keycloak (usuarios demo: admin / Admin123!, user / User123!).",
        Flows = new OpenApiOAuthFlows
        {
            AuthorizationCode = new OpenApiOAuthFlow
            {
                AuthorizationUrl = new Uri($"{oidcOptions.Issuer}/protocol/openid-connect/auth"),
                TokenUrl = new Uri($"{oidcOptions.Issuer}/protocol/openid-connect/token"),
                Scopes = new Dictionary<string, string>
                {
                    ["openid"] = "Identidad OIDC",
                    [AuthorizationPolicies.EventsRead] = "Consultar eventos",
                    [AuthorizationPolicies.EventsWrite] = "Crear eventos (requiere rol Admin)"
                }
            }
        }
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("oauth2", document)] =
            ["openid", AuthorizationPolicies.EventsRead, AuthorizationPolicies.EventsWrite]
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<EventServiceDbContext>();
    await dbContext.Database.MigrateAsync();
}

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.OAuthClientId("swagger-ui");
    options.OAuthUsePkce();
    options.OAuthScopes("openid", AuthorizationPolicies.EventsRead, AuthorizationPolicies.EventsWrite);
});

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseCors(FrontendCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapHealthEndpoints();
app.MapEventsEndpoints();

app.Run();
