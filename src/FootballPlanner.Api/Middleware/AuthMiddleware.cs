using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace FootballPlanner.Api.Middleware;

public class AuthMiddleware(IConfiguration configuration) : IFunctionsWorkerMiddleware
{
    private static string ExtractBearerToken(FunctionContext context)
    {
        var request = context.GetHttpContext()?.Request;
        var auth = request?.Headers.Authorization.FirstOrDefault();
        if (auth != null && auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return auth["Bearer ".Length..];
        return string.Empty;
    }

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var token = ExtractBearerToken(context);
        if (string.IsNullOrEmpty(token))
        {
            await RespondUnauthorized(context);
            return;
        }

        var domain = configuration["Auth0:Domain"];
        var audience = configuration["Auth0:Audience"];

        var configManager = new ConfigurationManager<OpenIdConnectConfiguration>(
            $"https://{domain}/.well-known/openid-configuration",
            new OpenIdConnectConfigurationRetriever());

        var openIdConfig = await configManager.GetConfigurationAsync();

        var validationParameters = new TokenValidationParameters
        {
            ValidIssuer = $"https://{domain}/",
            ValidAudience = audience,
            IssuerSigningKeys = openIdConfig.SigningKeys,
        };

        try
        {
            var handler = new JwtSecurityTokenHandler();
            handler.ValidateToken(token, validationParameters, out _);
            await next(context);
        }
        catch (SecurityTokenException)
        {
            await RespondUnauthorized(context);
        }
    }

    private static async Task RespondUnauthorized(FunctionContext context)
    {
        var httpContext = context.GetHttpContext();
        if (httpContext != null)
        {
            httpContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            await httpContext.Response.WriteAsJsonAsync(new { error = "Unauthorized" });
        }
    }
}
