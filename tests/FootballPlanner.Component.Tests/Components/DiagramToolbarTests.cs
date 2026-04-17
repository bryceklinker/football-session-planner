using Bunit;
using FootballPlanner.Web.Components;
using FootballPlanner.Web.Models;
using MudBlazor;
using MudBlazor.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace FootballPlanner.Component.Tests.Components;

public class DiagramToolbarTests : TestContext
{
    public DiagramToolbarTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
        RenderComponent<MudPopoverProvider>();
    }

    private static DiagramEditorState DefaultState()
    {
        var state = new DiagramEditorState();
        state.Initialize(null);
        return state;
    }

    [Theory]
    [InlineData("Place player",   "player")]
    [InlineData("Place coach",    "coach")]
    [InlineData("Place cone",     "cone")]
    [InlineData("Place goal",     "goal")]
    [InlineData("Run arrow",      "arrow-run")]
    [InlineData("Pass arrow",     "arrow-pass")]
    [InlineData("Dribble arrow",  "arrow-dribble")]
    [InlineData("Move element",   "move")]
    [InlineData("Delete element", "delete")]
    public void ClickToolButton_SetsActiveToolAndFiresOnChanged(string ariaLabel, string expectedTool)
    {
        var state = DefaultState();
        var onChangedFired = false;
        var cut = RenderComponent<DiagramToolbar>(p =>
        {
            p.Add(x => x.State, state);
            p.Add(x => x.OnChanged, () => onChangedFired = true);
        });

        cut.Find($"[aria-label='{ariaLabel}']").Click();

        Assert.Equal(expectedTool, state.ActiveTool);
        Assert.True(onChangedFired);
    }

    [Fact]
    public void ToolButton_WhenActive_HasPrimaryColor()
    {
        var state = DefaultState();
        state.SetTool("player");
        var cut = RenderComponent<DiagramToolbar>(p => p.Add(x => x.State, state));

        var btn = cut.Find("[aria-label='Place player']");

        Assert.Contains("mud-primary-text", btn.ClassName);
    }

    [Fact]
    public void ToolButton_WhenInactive_DoesNotHavePrimaryColor()
    {
        var state = DefaultState();
        state.SetTool("cone"); // player is not active
        var cut = RenderComponent<DiagramToolbar>(p => p.Add(x => x.State, state));

        var btn = cut.Find("[aria-label='Place player']");

        Assert.DoesNotContain("mud-primary-text", btn.ClassName);
    }

    [Fact]
    public async Task FormatSelect_ChangingFormat_UpdatesStateAndFiresOnChanged()
    {
        var state = DefaultState();
        var onChangedFired = false;
        var cut = RenderComponent<DiagramToolbar>(p =>
        {
            p.Add(x => x.State, state);
            p.Add(x => x.OnChanged, () => onChangedFired = true);
        });

        var select = cut.FindComponent<MudSelect<PitchFormat>>();
        await cut.InvokeAsync(() => select.Instance.ValueChanged.InvokeAsync(PitchFormat.SevenVSevenFull));

        Assert.Equal(PitchFormat.SevenVSevenFull, state.Diagram.PitchFormat);
        Assert.True(onChangedFired);
    }

    [Fact]
    public async Task FormatSelect_SelectingCustom_ShowsWidthAndHeightFields()
    {
        var state = DefaultState();
        var cut = RenderComponent<DiagramToolbar>(p => p.Add(x => x.State, state));

        var select = cut.FindComponent<MudSelect<PitchFormat>>();
        await cut.InvokeAsync(() => select.Instance.ValueChanged.InvokeAsync(PitchFormat.Custom));

        // After re-render, width and height fields should be visible
        cut.Render();
        Assert.NotNull(cut.Find("[aria-label='Pitch width']"));
        Assert.NotNull(cut.Find("[aria-label='Pitch height']"));
    }

    [Fact]
    public async Task CustomWidthField_UpdatingValue_UpdatesStateCustomWidth()
    {
        var state = DefaultState();
        state.SetPitchFormat(PitchFormat.Custom, customWidth: 100.0, customHeight: 64.0);
        var cut = RenderComponent<DiagramToolbar>(p => p.Add(x => x.State, state));

        // fields[0] = width, fields[1] = height
        var fields = cut.FindComponents<MudNumericField<double>>();
        await cut.InvokeAsync(() => fields[0].Instance.ValueChanged.InvokeAsync(80.0));

        Assert.Equal(80.0, state.Diagram.CustomWidth);
        Assert.Equal(64.0, state.Diagram.CustomHeight); // unchanged
    }

    [Fact]
    public async Task CustomHeightField_UpdatingValue_UpdatesStateCustomHeight()
    {
        var state = DefaultState();
        state.SetPitchFormat(PitchFormat.Custom, customWidth: 100.0, customHeight: 64.0);
        var cut = RenderComponent<DiagramToolbar>(p => p.Add(x => x.State, state));

        var fields = cut.FindComponents<MudNumericField<double>>();
        await cut.InvokeAsync(() => fields[1].Instance.ValueChanged.InvokeAsync(50.0));

        Assert.Equal(100.0, state.Diagram.CustomWidth); // unchanged
        Assert.Equal(50.0, state.Diagram.CustomHeight);
    }
}
