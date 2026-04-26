# Arrow Editing — Design Spec

**Date:** 2026-04-26

## Overview

Extends the diagram editor with full arrow editing: a fix for the blunt arrowhead rendering bug, drag handles for reshaping arrows (endpoint repositioning and curve control), an arrow properties panel (style, color, sequence number), and an auto-generated movable legend that maps sequence colors to numbers.

---

## Bug Fix: Arrowhead Rendering

**Problem:** All three arrow markers use `refX="4"`, which places the path endpoint at the arrowhead *tip*. The stroke's butt cap creates a flat square end that overlaps and obscures the tip, making arrows look blunt.

**Fix:** Change `refX="4"` → `refX="0"` on all markers. This places the path endpoint at the arrowhead *base* — the arrowhead polygon extends forward beyond the path end, so the tip is never obscured by the stroke.

**Dynamic markers:** With per-arrow custom colors, the marker fill can no longer be shared across arrows. Each arrow gets its own `<marker>` in `<defs>`, keyed by element ref (e.g. `arr-{_svgId}-arrows-0`). The fill is set to the arrow's resolved color (custom color if set, otherwise the type-based default: white for Run, `#90caf9` for Pass, `#ffcc80` for Dribble).

---

## Data Model Changes

### `ArrowElement`

Two new optional fields with backward-compatible defaults:

```csharp
public record ArrowElement(
    ArrowStyle Style,
    double X1, double Y1,
    double X2, double Y2,
    double Cx, double Cy,
    string? Color = null,        // hex; null = type-based default
    int? SequenceNumber = null); // null = not included in legend
```

### `DiagramLegend`

New record storing only position. Collapsed/expanded is transient UI state — not persisted.

```csharp
public record DiagramLegend(double X = 5, double Y = 5);
```

### `DiagramModel`

```csharp
public record DiagramModel(
    ...,
    DiagramLegend? Legend = null);  // null = legend not shown
```

---

## Arrow Editing Handles

### Interaction model

- **Drag arrow body** → moves the whole arrow (existing behaviour, unchanged)
- **Click arrow body** → selects the arrow; shows three drag handles and the properties panel
- **Drag a handle** → reshapes the arrow; only the relevant point(s) move

### Handles

Three handles render on top of a selected arrow:

| Handle | Ref format | Effect |
|---|---|---|
| Tail | `arrows/0/tail` | Repositions start point (X1, Y1) |
| Tip | `arrows/0/tip` | Repositions end point (X2, Y2), changing length and direction |
| Curve | `arrows/0/curve` | Moves Bezier control point (Cx, Cy), bending the arrow |

The curve handle starts at the midpoint of the arrow and is shown for all three styles (Run, Pass, Dribble).

### Dribble curve

Currently the dribble wavy path generates perpendicular offsets along a straight baseline. With curve support, the waves follow the quadratic Bezier baseline (X1,Y1 → Cx,Cy → X2,Y2). At each wave segment the perpendicular offset is computed relative to the local tangent of the curve.

### `DiagramEditorState` changes

`MoveByDelta` gains handling for the three handle ref formats:
- `arrows/{i}/tail` — translate X1, Y1 and Cx, Cy by the same delta (preserves relative curve shape)
- `arrows/{i}/tip` — translate X2, Y2 and Cx, Cy by the same delta (preserves relative curve shape)
- `arrows/{i}/curve` — translate Cx, Cy by delta only (endpoints unchanged)

`BeginDrag` / `Undo` are unchanged — handle drags use the same speculative undo pattern as all other drags.

### `DiagramCanvas` changes

- Arrow `<path>` elements gain `@onclick:stopPropagation` and `@onclick` → `HandleElementClick`
- When `State.SelectedElement == aref`, three handle circles render after the path, each with their own `@onmousedown` handler using the sub-ref format
- Clicking the SVG background deselects (existing `OnDeselect` callback)

---

## Arrow Properties Panel

When `SelectedElement` is an arrow ref (`arrows/{i}`), `DiagramElementPanel` shows:

| Control | Details |
|---|---|
| Style | Toggle group: Run / Pass / Dribble |
| Color | Color picker (hex input); clearing reverts to type-based default; quick-pick swatches derived from colors already used by other arrows in the diagram |
| Sequence number | Integer input (1–99); clearable (null = not in legend) |

### New `DiagramEditorState` methods

```csharp
void ChangeArrowStyle(string elementRef, ArrowStyle style)     // pushes undo
void ChangeArrowColor(string elementRef, string? color)        // pushes undo
void ChangeArrowSequenceNumber(string elementRef, int? number) // pushes undo
```

---

## Legend

### Adding / removing

`DiagramToolbar` gains a "Legend" toggle button. When no legend exists, clicking adds one at the default position (top-left of the canvas). When a legend exists, clicking removes it.

```csharp
void AddLegend()     // creates DiagramLegend at (5, 5); pushes undo
void RemoveLegend()  // sets Legend = null; pushes undo
```

### Rendering

The legend renders as an SVG `<g>` element in `DiagramCanvas`, drawn after all other elements so it appears on top. It is a selectable, draggable element with ref `"legend"`.

**Collapsed/expanded** state is `bool _legendCollapsed` (local field in `DiagramCanvas`, default `false`). Clicking the collapse toggle flips it — no undo, no persistence. The legend always opens expanded when a diagram loads.

**Expanded layout:**
- Rounded rect background
- Header row: "Legend" label + collapse toggle (▼)
- One row per unique `(Color, SequenceNumber)` pair derived from arrows where `SequenceNumber != null`
- Each row: filled circle in the entry color + sequence number label
- Rows sorted ascending by sequence number

**Collapsed layout:**
- Header row only (▶ toggle); background shrinks to fit

### Legend properties panel

When `SelectedElement == "legend"`, `DiagramElementPanel` shows a single "Remove Legend" button (convenience shortcut for the toolbar toggle).

### Legend content derivation

Computed at render time — no stored list:

```csharp
var entries = State.Diagram.Arrows
    .Where(a => a.SequenceNumber.HasValue)
    .Select(a => (Color: a.Color ?? ResolvedColor(a.Style), Number: a.SequenceNumber!.Value))
    .Distinct()
    .OrderBy(e => e.Number)
    .ToList();
```

---

## Testing

### `DiagramEditorStateTests.cs` — new tests

- `ChangeArrowStyle_UpdatesStyle`
- `ChangeArrowColor_UpdatesColor`
- `ChangeArrowColor_Null_ClearsColor`
- `ChangeArrowSequenceNumber_UpdatesNumber`
- `ChangeArrowSequenceNumber_Null_ClearsNumber`
- `MoveByDelta_ArrowTailHandle_MovesStartPointAndRecomputesMidpoint`
- `MoveByDelta_ArrowTipHandle_MovesEndPointAndRecomputesMidpoint`
- `MoveByDelta_ArrowCurveHandle_MovesControlPointOnly`
- `AddLegend_CreateLegendAtDefaultPosition`
- `RemoveLegend_SetsLegendToNull`
- `AddLegend_WhenSelectedElement_ClearsSelection`
- `RemoveLegend_WhenLegendSelected_ClearsSelection`

### `DiagramCanvasTests.cs` — new tests

- `Renders_ArrowHandles_WhenArrowSelected`
- `Renders_NoHandles_WhenArrowNotSelected`
- `LegendCollapse_Toggle_UpdatesLocalState`
- `Renders_LegendEntries_DerivedFromArrows`
- `Renders_NoLegend_WhenLegendIsNull`
