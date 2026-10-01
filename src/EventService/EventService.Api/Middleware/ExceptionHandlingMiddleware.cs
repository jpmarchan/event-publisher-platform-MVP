using EventService.Api.Observability;
using EventService.Application.Idempotency;
using EventService.Domain;
using FluentValidation;

namespace EventService.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException ex)
        {
            logger.LogWarning("Validación fallida: {Errors}", ex.Errors.Select(e => e.ErrorMessage));
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Solicitud inválida.",
                details = ex.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage })
            });
        }
        catch (DomainException ex)
        {
            logger.LogWarning("Regla de negocio violada: {Message}", ex.Message);
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new { error = ex.Message });
        }
        catch (IdempotencyKeyReusedException ex)
        {
            logger.LogWarning("Idempotency-Key reutilizada con otro payload: {Message}", ex.Message);
            context.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
            await context.Response.WriteAsJsonAsync(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error no controlado procesando {Path}", context.Request.Path);
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Ocurrió un error inesperado.",
                correlationId = CorrelationIdMiddleware.GetCorrelationId(context)
            });
        }
    }
}
