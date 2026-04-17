# Drag-to-Move Diagram Elements Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Allow coaches to reposition placed elements on the pitch diagram by dragging them in real-time.

**Architecture:** `DiagramEditorState` gains two new methods (`BeginDrag` and `PreviewMove`) that update element position without touching undo stacks. `DiagramCanvas` adds `@onmousedown` handlers per element that call the existing `diagramInterop.startDrag` JS function when the move tool is active; the JS fires `[JSInvokable]` callbacks `OnDragMove` and `OnDragComplete` back into Blazor on each mousemove/mouseup. `DiagramCanvas` implements `IAsyncDisposable` to call `diagramInterop.cleanup` on unmount.

**Tech Stack:** Blazor WebAssembly (.NET 10), bUnit (component tests), existing `diagram-interop.js` module (no changes needed).

---

## File Map

**Modified files:**
- `src/FootballPlanner.Web/Models/DiagramEditorState.cs` — add `BeginDrag()` and `PreviewMove(string, double, double)`
- `src/FootballPlanner.Web/Components/DiagramCanvas.razor` — add drag fields, `@onmousedown` handlers, `[JSInvokable]` callbacks, `IAsyncDisposable`
- `tests/FootballPlanner.Component.Tests/Models/DiagramEditorStateTests.cs` — add two new tests
- `tests/FootballPlanner.Component.Tests/Components/DiagramCanvasTests.cs` — add one new test

---

## Task 1: DiagramEditorState — BeginDrag and PreviewMove

**Files:**
- Modify: `src/FootballPlanner.Web/Models/DiagramEditorState.cs`
- Modify: `tests/FootballPlanner.Component.Tests/Models/DiagramEditorStateTests.cs`

- [ ] **Step 1: Write the failing tests**

Open `tests/FootballPlanner.Component.Tests/Models/DiagramEditorStateTests.cs` and add these two tests at the end of the class (before the closing `}`):

```csharp
[Fact]
public void BeginDrag_PushesUndoSnapshot()
{
    var state = new DiagramEditorState();
    state.PlaceCone(10.0, 20.0);

    state.BeginDrag();

    Assert.True(state.CanUndo);
}

[Fact]
public void PreviewMove_UpdatesPositionWithoutAddingToUndoStack()
{
    var state = new DiagramEditorState();
    state.PlaceCone(10.0, 20.0);
    // Reset undo stack to a known state
    state.Initialize(state.Diagram);
    state.PlaceCone(10.0, 20.0); // one cone at index 0, one undo entry

    state.PreviewMove("cones/0", 50.0, 60.0);

    // Position updated
    Assert.Equal(50.0, state.Diagram.Cones[0].X);
    Assert.Equal(60.0, state.Diagram.Cones[0].Y);
    // Undo stack NOT grown — still only the one entry from PlaceCone
    Assert.True(state.CanUndo);
    state.Undo();
    Assert.False(state.CanUndo); // no further entries
}
```

- [ ] **Step 2: Run the tests to confirm they fail**

```bash
dotnet test tests/FootballPlanner.Component.Tests --filter "BeginDrag|PreviewMove" 2>&1 | tail -10
```

Expected: build error — `BeginDrag` and `PreviewMove` do not exist yet.

- [ ] **Step 3: Add BeginDrag and PreviewMove to DiagramEditorState**

Open `src/FootballPlanner.Web/Models/DiagramEditorState.cs`. Add these two methods directly after the `MoveElement` method (around line 89):

```csharp
public void BeginDrag() => PushUndo();

public void PreviewMove(string elementRef, double x, double y)
    => Diagram = ApplyMove(Diagram, elementRef, x, y);
```

- [ ] **Step 4: Run the tests to confirm they pass**

```bash
dotnet test tests/FootballPlanner.Component.Tests --filter "BeginDrag|PreviewMove" 2>&1 | tail -5
```

Expected: `Passed! - Failed: 0, Passed: 2`

- [ ] **Step 5: Run all tests to confirm nothing is broken**

```bash
dotnet test tests/FootballPlanner.Component.Tests 2>&1 | tail -5
dotnet test tests/FootballPlanner.Unit.Tests 2>&1 | tail -5
```

Expected: all pass.

- [ ] **Step 6: Commit**

```bash
git add src/FootballPlanner.Web/Models/DiagramEditorState.cs \
        tests/FootballPlanner.Component.Tests/Models/DiagramEditorStateTests.cs
git commit -m "feat: add BeginDrag and PreviewMove to DiagramEditorState"
```

