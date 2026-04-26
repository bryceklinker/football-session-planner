# Diagram: Element Properties, Resize, and Notes

## Goals

1. **Notes area** — right-panel text area, persisted in `DiagramModel.Notes`
2. **Resizable elements** — players, coaches, cones each carry a size value; resize via properties panel
3. **Element editing** — players can be re-assigned to a different team; cones can change colour

---

## Data Model Changes (`DiagramModel.cs`)

| Record | New fields |
|---|---|
| `DiagramModel` | `string? Notes = null` |
| `PlayerElement` | `double Radius = 2.0` |
| `CoachElement` | `double Radius = 2.0` |
| `ConeElement` | `double Size = 1.0`, `string Color = "#f0a500"` |

All new fields have defaults so old saved JSON deserialises without errors.

---

## State Changes (`DiagramEditorState.cs`)

New property:
- `string? SelectedElement` — elementRef of the currently selected element (null = nothing selected)

New methods:
- `SelectElement(string? elementRef)` — sets selection (no undo)
- `SetNotes(string? notes)` — updates `Diagram.Notes` (no undo — avoids polluting undo stack on every keystroke)
- `ResizeElement(string elementRef, double value)` — sets `Radius` for players/coaches, `Size` for cones; clamps to valid range; pushes undo
- `ChangeConeColor(string elementRef, string color)` — sets cone `Color`; pushes undo
- `TransferPlayer(string elementRef, string newTeamId)` — moves player to another team; updates `SelectedElement` to new ref; pushes undo

Existing method changes:
- `Initialize` — resets `SelectedElement = null`
- `DeleteElement` — clears `SelectedElement` if it matches the deleted element
- `Undo` / `Redo` / `Clear` — reset `SelectedElement = null` (state may be inconsistent after history jump)

---

## Canvas Changes (`DiagramCanvas.razor`)

Rendering:
- Players/coaches use `r="@element.Radius"` (was hardcoded `r="2"`)
- Cones scale polygon offsets by `cone.Size`; use `fill="@cone.Color"` (was hardcoded `#f0a500`)
- Dashed selection ring rendered on top of any selected element

Behaviour:
- `HandleElementClick` with no active tool → fires existing `OnElementClick` (unchanged)  
- `HandleSvgClick` with no active tool → fires new `OnDeselect EventCallback` so the modal can clear selection

New parameter: `EventCallback OnDeselect`

---

## New Component: `DiagramElementPanel.razor`

Shows a compact property editor when `State.SelectedElement != null`.  
Parameters: `DiagramEditorState State`, `EventCallback OnChanged`

Displays based on selected element type:

| Type | Controls |
|---|---|
| Player | Size slider (1–5, step 0.5), Team dropdown |
| Coach | Size slider (1–5, step 0.5) |
| Cone | Size slider (0.5–4, step 0.5), Colour swatches |

---

## Modal Changes (`DiagramEditorModal.razor`)

Right panel widened from `160px` → `220px`.  
New panel stacking order (top→bottom):
1. `DiagramTeamsPanel`
2. `DiagramElementPanel` (only when `_state.SelectedElement != null`)
3. Notes `MudTextField` (always visible, multiline)

Wire up `OnDeselect="Deselect"` on `DiagramCanvas`.  
`HandleElementClick` now also calls `_state.SelectElement(elementRef)` when tool is not delete.

---

## Test Coverage

### `DiagramEditorStateTests.cs` — new tests
- `SetNotes_UpdatesNotes`
- `SelectElement_SetsSelectedElement`
- `SelectElement_Null_ClearsSelection`
- `ResizeElement_Player_UpdatesRadius`
- `ResizeElement_Coach_UpdatesRadius`
- `ResizeElement_Cone_UpdatesSize`
- `ResizeElement_ClampsToMinAndMax`
- `ChangeConeColor_UpdatesColor`
- `TransferPlayer_MovesPlayerToNewTeam_UpdatesRef`
- `DeleteElement_WhenSelectedElement_ClearsSelection`
- `Undo_ClearsSelectedElement`

### `DiagramCanvasTests.cs` — new tests
- `Renders_SelectionRing_ForSelectedElement`
- `SvgClick_WithNoTool_FiresOnDeselect`
- `ElementClick_WithNoTool_DoesNotFireOnDeselect`
