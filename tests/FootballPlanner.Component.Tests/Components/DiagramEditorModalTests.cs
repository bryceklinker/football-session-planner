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
    public async Task ClickPlayer_SetsActiveToolToPlayer()
    {
        var provider = SetupDialogProvider();
        var dialogService = Services.GetRequiredService<IDialogService>();

        var parameters = new DialogParameters
        {
            [nameof(DiagramEditorModal.InitialDiagramJson)] = (string?)null
        };
        await provider.InvokeAsync(() => dialogService.ShowAsync<DiagramEditorModal>("Edit Diagram", parameters,
            new DialogOptions { FullScreen = true }));

        provider.Find("[data-testid='tool-player']").Click();

        Assert.NotNull(provider.Find("[data-testid='tool-player']"));
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

        var undoBtn = provider.Find("[data-testid='undo']");
        undoBtn.Click();
    }

    [Fact]
    public async Task ClickClear_RemovesAllElements()
    {
        var provider = SetupDialogProvider();
        var dialogService = Services.GetRequiredService<IDialogService>();
        DiagramEditorState? capturedState = null;

        var parameters = new DialogParameters
        {
            [nameof(DiagramEditorModal.InitialDiagramJson)] = (string?)null,
            [nameof(DiagramEditorModal.OnStateCreated)] = EventCallback.Factory.Create<DiagramEditorState>(
                this, s => capturedState = s)
        };
        await provider.InvokeAsync(() => dialogService.ShowAsync<DiagramEditorModal>("Edit Diagram", parameters,
            new DialogOptions { FullScreen = true }));

        provider.Find("[data-testid='clear']").Click();

        Assert.NotNull(capturedState);
        Assert.Empty(capturedState!.Diagram.Cones);
    }
}
