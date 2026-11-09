using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Domain.Common;

namespace TaskManager.Api.ErrorHandling;

/// <summary>
/// Centraliza el manejo de excepciones que se generan durante la ejecución de la API
/// y las transforma en respuestas con un formato Problem Details, evitando que cada controlador
/// tenga que implementar su propio manejo de errores
/// </summary>
/// <remarks>
/// Los errores se clasifican según su naturaleza: 400 para solicitudes con datos inválidos,
/// 404 cuando el recurso solicitado no existe, 409 cuando se presenta un conflicto,
/// 422 cuando no se cumple una regla de negocio y 500 para errores inesperados
/// Los detalles de los errores internos se conservan en los registros y no se exponen al cliente
/// </remarks>
internal sealed partial class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var problem = exception switch
        {
            ValidationException validation => CreateValidationProblem(validation),
            NotFoundException notFound => Create(StatusCodes.Status404NotFound, "Recurso no encontrado", notFound.Message, "not_found"),
            ConflictException conflict => Create(StatusCodes.Status409Conflict, "Conflicto", conflict.Message, "conflict"),
            DomainException domain => Create(StatusCodes.Status422UnprocessableEntity, "Regla de negocio incumplida", domain.Message, domain.Code),
            BadHttpRequestException badRequest => Create(badRequest.StatusCode, "Solicitud inválida", badRequest.Message, "bad_request"),
            _ => Create(StatusCodes.Status500InternalServerError, "Error interno del servidor",
                "Ocurrió un error inesperado. Si el problema persiste, contacte al administrador con el traceId.", "internal_error"),
        };

        if (problem.Status >= StatusCodes.Status500InternalServerError)
        {
            LogUnhandledException(logger, exception, httpContext.Request.Method, httpContext.Request.Path);
        }
        else
        {
            LogHandledException(logger, problem.Status ?? 0, exception.Message);
        }

        httpContext.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception,
        });
    }

    private static ProblemDetails Create(int status, string title, string detail, string code)
    {
        var problem = new ProblemDetails { Status = status, Title = title, Detail = detail };
        problem.Extensions["code"] = code;
        return problem;
    }

    private static ValidationProblemDetails CreateValidationProblem(ValidationException exception)
    {
        var errors = exception.Errors
            .GroupBy(failure => ToCamelCasePath(failure.PropertyName))
            .ToDictionary(group => group.Key, group => group.Select(failure => failure.ErrorMessage).Distinct().ToArray());

        var problem = new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Uno o más datos no son válidos.",
            Detail = "Revise los campos indicados en 'errors'.",
        };
        problem.Extensions["code"] = "validation_error";
        return problem;
    }

    /// <summary>
    /// Convierte las rutas de las propiedades a camelCase para mantener la misma nomenclatura
    /// que utiliza la respuesta JSON, incluyendo las propiedades anidadas y los índices de colecciones
    /// </summary>
    private static string ToCamelCasePath(string propertyPath) =>
        string.Join('.', propertyPath.Split('.').Select(JsonNamingPolicy.CamelCase.ConvertName));

    [LoggerMessage(Level = LogLevel.Error, Message = "Error no controlado procesando {Method} {Path}")]
    private static partial void LogUnhandledException(ILogger logger, Exception exception, string method, string path);

    [LoggerMessage(Level = LogLevel.Information, Message = "Solicitud rechazada con {StatusCode}: {Reason}")]
    private static partial void LogHandledException(ILogger logger, int statusCode, string reason);
}
