using Confluent.Kafka;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text;
using System.Text.Json;

namespace MToGo.OrderService.Middleware
{
    public class ApiExceptionHandlingMiddleware
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private readonly RequestDelegate _next;
        private readonly ILogger<ApiExceptionHandlingMiddleware> _logger;

        public ApiExceptionHandlingMiddleware(RequestDelegate next, ILogger<ApiExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                if (context.Response.HasStarted)
                {
                    _logger.LogWarning(ex, "Unhandled exception after response started");
                    throw;
                }

                var (statusCode, title, detail) = MapException(ex);

                var safePathForLog = SanitizeForLog(context.Request.Path.Value);
                _logger.LogError(ex, "Request failed with {StatusCode}: {Title}. Path={Path}", statusCode, title, safePathForLog);

                context.Response.Clear();
                context.Response.StatusCode = statusCode;
                context.Response.ContentType = "application/problem+json";

                var problem = new ProblemDetails
                {
                    Status = statusCode,
                    Title = title,
                    Detail = detail,
                    Instance = context.Request.Path
                };

                problem.Extensions["traceId"] = context.TraceIdentifier;

                await context.Response.WriteAsync(JsonSerializer.Serialize(problem, JsonOptions));
            }
        }

        private static string SanitizeForLog(string? input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input ?? string.Empty;
            }

            var builder = new StringBuilder(input.Length);
            foreach (var ch in input)
            {
                // Prevent log forging / line breaks in text-based log sinks.
                if (ch is '\r' or '\n' or '\u2028' or '\u2029')
                {
                    continue;
                }

                if (char.IsControl(ch))
                {
                    continue;
                }

                builder.Append(ch);
            }

            return builder.ToString();
        }

        private static (int statusCode, string title, string detail) MapException(Exception ex)
        {
            // Kafka
            if (ex is ProduceException<string, string> or KafkaException)
            {
                return ((int)HttpStatusCode.ServiceUnavailable,
                    "Kafka is unavailable",
                    "The service could not publish an event (Kafka is unavailable). Please try again later.");
            }

            // Downstream HTTP dependencies (via Gateway)
            if (ex is HttpRequestException)
            {
                return ((int)HttpStatusCode.BadGateway,
                    "Upstream dependency failed",
                    "A downstream service could not be reached or returned an error. Please try again later.");
            }

            // DB
            if (ex is DbUpdateException or DbUpdateConcurrencyException)
            {
                return ((int)HttpStatusCode.ServiceUnavailable,
                    "Database is unavailable",
                    "The service could not persist the request (database unavailable). Please try again later.");
            }

            // Timeouts / cancellations
            if (ex is TaskCanceledException or TimeoutException or OperationCanceledException)
            {
                return ((int)HttpStatusCode.GatewayTimeout,
                    "Request timed out",
                    "The request timed out. Please try again.");
            }

            // Default
            return ((int)HttpStatusCode.InternalServerError,
                "Unexpected server error",
                "An unexpected error occurred.");
        }
    }
}
