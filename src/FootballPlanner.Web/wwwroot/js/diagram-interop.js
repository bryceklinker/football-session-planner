// diagram-interop.js
// Handles mouse tracking during drag on the SVG pitch.
// All other diagram logic lives in C#.

let _listeners = new Map(); // svgId -> { up }

export function getSvgCoordinates(svgId, clientX, clientY) {
    const svg = document.getElementById(svgId);
    if (!svg) return { x: 0, y: 0 };
    const rect = svg.getBoundingClientRect();
    const x = ((clientX - rect.left) / rect.width) * 100;
    const y = ((clientY - rect.top) / rect.height) * 100;
    return { x: Math.max(0, Math.min(100, x)), y: Math.max(0, Math.min(100, y)) };
}

// Returns the data-element ref of the topmost draggable SVG element at the given
// client coordinates, or null if no draggable element is there.
export function getElementRefAt(svgId, clientX, clientY) {
    const el = document.elementFromPoint(clientX, clientY);
    if (!el) return null;
    let current = el;
    while (current && current !== document.body) {
        const ref = current.getAttribute('data-element');
        if (ref) return ref;
        current = current.parentElement;
    }
    return null;
}

// Registers a window mouseup listener so drag ends even when the mouse is
// released outside the SVG. Blazor's @onmousemove on the SVG handles the
// visual preview during drag — no mousemove listener is needed here.
export function startDrag(dotNetRef, svgId) {
    cleanup(svgId);
    const onUp = () => {
        dotNetRef.invokeMethodAsync('OnDragEnd');
        cleanup(svgId);
    };
    window.addEventListener('mouseup', onUp);
    _listeners.set(svgId, { up: onUp });
}

export function cleanup(svgId) {
    const entry = _listeners.get(svgId);
    if (!entry) return;
    window.removeEventListener('mouseup', entry.up);
    _listeners.delete(svgId);
}
