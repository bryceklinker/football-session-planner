// diagram-interop.js
// Handles mouse tracking during drag on the SVG pitch.
// All other diagram logic lives in C#.

window.diagramInterop = (function () {
    console.log('[diagramInterop] loaded');
    let _listeners = new Map(); // svgId -> { move, up }

    function getSvgCoordinates(svgId, clientX, clientY) {
        const svg = document.getElementById(svgId);
        if (!svg) return { x: 0, y: 0 };
        const rect = svg.getBoundingClientRect();
        const x = ((clientX - rect.left) / rect.width) * 100;
        const y = ((clientY - rect.top) / rect.height) * 100;
        return { x: Math.max(0, Math.min(100, x)), y: Math.max(0, Math.min(100, y)) };
    }

    function _svgCoords(svg, clientX, clientY) {
        if (!svg) return { x: clientX, y: clientY };
        const rect = svg.getBoundingClientRect();
        const x = ((clientX - rect.left) / rect.width) * 100;
        const y = ((clientY - rect.top) / rect.height) * 100;
        return { x: Math.max(0, Math.min(100, x)), y: Math.max(0, Math.min(100, y)) };
    }

    // Returns the data-element ref of the topmost draggable SVG element at the given
    // client coordinates, or null if no draggable element is there.
    function getElementRefAt(svgId, clientX, clientY) {
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

    // Registers window mousemove and mouseup listeners so drag tracks the cursor
    // anywhere on the page, not just over the SVG.
    function startDrag(dotNetRef, svgId) {
        console.log('[diagramInterop] startDrag called for svgId=' + svgId);
        cleanup(svgId);
        const svg = document.getElementById(svgId);
        if (!svg) {
            console.warn('[diagramInterop] startDrag: SVG element not found for id=' + svgId);
        }
        const onMove = function (ev) {
            const c = _svgCoords(svg, ev.clientX, ev.clientY);
            console.debug('[diagramInterop] mousemove: x=' + c.x.toFixed(1) + ' y=' + c.y.toFixed(1));
            dotNetRef.invokeMethodAsync('OnDragMove', c.x, c.y)
                .catch(function(err) { console.error('[diagramInterop] OnDragMove failed:', err); });
        };
        const onUp = function () {
            console.log('[diagramInterop] mouseup: ending drag');
            dotNetRef.invokeMethodAsync('OnDragEnd')
                .catch(function(err) { console.error('[diagramInterop] OnDragEnd failed:', err); });
            cleanup(svgId);
        };
        window.addEventListener('mousemove', onMove);
        window.addEventListener('mouseup', onUp);
        _listeners.set(svgId, { move: onMove, up: onUp });
        console.log('[diagramInterop] startDrag: listeners registered');
    }

    function cleanup(svgId) {
        const entry = _listeners.get(svgId);
        if (!entry) return;
        window.removeEventListener('mousemove', entry.move);
        window.removeEventListener('mouseup', entry.up);
        _listeners.delete(svgId);
    }

    return { getSvgCoordinates, getElementRefAt, startDrag, cleanup };
})();
