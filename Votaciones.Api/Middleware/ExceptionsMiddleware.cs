using System.Net;
using System.Text.Json;

namespace Votaciones.Api.Middleware
{
    public class ExceptionsMiddleware
    {
        private readonly RequestDelegate _next;

        public ExceptionsMiddleware(RequestDelegate next) 
        { 
            _next = next; 
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var statusCode = exception switch
            {
                KeyNotFoundException => HttpStatusCode.NotFound,
                ArgumentException => HttpStatusCode.BadRequest,
                InvalidOperationException => HttpStatusCode.BadRequest,
                _ => HttpStatusCode.InternalServerError
            };

            var message = statusCode == HttpStatusCode.InternalServerError
                ? "Ocurrió un error interno en el servidor." : exception.Message;

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;

            var response = new { message };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }

    }
}
