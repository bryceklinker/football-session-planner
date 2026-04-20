// diagram-interop.js
// Handles all mouse tracking for the SVG pitch diagram.
// attachDrag() is called once on first render and handles mousedown detection
// on [data-element] children, plus window-level mousemove/mouseup for drag tracking.

window.diagramInterop = (function () {
    console.log('[diagramInterop] loaded');
    let _listeners = new Map(); // svgId -> { mousedown, move, up, svg }

    function getSvgCoordinates(svgId, clientX, clientY) {
        const svg = document.getElementById(svgId);
        if (!svg) return { x: 0, y: 0 };
        return _svgCoords(svg, clientX, clientY);
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

    // Attaches a mousedown listener to the SVG and window-level mousemove/mouseup.
    // Called once from DiagramCanvas.OnAfterRenderAsync. The SVG DOM element is
    // reused across Blazor re-renders so the listener persists for the lifetime
    // of the component.
    function attachDrag(dotNetRef, svgId) {
        console.log('[diagramInterop] attachDrag called for svgId=' + svgId);
        cleanup(svgId);

        const svg = document.getElementById(svgId);
        if (!svg) {
            console.warn('[diagramInterop] attachDrag: SVG element not found for id=' + svgId);
            return;
        }

        let activeElement = null;

        const onMouseDown = function(ev) {
            // Walk up from the event target to find a [data-element] ancestor inside the SVG.
            let el = ev.target;
            let elementRef = null;
            while (el && el !== svg) {
                const ref = el.getAttribute('data-element');
                if (ref) { elementRef = ref; break; }
                el = el.parentElement;
            }
            if (!elementRef) return;

            console.log('[diagramInterop] element mousedown: ref=' + elementRef);
            activeElement = elementRef;
            dotNetRef.invokeMethodAsync('OnElementMouseDown', elementRef)
                .catch(function(err) { console.error('[diagramInterop] OnElementMouseDown failed:', err); });
        };

        const onMove = function(ev) {
            if (!activeElement) return;
            const c = _svgCoords(svg, ev.clientX, ev.clientY);
            console.debug('[diagramInterop] mousemove: x=' + c.x.toFixed(1) + ' y=' + c.y.toFixed(1));
            dotNetRef.invokeMethodAsync('OnDragMove', c.x, c.y)
                .catch(function(err) { console.error('[diagramInterop] OnDragMove failed:', err); });
        };

        const onUp = function() {
            if (!activeElement) return;
            console.log('[diagramInterop] mouseup: ending drag, ref=' + activeElement);
            activeElement = null;
            dotNetRef.invokeMethodAsync('OnDragEnd')
                .catch(function(err) { console.error('[diagramInterop] OnDragEnd failed:', err); });
        };

        svg.addEventListener('mousedown', onMouseDown);
        window.addEventListener('mousemove', onMove);
        window.addEventListener('mouseup', onUp);
        _listeners.set(svgId, { mousedown: onMouseDown, move: onMove, up: onUp, svg });
        console.log('[diagramInterop] attachDrag: listeners registered');
    }

    function cleanup(svgId) {
        const entry = _listeners.get(svgId);
        if (!entry) return;
        if (entry.svg && entry.mousedown)
            entry.svg.removeEventListener('mousedown', entry.mousedown);
        window.removeEventListener('mousemove', entry.move);
        window.removeEventListener('mouseup', entry.up);
        _listeners.delete(svgId);
    }

    return { getSvgCoordinates, getElementRefAt, attachDrag, cleanup };
})();
