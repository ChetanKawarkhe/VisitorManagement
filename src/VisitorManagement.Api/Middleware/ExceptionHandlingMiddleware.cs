using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using VisitorManagement.Api.Models;
using VisitorManagement.Application.Exceptions;

namespace VisitorManagement.Api.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;

    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException)
            when (context.RequestAborted.IsCancellationRequested)
        {
            _logger.LogInformation(
                "Request was cancelled by the client.");
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleExceptionAsync(
        HttpContext context,
        Exception exception)
    {
        int statusCode;
        string title;
        string message;

        Dictionary<string, string[]> errors = new();

        switch (exception)
        {
            case ValidationException validationException:
                statusCode = StatusCodes.Status400BadRequest;
                title = "Validation Failed";
                message = "Please correct the highlighted fields.";

                errors = validationException.Errors
                    .GroupBy(x => x.PropertyName)
                    .ToDictionary(
                        group => group.Key,
                        group => group
                            .Select(x => x.ErrorMessage)
                            .ToArray());

                break;

            case NotFoundException:
                statusCode = StatusCodes.Status404NotFound;
                title = "Not Found";
                message = "The requested record was not found.";
                break;

            case ConcurrencyConflictException:
                statusCode = StatusCodes.Status409Conflict;
                title = "Concurrency Conflict";
                message =
                    "This record was modified by another user. Please refresh and try again.";
                break;

            case UnauthorizedAccessException:
                statusCode = StatusCodes.Status401Unauthorized;
                title = "Unauthorized";
                message = "Authentication is required.";
                break;

            default:
                statusCode = StatusCodes.Status500InternalServerError;
                title = "Internal Server Error";
                message =
                    "An unexpected error occurred. Please try again later.";
                break;
        }

        string correlationId = GetCorrelationId(context);

        if (statusCode >= 500)
        {
            _logger.LogError(
                exception,
                "Unhandled API exception. StatusCode: {StatusCode}. CorrelationId: {CorrelationId}",
                statusCode,
                correlationId);
        }
        else
        {
            _logger.LogWarning(
                "API request failed. StatusCode: {StatusCode}. ErrorType: {ErrorType}. CorrelationId: {CorrelationId}",
                statusCode,
                exception.GetType().Name,
                correlationId);
        }

        if (context.Response.HasStarted)
        {
            _logger.LogError(
                "Cannot write error response because the response has already started.");

            throw exception;
        }

        context.Response.Clear();

        context.Response.StatusCode = statusCode;

        var response = ApiErrorResponse.Create(
            statusCode,
            title,
            message,
            correlationId,
            errors);

        await context.Response.WriteAsJsonAsync(
            response,
            cancellationToken: context.RequestAborted);
    }

    private static string GetCorrelationId(HttpContext context)
    {
        return context.Items.TryGetValue(
                   CorrelationIdMiddleware.ItemKey,
                   out var value)
               && value is string correlationId
            ? correlationId
            : Guid.NewGuid().ToString("D");
    }
}