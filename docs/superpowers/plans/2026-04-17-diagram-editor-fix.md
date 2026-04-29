# Diagram Editor Fix Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix broken save/cancel/placement in the diagram editor by extracting a `DiagramToolbar` component with correct event wiring, adding a pitch format selector, and adding comprehensive tests using `aria-label` selectors.

**Architecture:** The root cause is that all interactive elements use `<span @onclick><MudButton/></span>` wrappers — MudBlazor handles clicks internally, so the outer span's `@onclick` never fires in the browser. The fix moves all handlers onto `OnClick` parameters of `MudButton`/`MudIconButton` directly, using `aria-label` as the accessible and testable selector (replacing `data-testid`). The toolbar is extracted into `DiagramToolbar.razor` with `EventCallback OnChanged` to notify the parent to re-render.

**Tech Stack:** Blazor WebAssembly, MudBlazor 9.3.0, bUnit 1.38.x, xUnit

---

## File Structure

| File | Change |
|------|--------|
| `src/FootballPlanner.Web/Models/DiagramEditorState.cs` | Add `SetPitchFormat` method |
| `src/FootballPlanner.Web/Components/DiagramToolbar.razor` | **Create** — pitch format select + all 9 tool buttons |
| `src/FootballPlanner.Web/Components/DiagramEditorModal.razor` | Use `DiagramToolbar`; fix Save/Cancel/Undo/Redo/Clear buttons |
| `tests/FootballPlanner.Component.Tests/Models/DiagramEditorStateTests.cs` | Add 3 `SetPitchFormat` tests |
| `tests/FootballPlanner.Component.Tests/Components/DiagramToolbarTests.cs` | **Create** — 8 tests covering tools and format selector |
| `tests/FootballPlanner.Component.Tests/Components/DiagramEditorModalTests.cs` | Add 7 new tests; update 3 existing selectors |

---

### Task 1: SetPitchFormat on DiagramEditorState

**Files:**
- Modify: `src/FootballPlanner.Web/Models/DiagramEditorState.cs`
- Test: `tests/FootballPlanner.Component.Tests/Models/DiagramEditorStateTests.cs`

- [ ] **Step 1: Write the three failing tests**

Add to the bottom of the existing `DiagramEditorStateTests.cs`:

```csharp
[Fact]
public void SetPitchFormat_UpdatesDiagramPitchFormat()
{
    var state = new DiagramEditorState();
    state.SetPitchFormat(PitchFormat.SevenVSevenFull);
    Assert.Equal(PitchFormat.SevenVSevenFull, state.Diagram.PitchFormat);
}

[Fact]
public void SetPitchFormat_IsUndoable()
{
    var state = new DiagramEditorState();
    Assert.False(state.CanUndo);
    state.SetPitchFormat(PitchFormat.NineVNineFull);
    Assert.True(state.CanUndo);
    state.Undo();
    Assert.Equal(PitchFormat.ElevenVElevenFull, state.Diagram.PitchFormat);
}

[Fact]
public void SetPitchFormat_Custom_SetsWidthAndHeight()
{
    var state = new DiagramEditorState();
    state.SetPitchFormat(PitchFormat.Custom, customWidth: 80.0, customHeight: 50.0);
    Assert.Equal(PitchFormat.Custom, state.Diagram.PitchFormat);
    Assert.Equal(80.0, state.Diagram.CustomWidth);
    Assert.Equal(50.0, state.Diagram.CustomHeight);
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test tests/FootballPlanner.Component.Tests --filter "SetPitchFormat" -v
```

Expected: 3 failures — `DiagramEditorState` does not contain `SetPitchFormat`.

- [ ] **Step 3: Add `SetPitchFormat` to `DiagramEditorState.cs`**

Add this method after the `SetActiveTeam` method (after line 34 in the current file):

```csharp
public void SetPitchFormat(PitchFormat format, double? customWidth = null, double? customHeight = null)
{
    PushUndo();
    Diagram = Diagram with { PitchFormat = format, CustomWidth = customWidth, CustomHeight = customHeight };
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test tests/FootballPlanner.Component.Tests --filter "SetPitchFormat" -v
```

Expected: 3 tests pass.

- [ ] **Step 5: Commit**

```bash
git add src/FootballPlanner.Web/Models/DiagramEditorState.cs \
        tests/FootballPlanner.Component.Tests/Models/DiagramEditorStateTests.cs
git commit -m "feat: add SetPitchFormat to DiagramEditorState"
```

---

### Task 2: DiagramToolbar Component

**Files:**
- Create: `src/FootballPlanner.Web/Components/DiagramToolbar.razor`
- Create: `tests/FootballPlanner.Component.Tests/Components/DiagramToolbarTests.cs`

- [ ] **Step 1: Create the failing tests file**

