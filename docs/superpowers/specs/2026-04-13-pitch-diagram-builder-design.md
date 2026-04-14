# Pitch Diagram Builder — Design Spec

**Date:** 2026-04-13

## Overview

A full-screen SVG diagram editor embedded in the Activity Library page. Coaches build pitch diagrams by selecting tools and clicking the pitch to place players, coaches, cones, goals, and movement arrows. Diagrams are serialized to JSON and stored in `Activity.DiagramJson`, then rendered as static read-only SVGs on the mobile session runner.

---

## Design Decisions (with mockups)

### Layout: Full-Screen Modal

The diagram editor opens as a full-screen MudDialog from an "Edit Diagram" button in the activity editor panel. Maximum canvas space; intentional context switch into "diagram mode".

> Mockup: [`assets/pitch-diagram-builder/mockup-layout-options.html`](assets/pitch-diagram-builder/mockup-layout-options.html) — Option B selected.

### Placement: Select Tool, Click to Place

Click a tool in the left sidebar to activate it (highlighted). Click anywhere on the pitch to place the element. Switch to the move tool (✥) to drag existing elements. A ghost indicator shows where the next click will place.

> Mockup: [`assets/pitch-diagram-builder/mockup-placement-mechanic.html`](assets/pitch-diagram-builder/mockup-placement-mechanic.html) — Option A selected.

### Arrow Styles: Classic Coaching Convention

| Type | Appearance |
|------|-----------|
| Run | Solid white line |
| Pass | Dashed blue line |
| Dribble | Wavy/sinusoidal orange path |

All arrows support a Bezier curve control point. After drawing, a midpoint handle appears — drag it to bend the arrow. Default (unmodified) is straight.

> Mockup: [`assets/pitch-diagram-builder/mockup-arrow-styles.html`](assets/pitch-diagram-builder/mockup-arrow-styles.html) — Option A selected.

---

## Architecture

**Approach:** Blazor SVG with a thin JS drag module.

- Blazor renders the SVG and owns all state (element lists, selected tool, undo/redo stacks, serialization)
- A small isolated JS module (`diagram-interop.js`, ~50 lines) handles only mouse tracking during drag — fires `OnDragMove` and `OnDragComplete` callbacks to Blazor with percentage coordinates
- Everything else (click-to-place, arrow drawing, undo/redo) stays in C#
- No external JS canvas libraries

**Coordinate system:** All positions stored as percentages of the pitch rectangle (0–100 × 0–100). Diagrams scale correctly on any screen size.

---

## Data Model

`Activity.DiagramJson` stores a single JSON object. Serialized using `System.Text.Json` with `JsonStringEnumConverter` for all enums.

### Enums

```csharp
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
```

### Records

```csharp
public record DiagramModel(
    PitchFormat PitchFormat,
    double? CustomWidth,            // only set when PitchFormat == Custom
    double? CustomHeight,
    List<DiagramTeam> Teams,
    List<CoachElement> Coaches,
    List<ConeElement> Cones,
    List<GoalElement> Goals,
    List<ArrowElement> Arrows);

public record DiagramTeam(
    string Id,                      // short generated id e.g. "t1"
    string Name,                    // "Red", "Blues", "Yellows" — free text
    string Color,                   // hex e.g. "#e94560"
    List<PlayerElement> Players);

public record PlayerElement(
    string Label,                   // "", "9", "A", "GK" — empty = plain circle
    double X, double Y);

public record CoachElement(
    string Label,                   // "C", "Coach", a name
    double X, double Y);

public record ConeElement(double X, double Y);

public record GoalElement(
    double X, double Y,
    double Width);                  // as % of pitch width; height = Width / 7 (standard post ratio)

public record ArrowElement(
    ArrowStyle Style,
    double X1, double Y1,
    double X2, double Y2,
    double Cx, double Cy);          // Bezier control point; default = midpoint
```

