using System.Text.Json;
using Bunit;
using FootballPlanner.Web.Components;
using FootballPlanner.Web.Models;
using FootballPlanner.Web.Services;
using MudBlazor;
using MudBlazor.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace FootballPlanner.Component.Tests.Components;

public class DiagramEditorModalTests : BunitContext, IAsyncLifetime
{
    private readonly TestHttpMessageHandler _httpHandler = new();

    public DiagramEditorModalTests()
    {
        Services.AddMudServices();
        Services.AddSingleton(new HttpClient(_httpHandler) { BaseAddress = new Uri("http://localhost") });
        Services.AddScoped<ApiClient>();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public async Task DisposeAsync() => await ((IAsyncDisposable)this).DisposeAsync();
    protected override void Dispose(bool disposing) { }

    private IRenderedComponent<MudDialogProvider> SetupDialogProvider()
        => Render<MudDialogProvider>();

    private sealed class TestHttpMessageHandler : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
        }
    }

    [Fact]
    public async Task OpenModal_ShowsEditorContent()
    {
        var provider = SetupDialogProvider();
        var dialogService = Services.GetRequiredService<IDialogService>();

        var parameters = new DialogParameters
        {
            [nameof(DiagramEditorModal.InitialDiagramJson)] = (string?)null
        };
        await provider.InvokeAsync(() => dialogService.ShowAsync<DiagramEditorModal>("Edit Diagram", parameters,
            new DialogOptions { FullScreen = true }));

        Assert.Contains("player", provider.Markup.ToLower());
    }

    [Fact]
    public async Task ClickPlayer_ActivatesPlayerTool()
    {
        var provider = SetupDialogProvider();
        var dialogService = Services.GetRequiredService<IDialogService>();
        var parameters = new DialogParameters
        {
            [nameof(DiagramEditorModal.InitialDiagramJson)] = (string?)null
        };
        await provider.InvokeAsync(() => dialogService.ShowAsync<DiagramEditorModal>("Edit Diagram", parameters,
            new DialogOptions { FullScreen = true }));

        var playerBtnBefore = provider.Find("[aria-label='Place player']");
        Assert.DoesNotContain("mud-primary-text", playerBtnBefore.ClassName);

        provider.Find("[aria-label='Place player']").Click();

        var playerBtnAfter = provider.Find("[aria-label='Place player']");
        Assert.Contains("mud-primary-text", playerBtnAfter.ClassName);
    }

    [Fact]
    public async Task ClickUndo_WhenNothingToUndo_DoesNotThrow()
    {
        var provider = SetupDialogProvider();
        var dialogService = Services.GetRequiredService<IDialogService>();
        var parameters = new DialogParameters
        {
            [nameof(DiagramEditorModal.InitialDiagramJson)] = (string?)null
        };
        await provider.InvokeAsync(() => dialogService.ShowAsync<DiagramEditorModal>("Edit Diagram", parameters,
            new DialogOptions { FullScreen = true }));

        var undoBtn = provider.Find("[aria-label='Undo']");
        Assert.True(undoBtn.HasAttribute("disabled"));

        provider.Find("[aria-label='Undo']").Click();
    }

    [Fact]
    public async Task ClickClear_DoesNotThrow()
    {
        var provider = SetupDialogProvider();
        var dialogService = Services.GetRequiredService<IDialogService>();
        var parameters = new DialogParameters
        {
            [nameof(DiagramEditorModal.InitialDiagramJson)] = (string?)null
        };
        await provider.InvokeAsync(() => dialogService.ShowAsync<DiagramEditorModal>("Edit Diagram", parameters,
            new DialogOptions { FullScreen = true }));

        provider.Find("[aria-label='Clear']").Click();

        Assert.NotNull(provider.Find("svg[id^='pitch-']"));
    }

    [Fact]
    public async Task InitialDiagramJson_LoadsExistingDiagram()
    {
        var provider = SetupDialogProvider();
        var dialogService = Services.GetRequiredService<IDialogService>();

        var diagram = new DiagramModel(
            PitchFormat.ElevenVElevenFull, null, null,
            new List<DiagramTeam>
            {
                new("t1", "Red", "#e94560", new List<PlayerElement> { new("9", 30, 40) })
            },
            new List<CoachElement>(),
            new List<ConeElement>(),
            new List<GoalElement>(),
            new List<ArrowElement>());
        var json = JsonSerializer.Serialize(diagram,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        var parameters = new DialogParameters
        {
            [nameof(DiagramEditorModal.InitialDiagramJson)] = json
        };
        await provider.InvokeAsync(() => dialogService.ShowAsync<DiagramEditorModal>("Edit Diagram", parameters,
            new DialogOptions { FullScreen = true }));

        // The SVG should contain a circle for the player element
        Assert.Contains("<circle", provider.Markup);
    }

    [Fact]
    public async Task ClickCancel_ClosesDialogAsCanceled()
    {
        var provider = SetupDialogProvider();
        var dialogService = Services.GetRequiredService<IDialogService>();
        var parameters = new DialogParameters
        {
            [nameof(DiagramEditorModal.ActivityId)] = 1,
            [nameof(DiagramEditorModal.InitialDiagramJson)] = (string?)null
        };
        var dialogRef = await provider.InvokeAsync(() =>
            dialogService.ShowAsync<DiagramEditorModal>("Edit Diagram", parameters,
                new DialogOptions { FullScreen = true }));

        provider.Find("[aria-label='Cancel']").Click();

        var result = await dialogRef.Result;
        Assert.True(result.Canceled);
        Assert.Null(_httpHandler.LastRequest); // no API call made
    }

    [Fact]
    public async Task ClickSave_CallsApiAndClosesDialogWithJson()
    {
        var provider = SetupDialogProvider();
        var dialogService = Services.GetRequiredService<IDialogService>();
        var parameters = new DialogParameters
        {
            [nameof(DiagramEditorModal.ActivityId)] = 42,
            [nameof(DiagramEditorModal.InitialDiagramJson)] = (string?)null
        };
        var dialogRef = await provider.InvokeAsync(() =>
            dialogService.ShowAsync<DiagramEditorModal>("Edit Diagram", parameters,
                new DialogOptions { FullScreen = true }));

        await provider.InvokeAsync(() => provider.Find("[aria-label='Save Diagram']").Click());

        Assert.NotNull(_httpHandler.LastRequest);
        Assert.Equal(HttpMethod.Put, _httpHandler.LastRequest!.Method);
        Assert.Contains("activities/42/diagram", _httpHandler.LastRequest.RequestUri!.ToString());

        var result = await dialogRef.Result;
        Assert.False(result.Canceled);
        Assert.IsType<string>(result.Data);
    }

    [Fact]
    public async Task SelectPlayerTool_ThenClickCanvas_PlacesPlayerCircleOnSvg()
    {
        var provider = SetupDialogProvider();
        var dialogService = Services.GetRequiredService<IDialogService>();
        var parameters = new DialogParameters
        {
            [nameof(DiagramEditorModal.InitialDiagramJson)] = (string?)null
        };
        await provider.InvokeAsync(() => dialogService.ShowAsync<DiagramEditorModal>("Edit Diagram", parameters,
            new DialogOptions { FullScreen = true }));

        provider.Find("[aria-label='Place player']").Click();
        provider.Find("svg[id^='pitch-']").Click();

        Assert.NotEmpty(provider.FindAll("[data-element^='teams']"));
    }

    [Fact]
    public async Task SelectConeTool_ThenClickCanvas_PlacesConePolygonOnSvg()
    {
        var provider = SetupDialogProvider();
        var dialogService = Services.GetRequiredService<IDialogService>();
        var parameters = new DialogParameters
        {
            [nameof(DiagramEditorModal.InitialDiagramJson)] = (string?)null
        };
        await provider.InvokeAsync(() => dialogService.ShowAsync<DiagramEditorModal>("Edit Diagram", parameters,
            new DialogOptions { FullScreen = true }));

        provider.Find("[aria-label='Place cone']").Click();
        provider.Find("svg[id^='pitch-']").Click();

        Assert.NotEmpty(provider.FindAll("polygon[data-element^='cones']"));
    }

    [Fact]
    public async Task SelectArrowRunTool_ThenClickCanvasTwice_PlacesArrowPathOnSvg()
    {
        var provider = SetupDialogProvider();
        var dialogService = Services.GetRequiredService<IDialogService>();
        var parameters = new DialogParameters
        {
            [nameof(DiagramEditorModal.InitialDiagramJson)] = (string?)null
        };
        await provider.InvokeAsync(() => dialogService.ShowAsync<DiagramEditorModal>("Edit Diagram", parameters,
            new DialogOptions { FullScreen = true }));

        provider.Find("[aria-label='Run arrow']").Click();
        provider.Find("svg[id^='pitch-']").Click(); // sets ArrowStartPoint
        provider.Find("svg[id^='pitch-']").Click(); // completes arrow

        Assert.NotEmpty(provider.FindAll("path[data-element^='arrows']"));
    }

    [Fact]
    public async Task UndoAfterPlacement_RemovesElement()
    {
        var provider = SetupDialogProvider();
        var dialogService = Services.GetRequiredService<IDialogService>();
        var parameters = new DialogParameters
        {
            [nameof(DiagramEditorModal.InitialDiagramJson)] = (string?)null
        };
        await provider.InvokeAsync(() => dialogService.ShowAsync<DiagramEditorModal>("Edit Diagram", parameters,
            new DialogOptions { FullScreen = true }));

        provider.Find("[aria-label='Place cone']").Click();
        provider.Find("svg[id^='pitch-']").Click();
        Assert.NotEmpty(provider.FindAll("polygon[data-element^='cones']"));

        provider.Find("[aria-label='Undo']").Click();

        Assert.Empty(provider.FindAll("polygon[data-element^='cones']"));
    }

    [Fact]
    public async Task RedoAfterUndo_RestoresElement()
    {
        var provider = SetupDialogProvider();
        var dialogService = Services.GetRequiredService<IDialogService>();
        var parameters = new DialogParameters
        {
            [nameof(DiagramEditorModal.InitialDiagramJson)] = (string?)null
        };
        await provider.InvokeAsync(() => dialogService.ShowAsync<DiagramEditorModal>("Edit Diagram", parameters,
            new DialogOptions { FullScreen = true }));

        provider.Find("[aria-label='Place cone']").Click();
        provider.Find("svg[id^='pitch-']").Click();
        provider.Find("[aria-label='Undo']").Click();
        Assert.Empty(provider.FindAll("polygon[data-element^='cones']"));

        provider.Find("[aria-label='Redo']").Click();

        Assert.NotEmpty(provider.FindAll("polygon[data-element^='cones']"));
    }
}
