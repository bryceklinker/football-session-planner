// diagram-interop.js
// Handles all mouse tracking for the SVG pitch diagram.
// attachDrag() is called once on first render. During drag, the element is moved
// visually via an SVG transform (no Blazor re-renders). On mouseup, the final
// position is sent to C# once, which updates the model and triggers one re-render.

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
    // During drag, the element is translated via an SVG transform attribute for
    // smooth visual feedback without Blazor re-renders.
    // On mouseup, OnDragEnd(finalX, finalY) is called once to update the model.
    function attachDrag(dotNetRef, svgId) {
        console.log('[diagramInterop] attachDrag called for svgId=' + svgId);
        cleanup(svgId);

        const svg = document.getElementById(svgId);
        if (!svg) {
            console.warn('[diagramInterop] attachDrag: SVG element not found for id=' + svgId);
            return;
        }

        let activeElement = null;  // element ref string ("cones/0", etc.)
        let activeSvgEl = null;    // the actual dragged SVG DOM element
        let startX = 0, startY = 0; // drag-start position in SVG % coords
        let lastX = 0, lastY = 0;   // most recent cursor position in SVG % coords

        const onMouseDown = function(ev) {
            // Walk up from event target to find the nearest [data-element] ancestor.
            let el = ev.target;
            let svgEl = null;
            let elementRef = null;
            while (el && el !== svg) {
                const ref = el.getAttribute('data-element');
                if (ref) { elementRef = ref; svgEl = el; break; }
                el = el.parentElement;
            }
            if (!elementRef) return;

            const c = _svgCoords(svg, ev.clientX, ev.clientY);
            console.log('[diagramInterop] element mousedown: ref=' + elementRef +
                        ' x=' + c.x.toFixed(1) + ' y=' + c.y.toFixed(1));

            activeElement = elementRef;
            activeSvgEl = svgEl;
            startX = c.x;
            startY = c.y;
            lastX = c.x;
            lastY = c.y;

            dotNetRef.invokeMethodAsync('OnElementMouseDown', elementRef)
                .catch(function(err) { console.error('[diagramInterop] OnElementMouseDown failed:', err); });
        };

        const onMove = function(ev) {
            if (!activeElement || !activeSvgEl) return;
            const c = _svgCoords(svg, ev.clientX, ev.clientY);
            lastX = c.x;
            lastY = c.y;

            // Translate element in viewBox units for immediate visual feedback.
            // SVG viewBox x range = 0..100 (same as % coords), y range = 0..pitchHeight.
            const vb = svg.viewBox.baseVal;
            const dx = c.x - startX;
            const dy = (c.y - startY) * vb.height / 100;
            activeSvgEl.setAttribute('transform', 'translate(' + dx + ',' + dy + ')');
        };

        const onUp = function() {
            if (!activeElement) return;
            const ref = activeElement;
            const fx = lastX;
            const fy = lastY;
            console.log('[diagramInterop] mouseup: ref=' + ref +
                        ' finalX=' + fx.toFixed(1) + ' finalY=' + fy.toFixed(1));

            activeElement = null;
            activeSvgEl = null; // Blazor re-render will replace the element without transform

            dotNetRef.invokeMethodAsync('OnDragEnd', fx, fy)
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