**Notes:**
- Players belong to a team — deleting a team removes all its players
- Coaches are separate from teams; they render as gold (`#f0a500`) circles with a label
- New diagrams default to two teams (Red `#e94560`, Blue `#4169E1`) so simple activities require no setup
- `DiagramElement` base class is not needed — typed lists eliminate the need for JSON polymorphism

---

## Components

### `DiagramEditorModal.razor`
Full-screen MudDialog. Owns all state:
- `DiagramModel` (current diagram)
- Active tool (player/coach/cone/goal/arrow-run/arrow-pass/arrow-dribble/move/delete)
- Active team (for player placement)
- In-progress arrow (start point set, end point not yet clicked)
- Undo stack: `Stack<DiagramModel>` — max 50 snapshots
- Redo stack: `Stack<DiagramModel>` — cleared on any new mutation

Actions:
- **Save** — serialize `DiagramModel` to JSON, call `Api.SaveDiagramAsync`, close dialog
- **Clear** — wipe all elements, keep teams, push to undo stack
- **Cancel** — close without saving
- **Undo** — pop from undo stack, push current to redo stack
- **Redo** — pop from redo stack, push current to undo stack

Opens from a new "Edit Diagram" button in `Activities.razor`, replacing the "coming soon" placeholder. Passes existing `DiagramJson` (deserialized) as initial state.

### `DiagramCanvas.razor`
The SVG element. Receives `DiagramModel`, active tool, active team, and in-progress arrow start point as parameters. Emits `EventCallback` for each user action:

| Callback | When fired |
|----------|-----------|
| `OnPlacePlayer(double x, double y)` | Click while player tool active |
| `OnPlaceCoach(double x, double y)` | Click while coach tool active |
| `OnPlaceCone(double x, double y)` | Click while cone tool active |
| `OnPlaceGoal(double x, double y)` | Click while goal tool active |
| `OnArrowPoint(double x, double y)` | Click while any arrow tool active (first = start, second = end) |
| `OnMoveElement(string elementRef, double x, double y)` | JS drag complete |
| `OnSelectElement(string elementRef)` | Click while move tool active |
| `OnDeleteElement(string elementRef)` | Click while delete tool active |

Renders:
- Pitch background (green) and lines (halfway line, penalty areas, centre circle) matching the selected `PitchFormat` aspect ratio
- Goals, cones, coaches, team players (colored circles with optional label)
- Arrows as SVG paths with arrowhead markers; dribble arrows use a sinusoidal path
- Bezier control point handle on the currently selected arrow
- Ghost indicator at cursor position when a placement tool is active

`elementRef` is a string encoding the element list and index (e.g. `"teams/0/players/2"`, `"cones/1"`) so `DiagramEditorModal` can locate and mutate the correct element.

### `DiagramTeamsPanel.razor`
Sidebar panel inside the modal for managing teams:
- List of teams with color swatch and name
- Add team (generates id, default name "Team N", default color cycled from preset palette: `#e94560`, `#4169E1`, `#43a047`, `#f0a500`, `#9c27b0`, `#00acc1`)
- Rename team (inline edit)
- Recolor team (color picker)
- Delete team (removes team and all its players)
- Click a team to make it active for player placement

### `diagram-interop.js`
Exported ES module. Two functions:

```js
export function startDrag(dotNetRef, svgId)
// Attaches mousemove + mouseup to the SVG element identified by svgId.
// On mousemove: converts clientX/Y to SVG % coordinates,
//   calls dotNetRef.invokeMethodAsync("OnDragMove", x, y) for live preview.
// On mouseup: calls dotNetRef.invokeMethodAsync("OnDragComplete", x, y),
//   then removes listeners.

export function cleanup(svgId)
// Removes any lingering listeners (called on modal close).
```

### File Layout

```
src/FootballPlanner.Web/
  Components/
    DiagramEditorModal.razor
    DiagramCanvas.razor
    DiagramTeamsPanel.razor
  Models/
    DiagramModel.cs
  wwwroot/js/
    diagram-interop.js
```

---

## Backend