Create `tests/FootballPlanner.Component.Tests/Components/DiagramToolbarTests.cs`:

```csharp
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
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test tests/FootballPlanner.Component.Tests --filter "DiagramToolbarTests" -v
```

Expected: all failures — `DiagramToolbar` does not exist.

- [ ] **Step 3: Create `DiagramToolbar.razor`**

Create `src/FootballPlanner.Web/Components/DiagramToolbar.razor`:

```razor
@using FootballPlanner.Web.Models

<div style="width:150px;display:flex;flex-direction:column;gap:4px;align-items:center;padding:4px 0;">

    <MudSelect T="PitchFormat"
               aria-label="Pitch format"
               Value="@State.Diagram.PitchFormat"
               ValueChanged="@HandleFormatChanged"
               Dense="true"
               Margin="Margin.Dense"
               Style="width:100%">
        <MudSelectItem Value="PitchFormat.ElevenVElevenFull">11v11 Full</MudSelectItem>
        <MudSelectItem Value="PitchFormat.ElevenVElevenHalf">11v11 Half</MudSelectItem>
        <MudSelectItem Value="PitchFormat.NineVNineFull">9v9 Full</MudSelectItem>
        <MudSelectItem Value="PitchFormat.NineVNineHalf">9v9 Half</MudSelectItem>
        <MudSelectItem Value="PitchFormat.SevenVSevenFull">7v7 Full</MudSelectItem>
        <MudSelectItem Value="PitchFormat.SevenVSevenHalf">7v7 Half</MudSelectItem>
        <MudSelectItem Value="PitchFormat.Custom">Custom</MudSelectItem>
    </MudSelect>

    @if (State.Diagram.PitchFormat == PitchFormat.Custom)
    {
        <MudNumericField T="double"
                         aria-label="Pitch width"
                         Label="Width (m)"
                         Value="@(State.Diagram.CustomWidth ?? 100.0)"
                         ValueChanged="@HandleCustomWidthChanged"
                         Min="10" Max="200"
                         Dense="true" Margin="Margin.Dense"
                         Style="width:100%" />
        <MudNumericField T="double"
                         aria-label="Pitch height"
                         Label="Height (m)"
                         Value="@(State.Diagram.CustomHeight ?? 64.0)"
                         ValueChanged="@HandleCustomHeightChanged"
                         Min="10" Max="200"
                         Dense="true" Margin="Margin.Dense"
                         Style="width:100%" />
    }

    <MudDivider />
    <MudText Typo="Typo.caption" Color="Color.Secondary">Place</MudText>

    <MudIconButton aria-label="Place player"
                   Icon="@Icons.Material.Filled.Person"
                   Size="Size.Small"
                   Color="@(State.ActiveTool == "player" ? Color.Primary : Color.Default)"
                   OnClick="@(() => HandleToolClick("player"))" />
    <MudIconButton aria-label="Place coach"
                   Icon="@Icons.Material.Filled.SportsHandball"
                   Size="Size.Small"
                   Color="@(State.ActiveTool == "coach" ? Color.Primary : Color.Default)"
                   OnClick="@(() => HandleToolClick("coach"))" />
    <MudIconButton aria-label="Place cone"
                   Icon="@Icons.Material.Filled.ChangeHistory"
                   Size="Size.Small"
                   Color="@(State.ActiveTool == "cone" ? Color.Primary : Color.Default)"
                   OnClick="@(() => HandleToolClick("cone"))" />
    <MudIconButton aria-label="Place goal"
                   Icon="@Icons.Material.Filled.SportsSoccer"
                   Size="Size.Small"
                   Color="@(State.ActiveTool == "goal" ? Color.Primary : Color.Default)"
                   OnClick="@(() => HandleToolClick("goal"))" />

    <MudDivider />
    <MudText Typo="Typo.caption" Color="Color.Secondary">Arrow</MudText>

    <MudIconButton aria-label="Run arrow"
                   Icon="@Icons.Material.Filled.ArrowForward"
                   Size="Size.Small"
                   Color="@(State.ActiveTool == "arrow-run" ? Color.Primary : Color.Default)"
                   OnClick="@(() => HandleToolClick("arrow-run"))" />
    <MudIconButton aria-label="Pass arrow"
                   Icon="@Icons.Material.Filled.ArrowRightAlt"
                   Size="Size.Small"
                   Color="@(State.ActiveTool == "arrow-pass" ? Color.Primary : Color.Default)"
                   OnClick="@(() => HandleToolClick("arrow-pass"))" />
    <MudIconButton aria-label="Dribble arrow"
                   Icon="@Icons.Material.Filled.Waves"
                   Size="Size.Small"
                   Color="@(State.ActiveTool == "arrow-dribble" ? Color.Primary : Color.Default)"
                   OnClick="@(() => HandleToolClick("arrow-dribble"))" />

    <MudDivider />

    <MudIconButton aria-label="Move element"
                   Icon="@Icons.Material.Filled.OpenWith"
                   Size="Size.Small"
                   Color="@(State.ActiveTool == "move" ? Color.Primary : Color.Default)"
                   OnClick="@(() => HandleToolClick("move"))" />
    <MudIconButton aria-label="Delete element"
                   Icon="@Icons.Material.Filled.Delete"
                   Size="Size.Small"
                   Color="@(State.ActiveTool == "delete" ? Color.Primary : Color.Default)"
                   OnClick="@(() => HandleToolClick("delete"))" />

</div>

@code {
    [Parameter, EditorRequired] public DiagramEditorState State { get; set; } = null!;
    [Parameter] public EventCallback OnChanged { get; set; }

    private async Task HandleToolClick(string tool)
    {
        State.SetTool(tool);
        await OnChanged.InvokeAsync();
    }

    private async Task HandleFormatChanged(PitchFormat format)
    {
        State.SetPitchFormat(format);
        await OnChanged.InvokeAsync();
    }

    private async Task HandleCustomWidthChanged(double width)
    {
        State.SetPitchFormat(PitchFormat.Custom, width, State.Diagram.CustomHeight ?? 64.0);
        await OnChanged.InvokeAsync();
    }

    private async Task HandleCustomHeightChanged(double height)
    {
        State.SetPitchFormat(PitchFormat.Custom, State.Diagram.CustomWidth ?? 100.0, height);
        await OnChanged.InvokeAsync();
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test tests/FootballPlanner.Component.Tests --filter "DiagramToolbarTests" -v
```

