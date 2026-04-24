// diagram-interop.js
// Coordinate helpers and drag handling for the SVG pitch diagram.
// Drag is handled at the document level (not SVG element level) to ensure reliable
// mouse capture even when the pointer moves outside the SVG during fast drags.

window.diagramInterop = (function () {
    console.log('[diagramInterop] loaded');

    // Holds cleanup fn for the active drag so we can cancel on dispose.
    let _activeDragCleanup = null;

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

    function getSvgRect(svgId) {
        const svg = document.getElementById(svgId);
        if (!svg) return null;
        const r = svg.getBoundingClientRect();
        return { left: r.left, top: r.top, width: r.width, height: r.height };
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

    // Start a document-level drag for the SVG element identified by elementRef.
    // During the drag the element is moved visually via a SVG transform; on mouseup
    // the transform is removed and OnDragEnd is called on the Blazor component.
    function startDrag(svgId, elementRef, startClientX, startClientY, dotNetRef) {
        var svg = document.getElementById(svgId);
        if (!svg) { console.error('[diagramInterop] startDrag: SVG not found', svgId); return; }

        var svgRect = svg.getBoundingClientRect();

        // Parse viewBox from the attribute string — more reliable than viewBox.baseVal.
        var vbWidth = 100, vbHeight = 64.76;
        var vbAttr = svg.getAttribute('viewBox');
        if (vbAttr) {
            var vbParts = vbAttr.trim().split(/[\s,]+/);
            if (vbParts.length >= 4) {
                vbWidth = parseFloat(vbParts[2]) || 100;
                vbHeight = parseFloat(vbParts[3]) || 64.76;
            }
        }

        var el = svg.querySelector('[data-element="' + elementRef + '"]');
        if (!el) { console.error('[diagramInterop] startDrag: element not found', elementRef); return; }

        // Cancel any pre-existing drag.
        if (_activeDragCleanup) _activeDragCleanup();

        var startPixelX = startClientX - svgRect.left;
        var startPixelY = startClientY - svgRect.top;
        // Start position in model coordinates (0–100 for both axes).
        var startModelX = svgRect.width  > 0 ? (startPixelX / svgRect.width)  * 100 : 0;
        var startModelY = svgRect.height > 0 ? (startPixelY / svgRect.height) * 100 : 0;

        function onMove(e) {
            var dxPx = (e.clientX - svgRect.left) - startPixelX;
            var dyPx = (e.clientY - svgRect.top)  - startPixelY;
            // Convert pixel delta to SVG viewBox coordinate delta for the visual transform.
            var dxSvg = svgRect.width  > 0 ? (dxPx / svgRect.width)  * vbWidth  : 0;
            var dySvg = svgRect.height > 0 ? (dyPx / svgRect.height) * vbHeight : 0;
            el.setAttribute('transform', 'translate(' + dxSvg + ' ' + dySvg + ')');
        }

        function onUp(e) {
            cleanup();
            // Remove the visual transform — Blazor re-renders the element at the new model position.
            el.removeAttribute('transform');
            // Pass the model-coordinate delta so Blazor can translate all element points uniformly.
            var endModelX = svgRect.width  > 0 ? (e.clientX - svgRect.left) / svgRect.width  * 100 : 0;
            var endModelY = svgRect.height > 0 ? (e.clientY - svgRect.top)  / svgRect.height * 100 : 0;
            var deltaX = endModelX - startModelX;
            var deltaY = endModelY - startModelY;
            dotNetRef.invokeMethodAsync('OnDragEnd', elementRef, deltaX, deltaY)
                .catch(function (err) { console.error('[diagramInterop] OnDragEnd failed:', err); });
        }

        function cleanup() {
            document.removeEventListener('mousemove', onMove);
            document.removeEventListener('mouseup', onUp);
            _activeDragCleanup = null;
        }

        document.addEventListener('mousemove', onMove);
        document.addEventListener('mouseup', onUp);
        _activeDragCleanup = cleanup;
    }

    // Remove any active document-level drag listeners (called on component dispose).
    function cancelDrag() {
        if (_activeDragCleanup) _activeDragCleanup();
    }

    return { getSvgCoordinates, getElementRefAt, getSvgRect, startDrag, cancelDrag };
})();
