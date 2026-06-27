using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SyncChain.API.DTOs;
using SyncChain.API.Exceptions;
using SyncChain.API.Services;

namespace SyncChain.API.ExceptionHandling;

public class ApiExceptionHandler : IExceptionHandler
{
    private readonly ILogger<ApiExceptionHandler> _logger;
    private readonly ISystemErrorLogService _systemErrorLog;

    public ApiExceptionHandler(
        ILogger<ApiExceptionHandler> logger,
        ISystemErrorLogService systemErrorLog)
    {
        _logger = logger;
        _systemErrorLog = systemErrorLog;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var apiException = MapException(exception);

        if (apiException.StatusCode >= StatusCodes.Status500InternalServerError)
            _logger.LogError(exception, "Unhandled API error. TraceId: {TraceId}", httpContext.TraceIdentifier);
        else
            _logger.LogWarning(exception, "API request conflict. TraceId: {TraceId}", httpContext.TraceIdentifier);

        await _systemErrorLog.LogAsync(new SystemErrorLogEntry(
            apiException.Code,
            apiException.Message,
            apiException.StatusCode,
            apiException.Details,
            exception), cancellationToken);

        httpContext.Response.StatusCode = apiException.StatusCode;
        await httpContext.Response.WriteAsJsonAsync(new ApiErrorResponse
        {
            Code = apiException.Code,
            Message = apiException.Message,
            Details = apiException.Details,
            TraceId = httpContext.TraceIdentifier
        }, cancellationToken);

        return true;
    }

    private static ApiException MapException(Exception exception)
    {
        if (exception is ApiException apiException)
            return apiException;

        if (exception is DbUpdateConcurrencyException)
            return new ConcurrencyConflictException("Du lieu da thay doi, vui long thu lai.");

        if (exception is DbUpdateException dbUpdate &&
            dbUpdate.InnerException is PostgresException innerPostgres &&
            (innerPostgres.SqlState == PostgresErrorCodes.SerializationFailure ||
             innerPostgres.SqlState == PostgresErrorCodes.DeadlockDetected))
        {
            return new ConcurrencyConflictException("Xung dot du lieu, vui long thu lai.");
        }

        if (exception is PostgresException postgres &&
            (postgres.SqlState == PostgresErrorCodes.SerializationFailure ||
             postgres.SqlState == PostgresErrorCodes.DeadlockDetected))
        {
            return new ConcurrencyConflictException("Xung dot du lieu, vui long thu lai.");
        }

        if (exception is KeyNotFoundException)
        {
            return new GenericApiException(
                StatusCodes.Status404NotFound,
                "NOT_FOUND",
                exception.Message);
        }

        if (exception is InvalidOperationException)
            return new ValidationApiException(exception.Message);

        return new GenericApiException(
            StatusCodes.Status500InternalServerError,
            "INTERNAL_ERROR",
            "Da xay ra loi he thong.");
    }

    private sealed class GenericApiException : ApiException
    {
        public GenericApiException(int statusCode, string code, string message)
            : base(statusCode, code, message)
        {
        }
    }
}