---

## Task 2: DiagramCanvas — drag handling

**Files:**
- Modify: `src/FootballPlanner.Web/Components/DiagramCanvas.razor`
- Modify: `tests/FootballPlanner.Component.Tests/Components/DiagramCanvasTests.cs`

- [ ] **Step 1: Write the failing test**

Open `tests/FootballPlanner.Component.Tests/Components/DiagramCanvasTests.cs` and add this test at the end of the class (before the closing `}`):

```csharp
[Fact]
public void OnDragMove_UpdatesElementPosition()
{
    var state = DefaultState();
    state.SetTool("move");
    state.PlaceCone(10.0, 20.0);

    var cut = RenderComponent<DiagramCanvas>(
        p => p.Add(x => x.State, state));

    // Simulate the JS callback firing — as if the user dragged to (50, 60)
    cut.Instance.OnDragMove(50.0, 60.0);
    cut.Render();

    Assert.Equal(50.0, state.Diagram.Cones[0].X);
    Assert.Equal(60.0, state.Diagram.Cones[0].Y);
}
```

Note: `OnDragMove` is not yet public and the `_draggingRef` isn't set, so the test will fail after we implement — we'll wire `_draggingRef` before calling in the test (see Step 4 adjustment).

- [ ] **Step 2: Run the test to confirm it fails**

```bash
dotnet test tests/FootballPlanner.Component.Tests --filter "OnDragMove" 2>&1 | tail -10
```

Expected: build error — `OnDragMove` does not exist yet.

- [ ] **Step 3: Update DiagramCanvas.razor**

Replace the entire contents of `src/FootballPlanner.Web/Components/DiagramCanvas.razor` with the following. The changes are:
1. Add `@implements IAsyncDisposable` directive
2. Add `@using Microsoft.JSInterop` directive
3. Add `@onmousedown` to every element (players, coaches, cones, goals, arrows)
4. Add new fields: `_draggingRef`, `_dotNetRef`
5. Add `[JSInvokable]` methods `OnDragMove` and `OnDragComplete`
6. Add `HandleElementMouseDown` method
7. Add `DisposeAsync`