Expected: all tests pass.

- [ ] **Step 5: Commit**

```bash
git add src/FootballPlanner.Web/Components/DiagramToolbar.razor \
        tests/FootballPlanner.Component.Tests/Components/DiagramToolbarTests.cs
git commit -m "feat: add DiagramToolbar component with pitch format selector and aria-label buttons"
```

---

### Task 3: Fix DiagramEditorModal

**Files:**
- Modify: `src/FootballPlanner.Web/Components/DiagramEditorModal.razor`
- Modify: `tests/FootballPlanner.Component.Tests/Components/DiagramEditorModalTests.cs`

- [ ] **Step 1: Update the test constructor to use a trackable HTTP handler**

Replace the current constructor in `DiagramEditorModalTests.cs`. Add the nested handler class and update DI registration so HTTP requests can be intercepted:

```csharp
public class DiagramEditorModalTests : TestContext
{
    private readonly TestHttpMessageHandler _httpHandler = new();

    public DiagramEditorModalTests()
    {
        Services.AddMudServices();
        Services.AddSingleton(new HttpClient(_httpHandler) { BaseAddress = new Uri("http://localhost") });
        Services.AddScoped<ApiClient>();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private IRenderedComponent<MudDialogProvider> SetupDialogProvider()
        => RenderComponent<MudDialogProvider>();

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
    
    // ... existing tests below
}
```

- [ ] **Step 2: Update the three existing tests that use old `data-testid` selectors**

Replace these three test bodies in `DiagramEditorModalTests.cs`:

```csharp
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

    Assert.NotNull(provider.Find("svg"));
}
```

- [ ] **Step 3: Add the seven new tests**

Append to `DiagramEditorModalTests.cs`:

```csharp
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
    provider.Find("svg").Click();

    Assert.NotEmpty(provider.FindAll("circle[data-element]"));
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
    provider.Find("svg").Click();

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
    provider.Find("svg").Click(); // sets ArrowStartPoint
    provider.Find("svg").Click(); // completes arrow

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
    provider.Find("svg").Click();
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
    provider.Find("svg").Click();
    provider.Find("[aria-label='Undo']").Click();
    Assert.Empty(provider.FindAll("polygon[data-element^='cones']"));

    provider.Find("[aria-label='Redo']").Click();

    Assert.NotEmpty(provider.FindAll("polygon[data-element^='cones']"));
}
```

- [ ] **Step 4: Run tests to verify the new tests fail (old ones may fail too — expected)**

```bash
dotnet test tests/FootballPlanner.Component.Tests --filter "DiagramEditorModalTests" -v
```

Expected: multiple failures because `DiagramEditorModal` still uses span wrappers and old selectors.

- [ ] **Step 5: Replace the full content of `DiagramEditorModal.razor`**

Replace with:

