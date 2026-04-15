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

public class DiagramEditorModalTests : TestContext
{
    public DiagramEditorModalTests()
    {
        Services.AddMudServices();
        Services.AddSingleton(new HttpClient { BaseAddress = new Uri("http://localhost") });
        Services.AddScoped<ApiClient>();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private IRenderedComponent<MudDialogProvider> SetupDialogProvider()
    {
        return RenderComponent<MudDialogProvider>();
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

        // Player tool button is not active before clicking
        var playerBtnBefore = provider.Find("[data-testid='tool-player'] button");
        Assert.DoesNotContain("mud-primary-text", playerBtnBefore.ClassName);

        provider.Find("[data-testid='tool-player']").Click();

        // After clicking, MudBlazor renders the primary Color as mud-primary-text class on the button
        var playerBtnAfter = provider.Find("[data-testid='tool-player'] button");
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

        // Undo button is disabled when the undo stack is empty
        var undoBtn = provider.Find("[data-testid='undo'] button");
        Assert.True(undoBtn.HasAttribute("disabled"));

        // Click does not throw even though stack is empty
        provider.Find("[data-testid='undo']").Click();
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

        // Should not throw; pitch SVG should still be present after clear
        provider.Find("[data-testid='clear']").Click();

        Assert.NotNull(provider.Find("svg"));
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
}
