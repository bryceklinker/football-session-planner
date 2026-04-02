using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Configuration;

namespace FootballPlanner.Api.Middleware;

public class CorsMiddleware(IConfiguration configuration) : IFunctionsWorkerMiddleware
{
    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var httpContext = context.GetHttpContext();
        if (httpContext != null)
        {
            var allowedOrigins = configuration["AllowedOrigins"] ?? "http://localhost:4280";
            var origins = allowedOrigins.Split(',');

            var origin = httpContext.Request.Headers.Origin.FirstOrDefault();
            if (!string.IsNullOrEmpty(origin) && origins.Contains(origin))
            {
                httpContext.Response.Headers["Access-Control-Allow-Origin"] = origin;
                httpContext.Response.Headers["Access-Control-Allow-Methods"] = "GET, POST, PUT, DELETE, OPTIONS";
                httpContext.Response.Headers["Access-Control-Allow-Headers"] = "Content-Type, Authorization";
            }

            if (httpContext.Request.Method == "OPTIONS")
            {
                httpContext.Response.StatusCode = 200;
                return;
            }
        }

        await next(context);
    }
}
