using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

namespace FootballPlanner.Web.Services;

public class AuthRedirectHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            return await base.SendAsync(request, cancellationToken);
        }
        catch (AccessTokenNotAvailableException exception)
        {
            exception.Redirect();
            return new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized);
        }
    }
}
