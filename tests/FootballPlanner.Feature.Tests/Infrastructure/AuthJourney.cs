using Microsoft.Playwright;

namespace FootballPlanner.Feature.Tests.Infrastructure;

public class AuthJourney(IPage page, string baseUrl)
{
    public async Task LoginAsync()
    {
        var email = Environment.GetEnvironmentVariable("AUTH0_TEST_USER_EMAIL");
        if (string.IsNullOrEmpty(email))
            throw new InvalidOperationException(
                "AUTH0_TEST_USER_EMAIL is not set. See .env.test.example for setup instructions.");

        var password = Environment.GetEnvironmentVariable("AUTH0_TEST_USER_PASSWORD");
        if (string.IsNullOrEmpty(password))
            throw new InvalidOperationException(
                "AUTH0_TEST_USER_PASSWORD is not set. See .env.test.example for setup instructions.");

        await page.GotoAsync(baseUrl);

        // Wait for Auth0 redirect or the app (if storage state was somehow already valid)
        await page.WaitForURLAsync(
            url => url.Contains("auth0.com") || url.StartsWith(baseUrl),
            new() { Timeout = 30_000 });

        if (!page.Url.Contains("auth0.com"))
            return;

        // Auth0 Universal Login: enter email, then password (two-step flow)
        // If your tenant uses Classic Login (single form), replace with:
        //   await page.Locator("input[type='email']").FillAsync(email);
        //   await page.Locator("input[type='password']").FillAsync(password);
        //   await page.Locator("button[type='submit']").ClickAsync();
        await page.Locator("input[name='username']").FillAsync(email);
        await page.GetByRole(AriaRole.Button, new() { Name = "Continue" }).ClickAsync();
        await page.Locator("input[name='password']").FillAsync(password);
        await page.GetByRole(AriaRole.Button, new() { Name = "Continue" }).ClickAsync();

        await page.WaitForURLAsync(url => url.StartsWith(baseUrl), new() { Timeout = 30_000 });
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }
}
