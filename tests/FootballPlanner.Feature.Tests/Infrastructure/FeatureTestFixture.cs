using FootballPlanner.Feature.Tests.Journeys;
using Microsoft.Playwright;

namespace FootballPlanner.Feature.Tests.Infrastructure;

public class FeatureTestFixture : IAsyncLifetime
{
    public const string BaseUrl = "http://localhost:4280";

    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private string? _storageStatePath;
    private readonly List<IBrowserContext> _contexts = [];

    // Set by NewPageAsync() — available for direct assertions in tests
    public IPage Page { get; private set; } = null!;

    // Journey properties — rebound to the fresh page on each NewPageAsync() call
    public PhaseJourney PhaseJourney { get; private set; } = null!;
    public FocusJourney FocusJourney { get; private set; } = null!;
    public ActivityJourney ActivityJourney { get; private set; } = null!;
    public SessionJourney SessionJourney { get; private set; } = null!;
    public SessionEditorJourney SessionEditorJourney { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new() { Headless = true });

        _storageStatePath = Path.Combine(Path.GetTempPath(), $"auth-state-{Guid.NewGuid()}.json");

        var setupContext = await _browser.NewContextAsync();
        try
        {
            var page = await setupContext.NewPageAsync();
            await new AuthJourney(page, BaseUrl).LoginAsync();
            await setupContext.StorageStateAsync(new() { Path = _storageStatePath });
        }
        finally
        {
            await setupContext.DisposeAsync();
        }
    }

    /// <summary>
    /// Creates a fresh authenticated browser context and page for the current test,
    /// then rebinds all journey properties to that page.
    /// Call once at the start of each [Fact].
    /// </summary>
    public async Task NewPageAsync()
    {
        var context = await _browser!.NewContextAsync(new()
        {
            StorageStatePath = _storageStatePath,
            RecordVideoDir = "playwright-videos",
            RecordVideoSize = new RecordVideoSize { Width = 1280, Height = 720 }
        });
        _contexts.Add(context);
        Page = await context.NewPageAsync();
        PhaseJourney = new PhaseJourney(Page);
        FocusJourney = new FocusJourney(Page);
        ActivityJourney = new ActivityJourney(Page);
        SessionJourney = new SessionJourney(Page);
        SessionEditorJourney = new SessionEditorJourney(Page);
    }

    public async Task DisposeAsync()
    {
        foreach (var context in _contexts)
            await context.DisposeAsync();

        if (_storageStatePath != null && File.Exists(_storageStatePath))
            File.Delete(_storageStatePath);

        if (_browser != null)
            await _browser.DisposeAsync();

        _playwright?.Dispose();
    }
}
