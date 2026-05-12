using System.Net;
using System.Text.Json;
using FluentValidation;
using ProductService.Application.Common.Exceptions;
using ProductService.Domain.Exceptions;

namespace ProductService.Api.Middleware;

/// <summary>
/// Translates exceptions thrown deeper in the stack into HTTP problem responses.
/// Centralised here so handlers can throw and not worry about HTTP shape.
/// </summary>
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
            await WriteProblem(context, HttpStatusCode.BadRequest, "Validation failed",
                ex.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }));
        }
        catch (NotFoundException ex)
        {
            await WriteProblem(context, HttpStatusCode.NotFound, ex.Message);
        }
        catch (DomainException ex)
        {
            await WriteProblem(context, HttpStatusCode.BadRequest, ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception");
            await WriteProblem(context, HttpStatusCode.InternalServerError, "An unexpected error occurred.");
        }
    }

    private static async Task WriteProblem(HttpContext context, HttpStatusCode code, string title, object? errors = null)
    {
        context.Response.StatusCode = (int)code;
        context.Response.ContentType = "application/problem+json";
        var payload = new
        {
            status = (int)code,
            title,
            errors,
            traceId = context.TraceIdentifier
        };
        await context.Response.WriteAsync(JsonSerializer.Serialize(payload,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }
}