### New endpoint
`PUT /activities/{id}/diagram`

Thin Azure Function:
```csharp
var body = await req.ReadFromJsonAsync<SaveDiagramRequest>();
await mediator.Send(new SaveDiagramCommand(id, body.DiagramJson));
return req.CreateResponse(HttpStatusCode.NoContent);
private record SaveDiagramRequest(string? DiagramJson);
```

### Command
```csharp
public record SaveDiagramCommand(int ActivityId, string? DiagramJson) : IRequest;
```

Handler loads the activity by ID, calls `activity.UpdateDiagram(request.DiagramJson)`, saves. Throws `KeyNotFoundException` if activity not found (consistent with all other handlers).

Validator: `ActivityId > 0`. No validation on `DiagramJson` content — the column is a string store; schema is owned by the frontend.

### `ApiClient.cs`
```csharp
public Task<HttpResponseMessage> SaveDiagramAsync(int activityId, string? diagramJson) =>
    http.PutAsJsonAsync($"activities/{activityId}/diagram",
        new SaveDiagramRequest(diagramJson));
private record SaveDiagramRequest(string? DiagramJson);
```

---

## Pitch Rendering

Each `PitchFormat` has a fixed aspect ratio. The SVG `viewBox` is set to `"0 0 100 {height}"` where `height = 100 / aspectRatio`. This keeps all coordinates in 0–100 percentage space while the SVG scales to fill the available canvas.

| Format | Aspect ratio (W:H) |
|--------|--------------------|
| 11v11 Full | 100:64 |
| 11v11 Half | 50:64 |
| 9v9 Full | 80:50 |
| 9v9 Half | 40:50 |
| 7v7 Full | 60:40 |
| 7v7 Half | 30:40 |
| Custom | user-defined |

Pitch lines drawn for each format: halfway line, centre circle, penalty areas, goal areas. Half-pitch formats show only the relevant half.

Goals are placed by the user (not fixed to the pitch edges) — coaches sometimes place goals in non-standard positions for small-sided drills.

---

## Testing

### Unit tests (`FootballPlanner.Unit.Tests`)

**`SaveDiagramCommandTests.cs`**
- Saves diagram JSON to the activity
- Clears diagram when `null` is passed
- Throws `ValidationException` when `ActivityId` is 0
- Throws `KeyNotFoundException` for unknown activity ID

**`DiagramModelSerializationTests.cs`**
- Round-trip JSON serialization for each element type (`PlayerElement`, `CoachElement`, `ConeElement`, `GoalElement`, `ArrowElement`)
- All `PitchFormat` enum values serialize and deserialize correctly
- All `ArrowStyle` enum values serialize and deserialize correctly
- Null `CustomWidth`/`CustomHeight` round-trips correctly
- Multi-team diagram with players serializes correctly

**bUnit component tests (`FootballPlanner.Unit.Tests`)**

Add `bunit` NuGet package. Tests cover stateful diagram logic:

- Placing a player adds `PlayerElement` to the active team's `Players` list
- Placing a player with no teams defined does nothing
- Undo after placement restores previous `DiagramModel`
- Redo re-applies undone mutation
- Undo stack does not exceed 50 snapshots (oldest dropped)
- Clear removes all elements from all typed lists but preserves teams
- First canvas click with arrow tool sets in-progress start point; second click creates `ArrowElement` in `Arrows`
- Drag complete updates the target element's `X`/`Y`
- Deleting a team removes it and all its players
- Recoloring a team updates `Color` on the `DiagramTeam`

### Integration tests (`FootballPlanner.Integration.Tests`)

**`SaveDiagramEndpointTests.cs`**
- PUT diagram JSON for an activity, GET activity back, assert `DiagramJson` round-trips correctly
- PUT null clears the diagram

### Feature tests (`FootballPlanner.Feature.Tests`)

Smoke test: navigate to an activity, click "Edit Diagram", verify the modal opens, click Save without placing anything, verify the modal closes. Playwright coverage to be expanded once canvas interactions are stable.
