using Apcloudpms.Application.Common;
using System.Net;
using System.Text.Json;

namespace Apcloudpms.API.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;
        //public ExceptionMiddleware(RequestDelegate next)
        //{
        //    _next = next;
        //}
        public ExceptionMiddleware(RequestDelegate next,ILogger<ExceptionMiddleware> logger)
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
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = ex switch
                {
                    ForbiddenAccessException => StatusCodes.Status403Forbidden,
                    ArgumentException => StatusCodes.Status400BadRequest,
                    _ => StatusCodes.Status500InternalServerError
                };
                var response = new ApiResponse<object>
                {
                    Success = false,
                    Message = ex switch
                    {
                        ForbiddenAccessException or ArgumentException => ex.Message,
                        _ => "An unexpected server error occurred."
                    },
                    Data = null
                };

                var json = JsonSerializer.Serialize(response);
                _logger.LogError(ex, ex.Message);
                await context.Response.WriteAsync(json);
            }
        }
    }
}
