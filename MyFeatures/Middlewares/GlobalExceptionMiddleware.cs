using System.Net;
using System.Text.Json;

namespace MyFeatures.Middlewares
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;

        public GlobalExceptionMiddleware(RequestDelegate next)
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

        private Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var response = context.Response;
            response.ContentType = "application/json";

            response.StatusCode = exception switch
            {
                Core.Exceptions.NotFoundException => (int)HttpStatusCode.NotFound,
                //Core.Exceptions.BadRequestException => (int)HttpStatusCode.BadRequest,
                //Core.Exceptions.NotImplementedException => (int)HttpStatusCode.NotImplemented,
                _ => (int)HttpStatusCode.InternalServerError
            };

            var result = JsonSerializer.Serialize(new
            {
                statusCode = response.StatusCode,
                message = exception.Message
            });

            return response.WriteAsync(result);
        }
    }
}