```razor
@using FootballPlanner.Web.Models
@using Microsoft.JSInterop
@implements IAsyncDisposable
@inject IJSRuntime JS

<svg id="@_svgId"
     viewBox="0 0 100 @_pitchHeight.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)"
     style="width:100%;display:block;background:#2d5a27;cursor:@_cursor;"
     @onclick="HandleSvgClick"
     @onmousemove="HandleMouseMove">

    <defs>
        <marker id="arr-run-@_svgId"    markerWidth="8" markerHeight="6" refX="8" refY="3" orient="auto"><polygon points="0 0,8 3,0 6" fill="white"/></marker>
        <marker id="arr-pass-@_svgId"   markerWidth="8" markerHeight="6" refX="8" refY="3" orient="auto"><polygon points="0 0,8 3,0 6" fill="#90caf9"/></marker>
        <marker id="arr-dribble-@_svgId" markerWidth="8" markerHeight="6" refX="8" refY="3" orient="auto"><polygon points="0 0,8 3,0 6" fill="#ffcc80"/></marker>
    </defs>

    <!-- Pitch lines -->
    <rect x="2" y="@(2 * _pitchHeight / 100)"
          width="96" height="@(96 * _pitchHeight / 100)"
          fill="none" stroke="rgba(255,255,255,0.6)" stroke-width="0.5"/>
    <line x1="50" y1="@(2 * _pitchHeight / 100)" x2="50" y2="@(98 * _pitchHeight / 100)"
          stroke="rgba(255,255,255,0.4)" stroke-width="0.3"/>
    <ellipse cx="50" cy="@(50 * _pitchHeight / 100)" rx="9.15" ry="@(9.15 * _pitchHeight / 100)"
             fill="none" stroke="rgba(255,255,255,0.4)" stroke-width="0.3"/>

    <!-- Goals -->
    @for (var gi = 0; gi < State.Diagram.Goals.Count; gi++)
    {
        var goal = State.Diagram.Goals[gi];
        var gref = $"goals/{gi}";
        <rect data-element="@gref"
              x="@goal.X" y="@goal.Y"
              width="@goal.Width" height="@(goal.Width / 7.0)"
              fill="none" stroke="white" stroke-width="0.8"
              style="cursor:pointer;"
              @onmousedown="e => HandleElementMouseDown(gref, e)"
              @onclick:stopPropagation="true"
              @onclick="() => HandleElementClick(gref)"/>
    }

    <!-- Cones -->
    @for (var ci = 0; ci < State.Diagram.Cones.Count; ci++)
    {
        var cone = State.Diagram.Cones[ci];
        var cref = $"cones/{ci}";
        <polygon data-element="@cref"
                 points="@cone.X,@(cone.Y * _pitchHeight / 100 - 2) @(cone.X - 1.5),@(cone.Y * _pitchHeight / 100 + 1) @(cone.X + 1.5),@(cone.Y * _pitchHeight / 100 + 1)"
                 fill="#f0a500"
                 style="cursor:pointer;"
                 @onmousedown="e => HandleElementMouseDown(cref, e)"
                 @onclick:stopPropagation="true"
                 @onclick="() => HandleElementClick(cref)"/>
    }

    <!-- Coaches -->
    @for (var ki = 0; ki < State.Diagram.Coaches.Count; ki++)
    {
        var coach = State.Diagram.Coaches[ki];
        var kref = $"coaches/{ki}";
        <circle data-element="@kref"
                cx="@coach.X" cy="@(coach.Y * _pitchHeight / 100)"
                r="3" fill="#f0a500" stroke="white" stroke-width="0.5"
                style="cursor:pointer;"
                @onmousedown="e => HandleElementMouseDown(kref, e)"
                @onclick:stopPropagation="true"
                @onclick="() => HandleElementClick(kref)"/>
        @((MarkupString)$"<text x=\"{coach.X.ToString(System.Globalization.CultureInfo.InvariantCulture)}\" y=\"{(coach.Y * _pitchHeight / 100 + 0.8).ToString(System.Globalization.CultureInfo.InvariantCulture)}\" text-anchor=\"middle\" dominant-baseline=\"middle\" fill=\"white\" font-size=\"2.5\" font-weight=\"bold\" pointer-events=\"none\">{System.Net.WebUtility.HtmlEncode(coach.Label)}</text>")
    }

    <!-- Players -->
    @for (var ti = 0; ti < State.Diagram.Teams.Count; ti++)
    {
        var team = State.Diagram.Teams[ti];
        for (var pi = 0; pi < team.Players.Count; pi++)
        {
            var player = team.Players[pi];
            var pref = $"teams/{ti}/players/{pi}";
            <circle data-element="@pref"
                    cx="@player.X" cy="@(player.Y * _pitchHeight / 100)"
                    r="3" fill="@team.Color" stroke="white" stroke-width="0.5"
                    style="cursor:pointer;"
                    @onmousedown="e => HandleElementMouseDown(pref, e)"
                    @onclick:stopPropagation="true"
                    @onclick="() => HandleElementClick(pref)"/>
            @if (!string.IsNullOrEmpty(player.Label))
            {
                @((MarkupString)$"<text x=\"{player.X.ToString(System.Globalization.CultureInfo.InvariantCulture)}\" y=\"{(player.Y * _pitchHeight / 100 + 0.8).ToString(System.Globalization.CultureInfo.InvariantCulture)}\" text-anchor=\"middle\" dominant-baseline=\"middle\" fill=\"white\" font-size=\"2.5\" font-weight=\"bold\" pointer-events=\"none\">{System.Net.WebUtility.HtmlEncode(player.Label)}</text>")
            }
        }
    }

    <!-- Arrows -->
    @for (var ai = 0; ai < State.Diagram.Arrows.Count; ai++)
    {
        var arrow = State.Diagram.Arrows[ai];
        var aref = $"arrows/{ai}";
        var (stroke, dasharray, markerId) = arrow.Style switch
        {
            ArrowStyle.Pass    => ("#90caf9", "4,3",  $"arr-pass-{_svgId}"),
            ArrowStyle.Dribble => ("#ffcc80", "none", $"arr-dribble-{_svgId}"),
            _                  => ("white",   "none", $"arr-run-{_svgId}")
        };
        <path data-element="@aref"
              d="@BuildArrowPath(arrow)"
              stroke="@stroke"
              stroke-width="@(arrow.Style == ArrowStyle.Pass ? "1.5" : "2")"
              stroke-dasharray="@dasharray"
              fill="none"
              marker-end="url(#@markerId)"
              style="cursor:pointer;"
              @onmousedown="e => HandleElementMouseDown(aref, e)"
              @onclick:stopPropagation="true"
              @onclick="() => HandleElementClick(aref)"/>
    }

    <!-- Ghost cursor for active placement tools -->
    @if (_ghostX >= 0 && IsPlacementTool)
    {
        <circle cx="@_ghostX" cy="@_ghostY"
                r="3" fill="rgba(255,255,255,0.25)" stroke="rgba(255,255,255,0.5)"
                stroke-dasharray="1,1" stroke-width="0.5"
                pointer-events="none"/>
    }
</svg>

@code {
    [Parameter, EditorRequired] public DiagramEditorState State { get; set; } = null!;
    [Parameter] public EventCallback<(double X, double Y)> OnPlacePlayer { get; set; }
    [Parameter] public EventCallback<(double X, double Y)> OnPlaceCoach { get; set; }
    [Parameter] public EventCallback<(double X, double Y)> OnPlaceCone { get; set; }
    [Parameter] public EventCallback<(double X, double Y)> OnPlaceGoal { get; set; }
    [Parameter] public EventCallback<(double X, double Y)> OnArrowPoint { get; set; }
    [Parameter] public EventCallback<string> OnElementClick { get; set; }

    private readonly string _svgId = $"pitch-{Guid.NewGuid():N}";
    private double _ghostX = -1;
    private double _ghostY = -1;
    private string? _draggingRef;
    private DotNetObjectReference<DiagramCanvas>? _dotNetRef;

    private double _pitchHeight => State.Diagram.PitchFormat switch
    {
        PitchFormat.ElevenVElevenFull  => 64.0,
        PitchFormat.ElevenVElevenHalf  => 128.0,
        PitchFormat.NineVNineFull      => 62.5,
        PitchFormat.NineVNineHalf      => 125.0,
        PitchFormat.SevenVSevenFull    => 66.7,
        PitchFormat.SevenVSevenHalf    => 133.3,
        PitchFormat.Custom when State.Diagram is { CustomHeight: { } h, CustomWidth: { } w }
            => h / w * 100.0,
        _ => 64.0
    };

    private bool IsPlacementTool => State.ActiveTool is
        "player" or "coach" or "cone" or "goal" or
        "arrow-run" or "arrow-pass" or "arrow-dribble";

    private string _cursor => State.ActiveTool switch
    {
        "move"   => "move",
        "delete" => "crosshair",
        _ when IsPlacementTool => "crosshair",
        _ => "default"
    };

    private async Task HandleElementMouseDown(string elementRef, MouseEventArgs e)
    {
        if (State.ActiveTool != "move") return;
        _draggingRef = elementRef;
        State.BeginDrag();
        _dotNetRef ??= DotNetObjectReference.Create(this);
        try
        {
            await JS.InvokeVoidAsync("diagramInterop.startDrag", _dotNetRef, _svgId);
        }
        catch (JSException) { /* test environment — drag won't fire JS callbacks */ }
    }

    [JSInvokable]
    public void OnDragMove(double x, double y)
    {
        if (_draggingRef == null) return;
        State.PreviewMove(_draggingRef, x, y);
        StateHasChanged();
    }

    [JSInvokable]
    public void OnDragComplete(double x, double y)
    {
        if (_draggingRef == null) return;
        State.PreviewMove(_draggingRef, x, y);
        _draggingRef = null;
        StateHasChanged();
    }

    private async Task HandleSvgClick(MouseEventArgs e)
    {
        double x = e.OffsetX;
        double y = e.OffsetY;
        try
        {
            var coords = await JS.InvokeAsync<SvgCoords>(
                "diagramInterop.getSvgCoordinates", _svgId, e.ClientX, e.ClientY);
            if (coords is not null)
            {
                x = coords.X;
                y = coords.Y;
            }
        }
        catch (JSException)
        {
            // JS interop not available (test environment) — fall back to offset coordinates
        }

        switch (State.ActiveTool)
        {
            case "player":         await OnPlacePlayer.InvokeAsync((x, y)); break;
            case "coach":          await OnPlaceCoach.InvokeAsync((x, y));  break;
            case "cone":           await OnPlaceCone.InvokeAsync((x, y));   break;
            case "goal":           await OnPlaceGoal.InvokeAsync((x, y));   break;
            case "arrow-run":
            case "arrow-pass":
            case "arrow-dribble":  await OnArrowPoint.InvokeAsync((x, y));  break;
        }
    }

    private Task HandleElementClick(string elementRef)
        => OnElementClick.InvokeAsync(elementRef);

    private async Task HandleMouseMove(MouseEventArgs e)
    {
        if (!IsPlacementTool) return;
        try
        {
            var coords = await JS.InvokeAsync<SvgCoords>(
                "diagramInterop.getSvgCoordinates", _svgId, e.ClientX, e.ClientY);
            if (coords is not null)
            {
                _ghostX = coords.X;
                _ghostY = coords.Y * _pitchHeight / 100.0;
            }
            else
            {
                _ghostX = e.OffsetX;
                _ghostY = e.OffsetY;
            }
        }
        catch (JSException)
        {
            // Test environment — ghost position will be approximate
            _ghostX = e.OffsetX;
            _ghostY = e.OffsetY;
        }
    }

    private string BuildArrowPath(ArrowElement arrow)
    {
        var x1 = arrow.X1;
        var y1 = arrow.Y1 * _pitchHeight / 100;
        var x2 = arrow.X2;
        var y2 = arrow.Y2 * _pitchHeight / 100;
        var cx = arrow.Cx;
        var cy = arrow.Cy * _pitchHeight / 100;

        if (arrow.Style == ArrowStyle.Dribble)
        {
            const int waves = 4;
            var sb = new System.Text.StringBuilder();
            sb.Append($"M {x1.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)} {y1.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)}");
            for (var i = 0; i < waves; i++)
            {
                var t1 = (i + 0.5) / waves;
                var t2 = (i + 1.0) / waves;
                var mx1 = x1 + (x2 - x1) * t1;
                var my1 = y1 + (y2 - y1) * t1;
                var mx2 = x1 + (x2 - x1) * t2;
                var my2 = y1 + (y2 - y1) * t2;
                var dx = x2 - x1;
                var dy = y2 - y1;
                var len = Math.Sqrt(dx * dx + dy * dy);
                var amp = (i % 2 == 0 ? 1.0 : -1.0) * 3.0;
                var cpx = (mx1 + mx2) / 2 + (len > 0 ? -dy / len * amp : 0);
                var cpy = (my1 + my2) / 2 + (len > 0 ? dx / len * amp : 0);
                sb.Append($" Q {cpx.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)} {cpy.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)} {mx2.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)} {my2.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)}");
            }
            return sb.ToString();
        }

        return $"M {x1.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)} {y1.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)} Q {cx.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)} {cy.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)} {x2.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)} {y2.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)}";
    }

    public async ValueTask DisposeAsync()
    {
        try { await JS.InvokeVoidAsync("diagramInterop.cleanup", _svgId); }
        catch (JSException) { /* ignore — component may be disposing during test teardown */ }
        _dotNetRef?.Dispose();
    }

    private record SvgCoords(double X, double Y);
}
```

