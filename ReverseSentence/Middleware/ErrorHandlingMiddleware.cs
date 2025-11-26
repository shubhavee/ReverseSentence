using System.Net;
using System.Text.Json;

namespace ReverseSentence.Middleware
{
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate next;
        private readonly ILogger<ErrorHandlingMiddleware> logger;

        public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
        {
            this.next = next;
            this.logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await next(context);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An unhandled exception occurred");
                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var code = HttpStatusCode.InternalServerError;
            string errorMessage;

            if (exception is ArgumentException or ArgumentNullException)
            {
                code = HttpStatusCode.BadRequest;
                errorMessage = exception.Message; // Safe to expose validation errors
            }
            else if (exception is UnauthorizedAccessException)
            {
                code = HttpStatusCode.Unauthorized;
                errorMessage = "Unauthorized access";
            }
            else
            {
                // Don't expose internal error details to clients
                errorMessage = "An unexpected error occurred. Please try again later.";
            }

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)code;

            var response = new
            {
                error = errorMessage,
                statusCode = (int)code,
                timestamp = DateTime.UtcNow
            };

            var result = JsonSerializer.Serialize(response);

            return context.Response.WriteAsync(result);
        }
    }
}
