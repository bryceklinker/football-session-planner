// diagram-interop.js
// Handles mouse tracking during drag on the SVG pitch.
// All other diagram logic lives in C#.

let _listeners = new Map(); // svgId -> { move, up, svg }

export function getSvgCoordinates(svgId, clientX, clientY) {
    const svg = document.getElementById(svgId);
    if (!svg) return { x: 0, y: 0 };
    const rect = svg.getBoundingClientRect();
    const x = ((clientX - rect.left) / rect.width) * 100;
    const y = ((clientY - rect.top) / rect.height) * 100;
    return { x: Math.max(0, Math.min(100, x)), y: Math.max(0, Math.min(100, y)) };
}

export function startDrag(dotNetRef, svgId) {
    cleanup(svgId); // remove any stale listeners

    const svg = document.getElementById(svgId);
    if (!svg) return;

    const onMove = (e) => {
        const coords = getSvgCoordinates(svgId, e.clientX, e.clientY);
        dotNetRef.invokeMethodAsync('OnDragMove', coords.x, coords.y);
    };

    const onUp = (e) => {
        const coords = getSvgCoordinates(svgId, e.clientX, e.clientY);
        dotNetRef.invokeMethodAsync('OnDragComplete', coords.x, coords.y);
        cleanup(svgId);
    };

    svg.addEventListener('mousemove', onMove);
    window.addEventListener('mouseup', onUp);
    _listeners.set(svgId, { move: onMove, up: onUp, svg });
}

export function cleanup(svgId) {
    const entry = _listeners.get(svgId);
    if (!entry) return;
    entry.svg.removeEventListener('mousemove', entry.move);
    window.removeEventListener('mouseup', entry.up);
    _listeners.delete(svgId);
}