```razor
@using FootballPlanner.Web.Models
@using FootballPlanner.Web.Services
@using System.Text.Json
@inject ApiClient Api

<MudDialog>
    <TitleContent>
        <MudText Typo="Typo.h6">Diagram Editor</MudText>
    </TitleContent>
    <DialogContent>
        <div class="d-flex gap-2" style="height:100%;overflow:hidden;">
            <!-- Left toolbar -->
            <DiagramToolbar State="_state" OnChanged="HandleToolbarChanged" />

            <!-- Main canvas -->
            <div style="flex:1;overflow:auto;">
                <DiagramCanvas State="_state"
                               OnPlacePlayer="PlacePlayer"
                               OnPlaceCoach="PlaceCoach"
                               OnPlaceCone="PlaceCone"
                               OnPlaceGoal="PlaceGoal"
                               OnArrowPoint="HandleArrowPoint"
                               OnElementClick="HandleElementClick" />
            </div>

            <!-- Right teams panel -->
            <div style="width:160px;">
                <DiagramTeamsPanel State="_state" />
            </div>
        </div>
    </DialogContent>
    <DialogActions>
        <MudButton aria-label="Undo"
                   Disabled="@(!_state.CanUndo)"
                   StartIcon="@Icons.Material.Filled.Undo"
                   OnClick="Undo">Undo</MudButton>
        <MudButton aria-label="Redo"
                   Disabled="@(!_state.CanRedo)"
                   StartIcon="@Icons.Material.Filled.Redo"
                   OnClick="Redo">Redo</MudButton>
        <MudButton aria-label="Clear"
                   Color="Color.Warning"
                   OnClick="Clear">Clear</MudButton>
        <MudSpacer />
        <MudButton aria-label="Cancel"
                   OnClick="Cancel">Cancel</MudButton>
        <MudButton aria-label="Save Diagram"
                   Color="Color.Primary"
                   Variant="Variant.Filled"
                   OnClick="SaveAsync">Save Diagram</MudButton>
    </DialogActions>
</MudDialog>

@code {
    [CascadingParameter] IMudDialogInstance MudDialog { get; set; } = null!;
    [Parameter] public string? InitialDiagramJson { get; set; }
    [Parameter] public int ActivityId { get; set; }
    private readonly DiagramEditorState _state = new();

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    protected override async Task OnInitializedAsync()
    {
        DiagramModel? initial = null;
        if (!string.IsNullOrWhiteSpace(InitialDiagramJson))
        {
            try { initial = JsonSerializer.Deserialize<DiagramModel>(InitialDiagramJson, _jsonOptions); }
            catch { /* ignore malformed JSON — start fresh */ }
        }
        _state.Initialize(initial);
    }

    private void HandleToolbarChanged() => StateHasChanged();

    private void PlacePlayer((double X, double Y) coords) { _state.PlacePlayer(coords.X, coords.Y); StateHasChanged(); }
    private void PlaceCoach((double X, double Y) coords) { _state.PlaceCoach(coords.X, coords.Y); StateHasChanged(); }
    private void PlaceCone((double X, double Y) coords) { _state.PlaceCone(coords.X, coords.Y); StateHasChanged(); }
    private void PlaceGoal((double X, double Y) coords) { _state.PlaceGoal(coords.X, coords.Y); StateHasChanged(); }
    private void HandleArrowPoint((double X, double Y) coords) { _state.HandleArrowPoint(coords.X, coords.Y); StateHasChanged(); }

    private void HandleElementClick(string elementRef)
    {
        if (_state.ActiveTool == "delete") _state.DeleteElement(elementRef);
        StateHasChanged();
    }

    private void Undo() { _state.Undo(); StateHasChanged(); }
    private void Redo() { _state.Redo(); StateHasChanged(); }
    private void Clear() { _state.Clear(); StateHasChanged(); }
    private void Cancel() => MudDialog.Cancel();

    private async Task SaveAsync()
    {
        var json = JsonSerializer.Serialize(_state.Diagram, _jsonOptions);
        var response = await Api.SaveDiagramAsync(ActivityId, json);
        response.EnsureSuccessStatusCode();
        MudDialog.Close(json);
    }
}
```

- [ ] **Step 6: Run all component tests**

```bash
dotnet test tests/FootballPlanner.Component.Tests -v
```

Expected: all tests pass. If any test fails, the failure message will indicate which assertion failed — fix before moving on.

- [ ] **Step 7: Run all tests**

```bash
dotnet test tests/FootballPlanner.Unit.Tests && dotnet test tests/FootballPlanner.Component.Tests
```

Expected: all unit and component tests pass.

- [ ] **Step 8: Commit**

```bash
git add src/FootballPlanner.Web/Components/DiagramEditorModal.razor \
        tests/FootballPlanner.Component.Tests/Components/DiagramEditorModalTests.cs
git commit -m "fix: replace span wrappers with direct OnClick/aria-label, extract DiagramToolbar, add comprehensive tests"
```
