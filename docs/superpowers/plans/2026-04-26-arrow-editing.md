# Arrow Editing Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix the blunt arrowhead bug, add drag handles to reshape arrows (tail, tip, curve), an arrow properties panel (style, colour, sequence number), and an auto-generated movable legend derived from arrow data.

**Architecture:** All diagram state lives in `DiagramEditorState` (pure C# class, no DI). The SVG canvas renders handles when an arrow is selected; the same JS drag infrastructure used for moving elements handles the new sub-element refs (`arrows/i/tail`, `arrows/i/tip`, `arrows/i/curve`, `legend`). The legend is derived at render time from arrow data — no stored list.

**Tech Stack:** Blazor WASM, MudBlazor, xUnit, Playwright

---

## File Map

| File | Action | Purpose |
|---|---|---|
| `src/FootballPlanner.Web/Models/DiagramModel.cs` | Modify | Add `Color`, `SequenceNumber` to `ArrowElement`; add `DiagramLegend` record; add `Legend` to `DiagramModel` |
| `src/FootballPlanner.Web/Models/DiagramEditorState.cs` | Modify | New arrow + legend state methods; `MoveByDelta` handle sub-refs; legend `ApplyDelta` |
| `src/FootballPlanner.Web/Components/DiagramCanvas.razor` | Modify | Per-arrow dynamic markers, `refX` fix, handle rendering, dribble curve, legend SVG |
| `src/FootballPlanner.Web/Components/DiagramElementPanel.razor` | Modify | Arrow properties (style, colour, sequence number); legend remove button |
| `src/FootballPlanner.Web/Components/DiagramToolbar.razor` | Modify | Legend toggle button |
| `tests/FootballPlanner.Web.Tests/FootballPlanner.Web.Tests.csproj` | Create | xUnit project that compiles model files directly (avoids WASM project reference issues) |
| `tests/FootballPlanner.Web.Tests/DiagramEditorStateTests.cs` | Create | State unit tests |
| `tests/FootballPlanner.Feature.Tests/Journeys/DiagramJourney.cs` | Modify | Helper methods for new interactions |
| `tests/FootballPlanner.Feature.Tests/Tests/DiagramArrowEditingTests.cs` | Create | Playwright end-to-end tests |
| `FootballPlanner.slnx` | Modify | Add new test project |

---

## Task 1: Data Model Changes

**Files:**
- Modify: `src/FootballPlanner.Web/Models/DiagramModel.cs`

- [ ] **Step 1: Add `Color` and `SequenceNumber` to `ArrowElement`, add `DiagramLegend`, add `Legend` to `DiagramModel`**

Replace the entire file content:

```csharp
using System.Text.Json.Serialization;

namespace FootballPlanner.Web.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PitchFormat
{
    ElevenVElevenFull, ElevenVElevenHalf,
    NineVNineFull,     NineVNineHalf,
    SevenVSevenFull,   SevenVSevenHalf,
    Custom
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ArrowStyle { Run, Pass, Dribble }

public record DiagramModel(
    PitchFormat PitchFormat,
    double? CustomWidth,
    double? CustomHeight,
    List<DiagramTeam> Teams,
    List<CoachElement> Coaches,
    List<ConeElement> Cones,
    List<GoalElement> Goals,
    List<ArrowElement> Arrows,
    string? Notes = null,
    DiagramLegend? Legend = null);

public record DiagramTeam(
    string Id,
    string Name,
    string Color,
    List<PlayerElement> Players);

public record PlayerElement(string Label, double X, double Y, double Radius = 2.0);
public record CoachElement(string Label, double X, double Y, double Radius = 2.0);
public record ConeElement(double X, double Y, double Size = 1.0, string Color = "#f0a500");
public record GoalElement(double X, double Y, double Width);

public record ArrowElement(
    ArrowStyle Style,
    double X1, double Y1,
    double X2, double Y2,
    double Cx, double Cy,
    string? Color = null,
    int? SequenceNumber = null);

// Stores only position — collapsed/expanded is transient UI state, never persisted.
public record DiagramLegend(double X = 5, double Y = 5);
```

- [ ] **Step 2: Build to confirm no compilation errors**

```bash
dotnet build FootballPlanner.slnx
```
Expected: Build succeeded, 0 errors.

- [ ] **Step 3: Commit**

```bash
git add src/FootballPlanner.Web/Models/DiagramModel.cs
git commit -m "feat: add Color, SequenceNumber to ArrowElement and DiagramLegend model"
```

---

## Task 2: Create Web.Tests Project

**Files:**
- Create: `tests/FootballPlanner.Web.Tests/FootballPlanner.Web.Tests.csproj`
- Modify: `FootballPlanner.slnx`

The `FootballPlanner.Web` project uses the `Microsoft.NET.Sdk.BlazorWebAssembly` SDK which cannot be directly referenced from a standard test project. Instead, compile the model source files directly from the test project.

- [ ] **Step 1: Create the test project file**

Create `tests/FootballPlanner.Web.Tests/FootballPlanner.Web.Tests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <!-- Compile model files directly — avoids WASM SDK project reference issues -->
    <Compile Include="../../src/FootballPlanner.Web/Models/DiagramModel.cs" />
    <Compile Include="../../src/FootballPlanner.Web/Models/DiagramEditorState.cs" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="coverlet.collector" Version="6.0.4" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.14.1" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.4" />
  </ItemGroup>

  <ItemGroup>
    <Using Include="Xunit" />
  </ItemGroup>

</Project>
```

- [ ] **Step 2: Add the project to the solution**

```bash
dotnet sln FootballPlanner.slnx add tests/FootballPlanner.Web.Tests/FootballPlanner.Web.Tests.csproj
```

Expected: `Project 'FootballPlanner.Web.Tests' added to the solution.`

- [ ] **Step 3: Build to confirm project compiles**

```bash
dotnet build tests/FootballPlanner.Web.Tests
```
Expected: Build succeeded, 0 errors.

- [ ] **Step 4: Commit**

```bash
git add tests/FootballPlanner.Web.Tests/FootballPlanner.Web.Tests.csproj FootballPlanner.slnx
git commit -m "test: add FootballPlanner.Web.Tests project for diagram model tests"
```

---

## Task 3: Arrow State Methods (TDD)

**Files:**
- Create: `tests/FootballPlanner.Web.Tests/DiagramEditorStateTests.cs`
- Modify: `src/FootballPlanner.Web/Models/DiagramEditorState.cs`

- [ ] **Step 1: Write failing tests for `ChangeArrowStyle`, `ChangeArrowColor`, `ChangeArrowSequenceNumber`**

Create `tests/FootballPlanner.Web.Tests/DiagramEditorStateTests.cs`:

```csharp
using FootballPlanner.Web.Models;

namespace FootballPlanner.Web.Tests;

public class DiagramEditorStateTests
{
    // HandleArrowPoint does not require an active tool — style defaults to Run.
    private static DiagramEditorState StateWithOneArrow()
    {
        var state = new DiagramEditorState();
        state.Initialize(null);
        state.HandleArrowPoint(10, 20);
        state.HandleArrowPoint(80, 60);
        return state;
    }

    // ── ChangeArrowStyle ──────────────────────────────────────────────────────

    [Fact]
    public void ChangeArrowStyle_UpdatesStyle()
    {
        var state = StateWithOneArrow();
        state.ChangeArrowStyle("arrows/0", ArrowStyle.Pass);
        Assert.Equal(ArrowStyle.Pass, state.Diagram.Arrows[0].Style);
    }

    [Fact]
    public void ChangeArrowStyle_PushesUndo()
    {
        var state = StateWithOneArrow();
        state.ChangeArrowStyle("arrows/0", ArrowStyle.Dribble);
        state.Undo();
        Assert.Equal(ArrowStyle.Run, state.Diagram.Arrows[0].Style);
    }

    [Fact]
    public void ChangeArrowStyle_InvalidRef_DoesNothing()
    {
        var state = StateWithOneArrow();
        state.ChangeArrowStyle("cones/0", ArrowStyle.Pass);
        Assert.Equal(ArrowStyle.Run, state.Diagram.Arrows[0].Style);
    }

    // ── ChangeArrowColor ──────────────────────────────────────────────────────

    [Fact]
    public void ChangeArrowColor_UpdatesColor()
    {
        var state = StateWithOneArrow();
        state.ChangeArrowColor("arrows/0", "#ff0000");
        Assert.Equal("#ff0000", state.Diagram.Arrows[0].Color);
    }

    [Fact]
    public void ChangeArrowColor_Null_ClearsColor()
    {
        var state = StateWithOneArrow();
        state.ChangeArrowColor("arrows/0", "#ff0000");
        state.ChangeArrowColor("arrows/0", null);
        Assert.Null(state.Diagram.Arrows[0].Color);
    }

    [Fact]
    public void ChangeArrowColor_PushesUndo()
    {
        var state = StateWithOneArrow();
        state.ChangeArrowColor("arrows/0", "#ff0000");
        state.Undo();
        Assert.Null(state.Diagram.Arrows[0].Color);
    }

    // ── ChangeArrowSequenceNumber ─────────────────────────────────────────────

    [Fact]
    public void ChangeArrowSequenceNumber_UpdatesNumber()
    {
        var state = StateWithOneArrow();
        state.ChangeArrowSequenceNumber("arrows/0", 2);
        Assert.Equal(2, state.Diagram.Arrows[0].SequenceNumber);
    }

    [Fact]
    public void ChangeArrowSequenceNumber_Null_ClearsNumber()
    {
        var state = StateWithOneArrow();
        state.ChangeArrowSequenceNumber("arrows/0", 3);
        state.ChangeArrowSequenceNumber("arrows/0", null);
        Assert.Null(state.Diagram.Arrows[0].SequenceNumber);
    }

    [Fact]
    public void ChangeArrowSequenceNumber_PushesUndo()
    {
        var state = StateWithOneArrow();
        state.ChangeArrowSequenceNumber("arrows/0", 1);
        state.Undo();
        Assert.Null(state.Diagram.Arrows[0].SequenceNumber);
    }
}
```

- [ ] **Step 2: Run tests to confirm they fail**

```bash
dotnet test tests/FootballPlanner.Web.Tests
```
Expected: Multiple failures — `ChangeArrowStyle`, `ChangeArrowColor`, `ChangeArrowSequenceNumber` not found.

- [ ] **Step 3: Implement `ChangeArrowStyle`, `ChangeArrowColor`, `ChangeArrowSequenceNumber` in `DiagramEditorState.cs`**

Add these three methods after `ChangeConeColor` in `DiagramEditorState.cs`:

```csharp
public void ChangeArrowStyle(string elementRef, ArrowStyle style)
{
    var parts = elementRef.Split('/');
    if (parts.Length < 2 || parts[0] != "arrows" || !int.TryParse(parts[1], out var idx)) return;
    PushUndo();
    Diagram = Diagram with { Arrows = ReplaceAt(Diagram.Arrows, idx, a => a with { Style = style }) };
}

public void ChangeArrowColor(string elementRef, string? color)
{
    var parts = elementRef.Split('/');
    if (parts.Length < 2 || parts[0] != "arrows" || !int.TryParse(parts[1], out var idx)) return;
    PushUndo();
    Diagram = Diagram with { Arrows = ReplaceAt(Diagram.Arrows, idx, a => a with { Color = color }) };
}

public void ChangeArrowSequenceNumber(string elementRef, int? number)
{
    var parts = elementRef.Split('/');
    if (parts.Length < 2 || parts[0] != "arrows" || !int.TryParse(parts[1], out var idx)) return;
    PushUndo();
    Diagram = Diagram with { Arrows = ReplaceAt(Diagram.Arrows, idx, a => a with { SequenceNumber = number }) };
}
```

- [ ] **Step 4: Run tests to confirm they pass**

```bash
dotnet test tests/FootballPlanner.Web.Tests
```
Expected: All tests pass.

- [ ] **Step 5: Commit**

```bash
git add src/FootballPlanner.Web/Models/DiagramEditorState.cs \
        tests/FootballPlanner.Web.Tests/DiagramEditorStateTests.cs
git commit -m "feat: add ChangeArrowStyle, ChangeArrowColor, ChangeArrowSequenceNumber to DiagramEditorState"
```

---

## Task 4: Arrow Handle State (TDD)

**Files:**
- Modify: `tests/FootballPlanner.Web.Tests/DiagramEditorStateTests.cs`
- Modify: `src/FootballPlanner.Web/Models/DiagramEditorState.cs`

- [ ] **Step 1: Add failing tests for handle `MoveByDelta`**

Append to `DiagramEditorStateTests.cs`:

```csharp
// ── MoveByDelta arrow handles ─────────────────────────────────────────────

[Fact]
public void MoveByDelta_ArrowTailHandle_MovesStartPointAndControlPoint()
{
    var state = StateWithOneArrow();
    var before = state.Diagram.Arrows[0];
    state.MoveByDelta("arrows/0/tail", 5.0, 3.0);
    var after = state.Diagram.Arrows[0];

    Assert.Equal(before.X1 + 5.0, after.X1, precision: 6);
    Assert.Equal(before.Y1 + 3.0, after.Y1, precision: 6);
    // Control point translates by same delta
    Assert.Equal(before.Cx + 5.0, after.Cx, precision: 6);
    Assert.Equal(before.Cy + 3.0, after.Cy, precision: 6);
    // End point unchanged
    Assert.Equal(before.X2, after.X2, precision: 6);
    Assert.Equal(before.Y2, after.Y2, precision: 6);
}

[Fact]
public void MoveByDelta_ArrowTipHandle_MovesEndPointAndControlPoint()
{
    var state = StateWithOneArrow();
    var before = state.Diagram.Arrows[0];
    state.MoveByDelta("arrows/0/tip", 4.0, -2.0);
    var after = state.Diagram.Arrows[0];

    Assert.Equal(before.X2 + 4.0, after.X2, precision: 6);
    Assert.Equal(before.Y2 - 2.0, after.Y2, precision: 6);
    // Control point translates by same delta
    Assert.Equal(before.Cx + 4.0, after.Cx, precision: 6);
    Assert.Equal(before.Cy - 2.0, after.Cy, precision: 6);
    // Start point unchanged
    Assert.Equal(before.X1, after.X1, precision: 6);
    Assert.Equal(before.Y1, after.Y1, precision: 6);
}

[Fact]
public void MoveByDelta_ArrowCurveHandle_MovesControlPointOnly()
{
    var state = StateWithOneArrow();
    var before = state.Diagram.Arrows[0];
    state.MoveByDelta("arrows/0/curve", 2.0, 8.0);
    var after = state.Diagram.Arrows[0];

    Assert.Equal(before.Cx + 2.0, after.Cx, precision: 6);
    Assert.Equal(before.Cy + 8.0, after.Cy, precision: 6);
    // Endpoints unchanged
    Assert.Equal(before.X1, after.X1, precision: 6);
    Assert.Equal(before.Y1, after.Y1, precision: 6);
    Assert.Equal(before.X2, after.X2, precision: 6);
    Assert.Equal(before.Y2, after.Y2, precision: 6);
}
```

- [ ] **Step 2: Run tests to confirm they fail**

```bash
dotnet test tests/FootballPlanner.Web.Tests --filter "MoveByDelta_Arrow"
```
Expected: 3 failures.

- [ ] **Step 3: Update `ApplyDelta` in `DiagramEditorState.cs` to handle arrow handle refs**

The current `ApplyDelta` switch starts with `parts[0]`. Replace the `"arrows"` case with two cases — handle sub-refs first:

```csharp
private static DiagramModel ApplyDelta(DiagramModel diagram, string elementRef, double dx, double dy)
{
    // Legend is the only element ref without an index.
    if (elementRef == "legend" && diagram.Legend != null)
        return diagram with { Legend = diagram.Legend with {
            X = ClampPos(diagram.Legend.X + dx),
            Y = ClampPos(diagram.Legend.Y + dy)
        }};

    var parts = elementRef.Split('/');
    if (parts.Length < 2 || !int.TryParse(parts[1], out var idx)) return diagram;

    return parts[0] switch
    {
        "teams" when parts.Length >= 4 && int.TryParse(parts[3], out var pIdx)
            => ApplyDeltaPlayer(diagram, idx, pIdx, dx, dy),
        "coaches" => diagram with { Coaches = ReplaceAt(diagram.Coaches, idx,
            c => c with { X = ClampPos(c.X + dx), Y = ClampPos(c.Y + dy) }) },
        "cones"   => diagram with { Cones   = ReplaceAt(diagram.Cones,   idx,
            c => c with { X = ClampPos(c.X + dx), Y = ClampPos(c.Y + dy) }) },
        "goals"   => diagram with { Goals   = ReplaceAt(diagram.Goals,   idx,
            g => g with { X = ClampPos(g.X + dx), Y = ClampPos(g.Y + dy) }) },
        // Arrow sub-element handles — only translate the relevant points.
        "arrows" when parts.Length == 3 => parts[2] switch
        {
            "tail"  => diagram with { Arrows = ReplaceAt(diagram.Arrows, idx, a => a with {
                X1 = ClampPos(a.X1 + dx), Y1 = ClampPos(a.Y1 + dy),
                Cx = ClampPos(a.Cx + dx), Cy = ClampPos(a.Cy + dy)
            })},
            "tip"   => diagram with { Arrows = ReplaceAt(diagram.Arrows, idx, a => a with {
                X2 = ClampPos(a.X2 + dx), Y2 = ClampPos(a.Y2 + dy),
                Cx = ClampPos(a.Cx + dx), Cy = ClampPos(a.Cy + dy)
            })},
            "curve" => diagram with { Arrows = ReplaceAt(diagram.Arrows, idx, a => a with {
                Cx = ClampPos(a.Cx + dx), Cy = ClampPos(a.Cy + dy)
            })},
            _ => diagram
        },
        // Move entire arrow — translate all points by the same delta.
        "arrows"  => diagram with { Arrows  = ReplaceAt(diagram.Arrows,  idx,
            a => a with {
                X1 = ClampPos(a.X1 + dx), Y1 = ClampPos(a.Y1 + dy),
                X2 = ClampPos(a.X2 + dx), Y2 = ClampPos(a.Y2 + dy),
                Cx = ClampPos(a.Cx + dx), Cy = ClampPos(a.Cy + dy)
            }) },
        _ => diagram
    };
}
```

- [ ] **Step 4: Run tests to confirm they pass**

```bash
dotnet test tests/FootballPlanner.Web.Tests
```
Expected: All tests pass.

- [ ] **Step 5: Commit**

```bash
git add src/FootballPlanner.Web/Models/DiagramEditorState.cs \
        tests/FootballPlanner.Web.Tests/DiagramEditorStateTests.cs
git commit -m "feat: add arrow handle MoveByDelta support (tail, tip, curve)"
```

---

## Task 5: Legend State Methods (TDD)

**Files:**
- Modify: `tests/FootballPlanner.Web.Tests/DiagramEditorStateTests.cs`
- Modify: `src/FootballPlanner.Web/Models/DiagramEditorState.cs`

- [ ] **Step 1: Add failing tests for `AddLegend` and `RemoveLegend`**

Append to `DiagramEditorStateTests.cs`:

```csharp
// ── Legend ────────────────────────────────────────────────────────────────

[Fact]
public void AddLegend_CreatesLegendAtDefaultPosition()
{
    var state = new DiagramEditorState();
    state.Initialize(null);
    state.AddLegend();
    Assert.NotNull(state.Diagram.Legend);
    Assert.Equal(5.0, state.Diagram.Legend!.X, precision: 6);
    Assert.Equal(5.0, state.Diagram.Legend!.Y, precision: 6);
}

[Fact]
public void AddLegend_PushesUndo()
{
    var state = new DiagramEditorState();
    state.Initialize(null);
    state.AddLegend();
    state.Undo();
    Assert.Null(state.Diagram.Legend);
}

[Fact]
public void AddLegend_ClearsSelectedElement()
{
    var state = StateWithOneArrow();
    state.SelectElement("arrows/0");
    state.AddLegend();
    Assert.Null(state.SelectedElement);
}

[Fact]
public void RemoveLegend_SetsLegendToNull()
{
    var state = new DiagramEditorState();
    state.Initialize(null);
    state.AddLegend();
    state.RemoveLegend();
    Assert.Null(state.Diagram.Legend);
}

[Fact]
public void RemoveLegend_PushesUndo()
{
    var state = new DiagramEditorState();
    state.Initialize(null);
    state.AddLegend();
    state.RemoveLegend();
    state.Undo();
    Assert.NotNull(state.Diagram.Legend);
}

[Fact]
public void RemoveLegend_WhenLegendSelected_ClearsSelectedElement()
{
    var state = new DiagramEditorState();
    state.Initialize(null);
    state.AddLegend();
    state.SelectElement("legend");
    state.RemoveLegend();
    Assert.Null(state.SelectedElement);
}
```

- [ ] **Step 2: Run tests to confirm they fail**

```bash
dotnet test tests/FootballPlanner.Web.Tests --filter "Legend"
```
Expected: 6 failures — `AddLegend` and `RemoveLegend` not found.

- [ ] **Step 3: Implement `AddLegend` and `RemoveLegend` in `DiagramEditorState.cs`**

Add after `DeleteTeam`:

```csharp
public void AddLegend()
{
    PushUndo();
    Diagram = Diagram with { Legend = new DiagramLegend() };
    SelectedElement = null;
}

public void RemoveLegend()
{
    PushUndo();
    Diagram = Diagram with { Legend = null };
    if (SelectedElement == "legend") SelectedElement = null;
}
```

- [ ] **Step 4: Run all tests to confirm they pass**

```bash
dotnet test tests/FootballPlanner.Web.Tests
```
Expected: All tests pass.

- [ ] **Step 5: Commit**

```bash
git add src/FootballPlanner.Web/Models/DiagramEditorState.cs \
        tests/FootballPlanner.Web.Tests/DiagramEditorStateTests.cs
git commit -m "feat: add AddLegend and RemoveLegend to DiagramEditorState"
```

---

## Task 6: Fix Arrowhead Bug and Dynamic Markers

**Files:**
- Modify: `src/FootballPlanner.Web/Components/DiagramCanvas.razor`

**Background:** The current `<defs>` block has three shared markers using `refX="4"` (path endpoint at tip). This causes the stroke butt cap to overlap and obscure the arrowhead tip. Fix: use `refX="0"` (path endpoint at base) so the tip extends cleanly beyond the stroke. Because each arrow may now have a custom colour, shared markers cannot be used — generate one per arrow.

- [ ] **Step 1: Replace `<defs>` block with a per-arrow dynamic marker loop**

In `DiagramCanvas.razor`, replace the entire `<defs>` block (lines 14–24):

```razor
<defs>
    @for (var ai = 0; ai < State.Diagram.Arrows.Count; ai++)
    {
        var a = State.Diagram.Arrows[ai];
        var mFill = a.Color ?? ResolvedArrowColor(a.Style);
        <marker id="arr-@(_svgId)-@ai"
                markerWidth="4" markerHeight="3"
                refX="0" refY="1.5"
                orient="auto">
            <polygon points="0 0,4 1.5,0 3" fill="@mFill"/>
        </marker>
    }
</defs>
```

- [ ] **Step 2: Add the `ResolvedArrowColor` helper to the `@code` block**

Add after `_cursor` property:

```csharp
private static string ResolvedArrowColor(ArrowStyle style) => style switch
{
    ArrowStyle.Pass    => "#90caf9",
    ArrowStyle.Dribble => "#ffcc80",
    _                  => "white"
};
```

- [ ] **Step 3: Update the arrow rendering loop to use per-arrow marker id and resolved colour**

Replace the existing arrows loop (the `<!-- Arrows -->` section):

```razor
<!-- Arrows -->
@for (var ai = 0; ai < State.Diagram.Arrows.Count; ai++)
{
    var arrow = State.Diagram.Arrows[ai];
    var aref = $"arrows/{ai}";
    var stroke = arrow.Color ?? ResolvedArrowColor(arrow.Style);
    var dasharray = arrow.Style == ArrowStyle.Pass ? "2,2" : "none";
    <path data-element="@aref"
          d="@BuildArrowPath(arrow)"
          stroke="@stroke"
          stroke-width="@(arrow.Style == ArrowStyle.Pass ? "0.4" : "0.5")"
          stroke-dasharray="@dasharray"
          fill="none"
          marker-end="url(#arr-@(_svgId)-@ai)"
          style="cursor:pointer;"
          @onmousedown="(e) => HandleElementMouseDown(e, aref)"
          @onmousedown:stopPropagation="true"
          @onclick:stopPropagation="true"
          @onclick="() => HandleElementClick(aref)"/>
}
```

- [ ] **Step 4: Build to confirm no compilation errors**

```bash
dotnet build src/FootballPlanner.Web
```
Expected: Build succeeded, 0 errors.

- [ ] **Step 5: Commit**

```bash
git add src/FootballPlanner.Web/Components/DiagramCanvas.razor
git commit -m "fix: per-arrow dynamic markers with refX=0 — eliminates blunt arrowhead"
```

---

## Task 7: Arrow Handle Rendering

**Files:**
- Modify: `src/FootballPlanner.Web/Components/DiagramCanvas.razor`

When an arrow is the selected element, render three drag handles (tail, tip, curve) on top of the arrow path.

- [ ] **Step 1: Add handle rendering after each arrow path in the arrows loop**

Inside the arrows loop, directly after the closing `/>` of the `<path>`, add:

```razor
@if (State.SelectedElement == aref)
{
    // Pre-capture handle refs — Blazor Razor cannot use string interpolation directly
    // inside event handler attributes without compilation errors.
    var tailRef  = $"{aref}/tail";
    var tipRef   = $"{aref}/tip";
    var curveRef = $"{aref}/curve";

    <!-- Dashed selection outline -->
    <path d="@BuildArrowPath(arrow)"
          stroke="white" stroke-width="0.8" stroke-dasharray="1.5,1.5"
          fill="none" opacity="0.4" pointer-events="none"/>

    <!-- Tail handle -->
    <circle data-element="@tailRef"
            cx="@arrow.X1" cy="@(arrow.Y1 * _pitchHeight / 100)"
            r="1.8" fill="white" stroke="rgba(0,0,0,0.6)" stroke-width="0.3"
            style="cursor:move;"
            @onmousedown="(e) => HandleElementMouseDown(e, tailRef)"
            @onmousedown:stopPropagation="true"
            @onclick="() => {}"
            @onclick:stopPropagation="true"/>

    <!-- Tip handle -->
    <circle data-element="@tipRef"
            cx="@arrow.X2" cy="@(arrow.Y2 * _pitchHeight / 100)"
            r="1.8" fill="white" stroke="rgba(0,0,0,0.6)" stroke-width="0.3"
            style="cursor:move;"
            @onmousedown="(e) => HandleElementMouseDown(e, tipRef)"
            @onmousedown:stopPropagation="true"
            @onclick="() => {}"
            @onclick:stopPropagation="true"/>

    <!-- Curve handle (yellow — visually distinct from tail/tip) -->
    <circle data-element="@curveRef"
            cx="@arrow.Cx" cy="@(arrow.Cy * _pitchHeight / 100)"
            r="1.4" fill="#ffeb3b" stroke="rgba(0,0,0,0.6)" stroke-width="0.3"
            style="cursor:move;"
            @onmousedown="(e) => HandleElementMouseDown(e, curveRef)"
            @onmousedown:stopPropagation="true"
            @onclick="() => {}"
            @onclick:stopPropagation="true"/>
}
```

- [ ] **Step 2: Build to confirm no errors**

```bash
dotnet build src/FootballPlanner.Web
```
Expected: Build succeeded, 0 errors.

- [ ] **Step 3: Commit**

```bash
git add src/FootballPlanner.Web/Components/DiagramCanvas.razor
git commit -m "feat: render tail/tip/curve drag handles on selected arrow"
```

---

## Task 8: Dribble Curve Along Bezier Baseline

**Files:**
- Modify: `src/FootballPlanner.Web/Components/DiagramCanvas.razor`

Currently dribble waves are generated along a straight line. Update `BuildArrowPath` so dribble waves follow the quadratic Bezier baseline defined by the control point.

- [ ] **Step 1: Replace the dribble branch in `BuildArrowPath`**

In `DiagramCanvas.razor`, find the `BuildArrowPath` method and replace only the dribble branch (the `if (arrow.Style == ArrowStyle.Dribble)` block):

```csharp
if (arrow.Style == ArrowStyle.Dribble)
{
    const int waves = 4;
    var ic2 = System.Globalization.CultureInfo.InvariantCulture;
    string Fmt(double v) => v.ToString("F2", ic2);

    // Bezier point at parameter t: B(t) = (1-t)²·P0 + 2t(1-t)·Pc + t²·P1
    double Bx(double t) => (1-t)*(1-t)*x1 + 2*t*(1-t)*cx + t*t*x2;
    double By(double t) => (1-t)*(1-t)*y1 + 2*t*(1-t)*cy + t*t*y2;
    // Bezier tangent (un-normalized): B'(t) = 2(1-t)(Pc-P0) + 2t(P1-Pc)
    double Tx(double t) => 2*(1-t)*(cx-x1) + 2*t*(x2-cx);
    double Ty(double t) => 2*(1-t)*(cy-y1) + 2*t*(y2-cy);

    var sb = new System.Text.StringBuilder();
    sb.Append($"M {Fmt(Bx(0))} {Fmt(By(0))}");

    for (var i = 0; i < waves; i++)
    {
        double tm = (i + 0.5) / waves; // midpoint of this wave segment
        double te = (i + 1.0) / waves; // end of this wave segment

        double tangX = Tx(tm);
        double tangY = Ty(tm);
        double tlen = Math.Sqrt(tangX * tangX + tangY * tangY);

        // Bezier midpoint + perpendicular offset to create the wave bulge
        double pmx = Bx(tm);
        double pmy = By(tm);
        double amp = (i % 2 == 0 ? 1.5 : -1.5);
        double cpx = pmx + (tlen > 0 ? -tangY / tlen * amp : 0);
        double cpy = pmy + (tlen > 0 ?  tangX / tlen * amp : 0);

        sb.Append($" Q {Fmt(cpx)} {Fmt(cpy)} {Fmt(Bx(te))} {Fmt(By(te))}");
    }
    return sb.ToString();
}
```

- [ ] **Step 2: Build to confirm no errors**

```bash
dotnet build src/FootballPlanner.Web
```
Expected: Build succeeded, 0 errors.

- [ ] **Step 3: Commit**

```bash
git add src/FootballPlanner.Web/Components/DiagramCanvas.razor
git commit -m "feat: dribble arrow waves follow bezier curve baseline"
```

---

## Task 9: Arrow Properties Panel

**Files:**
- Modify: `src/FootballPlanner.Web/Components/DiagramElementPanel.razor`

Add arrow-specific controls (style toggle, colour picker, sequence number) when the selected element is an arrow.

- [ ] **Step 1: Add `Arrow` to the `ElementType` enum and update `OnParametersSet`**

In `DiagramElementPanel.razor`, in the `@code` block:

Replace:
```csharp
private enum ElementType { None, Player, Coach, Cone }

private ElementType _type = ElementType.None;
private int _teamIdx, _playerIdx, _coachIdx, _coneIdx;
private double _playerRadius, _coachRadius, _coneSize;
```

With:
```csharp
private enum ElementType { None, Player, Coach, Cone, Arrow, Legend }

private ElementType _type = ElementType.None;
private int _teamIdx, _playerIdx, _coachIdx, _coneIdx, _arrowIdx;
private double _playerRadius, _coachRadius, _coneSize;
private string? _arrowColor;
private int? _arrowSequence;
```

In `OnParametersSet`, add arrow and legend detection at the end of the if-else chain, before the `else { _type = ElementType.None; }`:

```csharp
else if (parts[0] == "arrows" && parts.Length == 2
    && int.TryParse(parts[1], out _arrowIdx)
    && _arrowIdx < State.Diagram.Arrows.Count)
{
    _type = ElementType.Arrow;
    _arrowColor = State.Diagram.Arrows[_arrowIdx].Color;
    _arrowSequence = State.Diagram.Arrows[_arrowIdx].SequenceNumber;
}
else if (sel == "legend" && State.Diagram.Legend != null)
{
    _type = ElementType.Legend;
}
else
{
    _type = ElementType.None;
}
```

- [ ] **Step 2: Add arrow and legend markup to the template section**

After the cone `@if` block and before the closing `}` of the outer `@if (_type != ElementType.None)`, add:

```razor
@if (_type == ElementType.Arrow)
{
    <MudText Typo="Typo.caption" Color="Color.Secondary">Style</MudText>
    <div class="d-flex gap-1 mb-1">
        @foreach (var style in new[] { ArrowStyle.Run, ArrowStyle.Pass, ArrowStyle.Dribble })
        {
            var s = style;
            var isActive = State.Diagram.Arrows[_arrowIdx].Style == s;
            <MudButton Size="Size.Small"
                       Variant="@(isActive ? Variant.Filled : Variant.Outlined)"
                       OnClick="() => HandleArrowStyle(s)">
                @s.ToString()
            </MudButton>
        }
    </div>

    <MudText Typo="Typo.caption" Color="Color.Secondary" Class="mt-1">Colour</MudText>
    <div class="d-flex align-center gap-1 mb-1">
        <input type="color"
               value="@(_arrowColor ?? "#ffffff")"
               @onchange="@(async e => await HandleArrowColor(e.Value?.ToString()))"
               style="width:32px;height:24px;border:none;background:none;cursor:pointer;padding:0;" />
        @if (_arrowColor != null)
        {
            <MudIconButton Icon="@Icons.Material.Filled.Clear" Size="Size.Small"
                           Title="Reset to default colour"
                           OnClick="() => HandleArrowColor(null)" />
        }
    </div>

    @{
        var swatches = State.Diagram.Arrows
            .Where((a, i) => i != _arrowIdx && a.Color != null)
            .Select(a => a.Color!)
            .Distinct()
            .ToList();
    }
    @if (swatches.Count > 0)
    {
        <div class="d-flex flex-wrap gap-1 mb-1">
            @foreach (var ec in swatches)
            {
                var c = ec;
                <div style="width:18px;height:18px;border-radius:3px;background:@c;cursor:pointer;border:@(_arrowColor == c ? "2px solid white" : "1px solid rgba(255,255,255,0.3)")"
                     title="@c"
                     @onclick="() => HandleArrowColor(c)" />
            }
        </div>
    }

    <MudText Typo="Typo.caption" Color="Color.Secondary" Class="mt-1">Sequence No.</MudText>
    <MudNumericField T="int?"
                     Value="@_arrowSequence"
                     ValueChanged="HandleArrowSequence"
                     Min="1" Max="99"
                     Dense="true" Margin="Margin.Dense"
                     Placeholder="–"
                     Style="width:80px;" />
}

@if (_type == ElementType.Legend)
{
    <MudButton Size="Size.Small"
               Color="Color.Warning"
               Variant="Variant.Outlined"
               OnClick="HandleRemoveLegend"
               Class="mt-1">
        Remove Legend
    </MudButton>
}
```

- [ ] **Step 3: Add handler methods to `@code`**

Append to the `@code` block:

```csharp
private async Task HandleArrowStyle(ArrowStyle style)
{
    State.ChangeArrowStyle(State.SelectedElement!, style);
    await OnChanged.InvokeAsync();
}

private async Task HandleArrowColor(string? color)
{
    _arrowColor = color;
    State.ChangeArrowColor(State.SelectedElement!, color);
    await OnChanged.InvokeAsync();
}

private async Task HandleArrowSequence(int? number)
{
    _arrowSequence = number;
    State.ChangeArrowSequenceNumber(State.SelectedElement!, number);
    await OnChanged.InvokeAsync();
}

private async Task HandleRemoveLegend()
{
    State.RemoveLegend();
    await OnChanged.InvokeAsync();
}
```

- [ ] **Step 4: Build to confirm no errors**

```bash
dotnet build src/FootballPlanner.Web
```
Expected: Build succeeded, 0 errors.

- [ ] **Step 5: Commit**

```bash
git add src/FootballPlanner.Web/Components/DiagramElementPanel.razor
git commit -m "feat: arrow properties panel — style, colour, sequence number"
```

---

## Task 10: Legend Rendering in Canvas

**Files:**
- Modify: `src/FootballPlanner.Web/Components/DiagramCanvas.razor`

Render the legend as a draggable, selectable, collapsible SVG group when `State.Diagram.Legend != null`.

- [ ] **Step 1: Add `_legendCollapsed` field to `@code`**

Add after `private bool _isDragging;`:

```csharp
private bool _legendCollapsed;
```

Also reset it in `OnAfterRenderAsync` or leave as `false` (default). The legend always opens expanded when the component initialises — `false` is correct.

- [ ] **Step 2: Add legend rendering at the end of the SVG template, before the ghost cursor section**

Add the following block just before the `<!-- Ghost cursor -->` comment:

```razor
<!-- Legend -->
@if (State.Diagram.Legend != null)
{
    var leg = State.Diagram.Legend;
    var ic3 = System.Globalization.CultureInfo.InvariantCulture;
    string LF(double v) => v.ToString("F2", ic3);

    var legendEntries = State.Diagram.Arrows
        .Where(a => a.SequenceNumber.HasValue)
        .Select(a => (Color: a.Color ?? ResolvedArrowColor(a.Style), Number: a.SequenceNumber!.Value))
        .Distinct()
        .OrderBy(e => e.Number)
        .ToList();

    const double legendWidth = 22.0;
    const double headerH = 5.0;
    const double rowH = 4.5;
    const double pad = 1.5;
    var contentH = _legendCollapsed ? 0.0 : legendEntries.Count * rowH;
    var legendH = headerH + contentH + pad;
    var lx = leg.X;
    var ly = leg.Y * _pitchHeight / 100;
    // Capture ref — Blazor Razor cannot use string literals directly in event handlers.
    var legendRef = "legend";
    var isLegendSelected = State.SelectedElement == legendRef;

    @if (isLegendSelected)
    {
        <rect x="@LF(lx - 0.5)" y="@LF(ly - 0.5)"
              width="@LF(legendWidth + 1)" height="@LF(legendH + 1)"
              fill="none" stroke="white" stroke-width="0.4" stroke-dasharray="1,1"
              pointer-events="none"/>
    }

    <!-- Background (draggable, selectable) -->
    <rect data-element="@legendRef"
          x="@LF(lx)" y="@LF(ly)"
          width="@LF(legendWidth)" height="@LF(legendH)"
          rx="1.5" ry="1.5"
          fill="rgba(0,0,0,0.65)" stroke="rgba(255,255,255,0.3)" stroke-width="0.4"
          style="cursor:pointer;"
          @onmousedown="(e) => HandleElementMouseDown(e, legendRef)"
          @onmousedown:stopPropagation="true"
          @onclick:stopPropagation="true"
          @onclick="() => HandleElementClick(legendRef)"/>

    <!-- Header label -->
    <text x="@LF(lx + pad)" y="@LF(ly + headerH / 2)"
          fill="white" font-size="2.5" font-weight="bold"
          dominant-baseline="middle" pointer-events="none">Legend</text>

    <!-- Collapse toggle -->
    <text x="@LF(lx + legendWidth - pad - 1.5)" y="@LF(ly + headerH / 2)"
          fill="white" font-size="2.8" dominant-baseline="middle" text-anchor="middle"
          style="cursor:pointer;"
          @onclick="() => _legendCollapsed = !_legendCollapsed"
          @onclick:stopPropagation="true">
        @(_legendCollapsed ? "▶" : "▼")
    </text>

    <!-- Entries -->
    @if (!_legendCollapsed)
    {
        for (var ri = 0; ri < legendEntries.Count; ri++)
        {
            var entry = legendEntries[ri];
            var ey = ly + headerH + ri * rowH + rowH / 2;
            <circle cx="@LF(lx + pad + 1.5)" cy="@LF(ey)"
                    r="1.2" fill="@entry.Color" pointer-events="none"/>
            <text x="@LF(lx + pad + 4.5)" y="@LF(ey)"
                  fill="white" font-size="2.5" dominant-baseline="middle"
                  pointer-events="none">@entry.Number</text>
        }
    }
}
```

- [ ] **Step 3: Build to confirm no errors**

```bash
dotnet build src/FootballPlanner.Web
```
Expected: Build succeeded, 0 errors.

- [ ] **Step 4: Commit**

```bash
git add src/FootballPlanner.Web/Components/DiagramCanvas.razor
git commit -m "feat: render movable collapsible legend derived from arrow sequence data"
```

---

## Task 11: Toolbar Legend Toggle

**Files:**
- Modify: `src/FootballPlanner.Web/Components/DiagramToolbar.razor`

Add a button to add/remove the legend from the diagram.

- [ ] **Step 1: Add legend toggle button to the toolbar**

In `DiagramToolbar.razor`, add after the existing `<MudIconButton aria-label="Delete element" .../>` button (before the closing `</div>`):

```razor
<MudDivider />

<MudIconButton aria-label="@(State.Diagram.Legend == null ? "Add legend" : "Remove legend")"
               Icon="@Icons.Material.Filled.Toc"
               Size="Size.Small"
               Color="@(State.Diagram.Legend != null ? Color.Primary : Color.Default)"
               OnClick="HandleLegendToggle" />
```

- [ ] **Step 2: Add the handler in `@code`**

Append to the `@code` block in `DiagramToolbar.razor`:

```csharp
private async Task HandleLegendToggle()
{
    if (State.Diagram.Legend == null)
        State.AddLegend();
    else
        State.RemoveLegend();
    await OnChanged.InvokeAsync();
}
```

- [ ] **Step 3: Build to confirm no errors**

```bash
dotnet build src/FootballPlanner.Web
```
Expected: Build succeeded, 0 errors.

- [ ] **Step 4: Commit**

```bash
git add src/FootballPlanner.Web/Components/DiagramToolbar.razor
git commit -m "feat: legend toggle button in diagram toolbar"
```

---

## Task 12: Feature Tests

**Files:**
- Modify: `tests/FootballPlanner.Feature.Tests/Journeys/DiagramJourney.cs`
- Create: `tests/FootballPlanner.Feature.Tests/Tests/DiagramArrowEditingTests.cs`

- [ ] **Step 1: Add helper methods to `DiagramJourney.cs`**

Append to the `DiagramJourney` class:

```csharp
/// <summary>Clicks a diagram element to select it (no tool active).</summary>
public async Task SelectElementAsync(string dataElement)
{
    await page.Locator($"[data-element='{dataElement}']").ClickAsync();
}

/// <summary>Clicks the "Add legend" or "Remove legend" toolbar button.</summary>
public async Task ToggleLegendAsync()
{
    var btn = page.GetByRole(AriaRole.Button, new() { Name = "Add legend" });
    if (await btn.IsVisibleAsync())
        await btn.ClickAsync();
    else
        await page.GetByRole(AriaRole.Button, new() { Name = "Remove legend" }).ClickAsync();
}

/// <summary>Returns true if the legend element is visible on the canvas.</summary>
public async Task<bool> LegendIsVisibleAsync()
{
    return await page.Locator("[data-element='legend']").IsVisibleAsync();
}

/// <summary>Returns true if a drag handle for the given arrow index and handle type is visible.</summary>
public async Task<bool> HandleIsVisibleAsync(int arrowIndex, string handle)
{
    return await page.Locator($"[data-element='arrows/{arrowIndex}/{handle}']").IsVisibleAsync();
}
```

- [ ] **Step 2: Create the feature test class**

Create `tests/FootballPlanner.Feature.Tests/Tests/DiagramArrowEditingTests.cs`:

```csharp
using FootballPlanner.Feature.Tests.Infrastructure;
using FootballPlanner.Feature.Tests.Journeys;

namespace FootballPlanner.Feature.Tests.Tests;

public class DiagramArrowEditingTests(FeatureTestFixture fixture) : IClassFixture<FeatureTestFixture>
{
    private async Task SetupAsync(string activityName)
    {
        await fixture.PhaseJourney.CreatePhaseAsync(new CreatePhaseInput("Warm Up", 1));
        await fixture.FocusJourney.CreateFocusAsync(new CreateFocusInput("Technique"));
        await fixture.ActivityJourney.CreateActivityAsync(
            new CreateActivityInput(activityName, "Arrow editing test", 10));
    }

    [Fact]
    public async Task ClickArrow_ShowsHandles()
    {
        await fixture.NewPageAsync();
        await SetupAsync("Arrow Handle Test");
        await fixture.DiagramJourney.OpenDiagramEditorAsync("Arrow Handle Test");

        await fixture.DiagramJourney.SelectToolAsync("Run arrow");
        await fixture.DiagramJourney.ClickCanvasAsync(0.3, 0.5);
        await fixture.DiagramJourney.ClickCanvasAsync(0.7, 0.5);

        // Deactivate tool then click the arrow to select it
        await fixture.DiagramJourney.SelectToolAsync("Run arrow");
        await fixture.DiagramJourney.SelectElementAsync("arrows/0");

        Assert.True(await fixture.DiagramJourney.HandleIsVisibleAsync(0, "tail"));
        Assert.True(await fixture.DiagramJourney.HandleIsVisibleAsync(0, "tip"));
        Assert.True(await fixture.DiagramJourney.HandleIsVisibleAsync(0, "curve"));
    }

    [Fact]
    public async Task DragArrowTipHandle_MovesEndpoint()
    {
        await fixture.NewPageAsync();
        await SetupAsync("Arrow Tip Drag Test");
        await fixture.DiagramJourney.OpenDiagramEditorAsync("Arrow Tip Drag Test");

        await fixture.DiagramJourney.SelectToolAsync("Run arrow");
        await fixture.DiagramJourney.ClickCanvasAsync(0.3, 0.5);
        await fixture.DiagramJourney.ClickCanvasAsync(0.5, 0.5);

        // Select arrow
        await fixture.DiagramJourney.SelectToolAsync("Run arrow");
        await fixture.DiagramJourney.SelectElementAsync("arrows/0");

        var tipBefore = await fixture.Page.Locator("[data-element='arrows/0/tip']").BoundingBoxAsync();
        Assert.NotNull(tipBefore);
        var fromX = tipBefore.X + tipBefore.Width / 2;
        var fromY = tipBefore.Y + tipBefore.Height / 2;

        var canvas = fixture.Page.GetByTestId("diagram-canvas");
        var canvasBox = await canvas.BoundingBoxAsync();
        Assert.NotNull(canvasBox);
        var toX = (float)(canvasBox.X + canvasBox.Width * 0.8);
        var toY = fromY;

        await fixture.Page.Mouse.MoveAsync(fromX, fromY);
        await fixture.Page.Mouse.DownAsync();
        await fixture.Page.Mouse.MoveAsync(toX, toY, new() { Steps = 20 });
        await fixture.Page.Mouse.UpAsync();

        var tipAfter = await fixture.Page.Locator("[data-element='arrows/0/tip']").BoundingBoxAsync();
        Assert.NotNull(tipAfter);
        var tipAfterX = tipAfter.X + tipAfter.Width / 2;
        Assert.True(tipAfterX > fromX + 30,
            $"Expected tip to move right, was {fromX:F0} before and {tipAfterX:F0} after");
    }

    [Fact]
    public async Task ToggleLegend_AddsAndRemovesLegend()
    {
        await fixture.NewPageAsync();
        await SetupAsync("Legend Toggle Test");
        await fixture.DiagramJourney.OpenDiagramEditorAsync("Legend Toggle Test");

        Assert.False(await fixture.DiagramJourney.LegendIsVisibleAsync());

        await fixture.DiagramJourney.ToggleLegendAsync();
        Assert.True(await fixture.DiagramJourney.LegendIsVisibleAsync());

        await fixture.DiagramJourney.ToggleLegendAsync();
        Assert.False(await fixture.DiagramJourney.LegendIsVisibleAsync());
    }

    [Fact]
    public async Task Legend_ShowsEntry_WhenArrowHasSequenceNumber()
    {
        await fixture.NewPageAsync();
        await SetupAsync("Legend Entry Test");
        await fixture.DiagramJourney.OpenDiagramEditorAsync("Legend Entry Test");

        // Place a run arrow
        await fixture.DiagramJourney.SelectToolAsync("Run arrow");
        await fixture.DiagramJourney.ClickCanvasAsync(0.3, 0.5);
        await fixture.DiagramJourney.ClickCanvasAsync(0.7, 0.5);

        // Select arrow and set sequence number via properties panel
        await fixture.DiagramJourney.SelectToolAsync("Run arrow");
        await fixture.DiagramJourney.SelectElementAsync("arrows/0");
        await fixture.Page.GetByLabel("Sequence No.").FillAsync("1");
        await fixture.Page.Keyboard.PressAsync("Tab");

        // Add legend and confirm it shows the entry
        await fixture.DiagramJourney.ToggleLegendAsync();
        var legend = fixture.Page.Locator("[data-element='legend']");
        await legend.WaitForAsync();
        Assert.True(await legend.IsVisibleAsync());
    }

    [Fact]
    public async Task SavedDiagram_ReopensWithLegend()
    {
        await fixture.NewPageAsync();
        await SetupAsync("Legend Persist Test");
        await fixture.DiagramJourney.OpenDiagramEditorAsync("Legend Persist Test");

        await fixture.DiagramJourney.ToggleLegendAsync();
        Assert.True(await fixture.DiagramJourney.LegendIsVisibleAsync());

        await fixture.DiagramJourney.SaveDiagramAsync();
        await fixture.DiagramJourney.OpenDiagramEditorAsync("Legend Persist Test");

        Assert.True(await fixture.DiagramJourney.LegendIsVisibleAsync());
    }
}
```

- [ ] **Step 3: Build to confirm no compilation errors**

```bash
dotnet build FootballPlanner.slnx
```
Expected: Build succeeded, 0 errors.

- [ ] **Step 4: Run unit tests to confirm nothing regressed**

```bash
dotnet test tests/FootballPlanner.Unit.Tests
dotnet test tests/FootballPlanner.Web.Tests
```
Expected: All tests pass.

- [ ] **Step 5: Commit**

```bash
git add tests/FootballPlanner.Feature.Tests/Journeys/DiagramJourney.cs \
        tests/FootballPlanner.Feature.Tests/Tests/DiagramArrowEditingTests.cs
git commit -m "test: Playwright feature tests for arrow handles and legend"
```

---

## Verification Checklist

Before declaring the feature complete:

- [ ] Arrowhead tips are clean and sharp (no blunt square cap overlapping the tip)
- [ ] Selecting an arrow (clicking with no tool) shows tail, tip, and curve handles
- [ ] Dragging tail/tip handle repositions the endpoint; curve handle bends without moving endpoints
- [ ] Dragging the arrow body (not a handle) still moves the whole arrow
- [ ] Dribble arrows curve correctly when the curve handle is moved
- [ ] Arrow properties panel shows Style / Colour / Sequence Number controls
- [ ] Changing arrow colour updates the arrowhead marker colour
- [ ] Sequence numbers appear in the legend; legend derives entries from arrows at render time
- [ ] Legend toggle adds/removes the legend; legend is draggable
- [ ] Legend collapses and expands without affecting saved data
- [ ] Saved diagram with legend reopens with legend in same position
- [ ] All unit tests pass: `dotnet test tests/FootballPlanner.Unit.Tests && dotnet test tests/FootballPlanner.Web.Tests`