- [ ] **Step 4: Update the test to properly set _draggingRef**

The `_draggingRef` field is private, so the test can't set it directly. Instead, simulate the mousedown on the element first (which sets `_draggingRef`), then call `OnDragMove`. Update the test in `DiagramCanvasTests.cs`:

```csharp
[Fact]
public void OnDragMove_UpdatesElementPosition()
{
    var state = DefaultState();
    state.SetTool("move");
    state.PlaceCone(10.0, 20.0);

    var cut = RenderComponent<DiagramCanvas>(
        p => p.Add(x => x.State, state));

    // Mousedown on the cone element starts the drag (sets _draggingRef)
    cut.Find("polygon[data-element^='cones']").MouseDown();

    // Simulate JS callback
    cut.Instance.OnDragMove(50.0, 60.0);
    cut.Render();

    Assert.Equal(50.0, state.Diagram.Cones[0].X);
    Assert.Equal(60.0, state.Diagram.Cones[0].Y);
}
```

- [ ] **Step 5: Run the new test to confirm it passes**

```bash
dotnet test tests/FootballPlanner.Component.Tests --filter "OnDragMove" 2>&1 | tail -5
```

Expected: `Passed! - Failed: 0, Passed: 1`

- [ ] **Step 6: Run all tests to confirm nothing is broken**

```bash
dotnet test tests/FootballPlanner.Component.Tests 2>&1 | tail -5
dotnet test tests/FootballPlanner.Unit.Tests 2>&1 | tail -5
```

Expected: all pass.

- [ ] **Step 7: Commit**

```bash
git add src/FootballPlanner.Web/Components/DiagramCanvas.razor \
        tests/FootballPlanner.Component.Tests/Components/DiagramCanvasTests.cs
git commit -m "feat: wire drag-to-move into DiagramCanvas"
```
