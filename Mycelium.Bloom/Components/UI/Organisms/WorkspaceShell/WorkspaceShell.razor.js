export function create(root, receiver) {
    const body = root.querySelector('.mb-workspace-shell__body');
    const main = body.querySelector('.mb-workspace-shell__main');
    const listeners = new AbortController();
    let settings;
    let drag = null;
    let pendingWidth = null;
    let measuredWidth;
    let commits = Promise.resolve();
    let disposed = false;
    let lastFocus = null;
    let observedLeft = null;

    const panel = () => body.querySelector('.mb-workspace-shell__right-panel');
    const splitter = () => body.querySelector('.mb-workspace-shell__right-splitter');
    const compact = () => getComputedStyle(body).display === 'block';

    function bounds() {
        const left = body.querySelector('.mb-workspace-shell__left-panel');
        const available = body.clientWidth - (left?.getBoundingClientRect().width ?? 0)
            - (splitter()?.getBoundingClientRect().width ?? 0);
        return [settings.minimum, Math.max(settings.minimum, Math.min(settings.maximum, available - settings.center))];
    }

    function measure() {
        if (disposed || !settings || !panel() || panel().hidden || compact() || !splitter()) {
            return;
        }
        const [minimum, maximum] = bounds();
        const width = Math.min(maximum, Math.max(minimum, drag?.width ?? pendingWidth ?? settings.preferred));
        measuredWidth = width;
        body.style.setProperty('--workspace-shell-effective-right-width', `${width}px`);
        const handle = splitter();
        handle.setAttribute('aria-valuemin', String(Math.round(minimum)));
        handle.setAttribute('aria-valuemax', String(Math.round(maximum)));
        handle.setAttribute('aria-valuenow', String(Math.round(width)));
        handle.setAttribute('aria-valuetext', `${Math.round(width)} pixels`);
    }

    function restoreFocus() {
        if (!lastFocus || (lastFocus.isConnected && lastFocus.getClientRects().length && !lastFocus.closest('[inert]'))) {
            return;
        }
        const target = panel()?.hidden
            ? document.getElementById(settings.returnFocusId) ?? main
            : body.querySelector('[data-narrow-active="true"]') ?? main;
        target.focus({ preventScroll: true });
        lastFocus = target;
    }

    const observer = new ResizeObserver(() => {
        if (compact()) {
            cancelDrag();
        }
        measure();
        if (settings) restoreFocus();
    });
    observer.observe(root);

    function cancelDrag() {
        const previous = drag;
        drag = null;
        if (previous?.handle.hasPointerCapture(previous.pointerId)) {
            previous.handle.releasePointerCapture(previous.pointerId);
        }
        measure();
    }

    function commit(width) {
        pendingWidth = width;
        commits = commits.then(async () => {
            if (!disposed) await receiver.invokeMethodAsync('CommitRightPanelWidth', width);
        });
        return commits.catch(error => {
            if (pendingWidth === width) pendingWidth = null;
            measure();
            throw error;
        });
    }

    root.addEventListener('focusin', event => { lastFocus = event.target; }, { signal: listeners.signal });
    root.addEventListener('pointerdown', event => {
        const handle = event.target.closest('.mb-workspace-shell__right-splitter');
        if (!handle || event.button !== 0 || !event.isPrimary || compact() || drag) return;
        event.preventDefault();
        handle.focus({ preventScroll: true });
        handle.setPointerCapture(event.pointerId);
        const width = measuredWidth ?? panel().getBoundingClientRect().width;
        drag = { handle, pointerId: event.pointerId, startX: event.clientX, initialWidth: width, width };
    }, { signal: listeners.signal });

    root.addEventListener('pointermove', event => {
        if (!drag || drag.pointerId !== event.pointerId) return;
        const [minimum, maximum] = bounds();
        drag.width = Math.min(maximum, Math.max(minimum, drag.initialWidth + drag.startX - event.clientX));
        measure();
    }, { signal: listeners.signal });

    root.addEventListener('pointerup', async event => {
        if (!drag || drag.pointerId !== event.pointerId) return;
        const width = drag.width;
        pendingWidth = width;
        cancelDrag();
        await commit(width);
    }, { signal: listeners.signal });

    for (const eventName of ['pointercancel', 'lostpointercapture']) {
        root.addEventListener(eventName, event => {
            if (drag?.pointerId === event.pointerId) cancelDrag();
        }, { signal: listeners.signal });
    }

    root.addEventListener('keydown', async event => {
        if (!event.target.matches('.mb-workspace-shell__right-splitter') || compact()) return;
        const [minimum, maximum] = bounds();
        const step = event.shiftKey ? 50 : 10;
        const current = measuredWidth ?? panel().getBoundingClientRect().width;
        let width;
        switch (event.key) {
            case 'ArrowLeft': width = current + step; break;
            case 'ArrowRight': width = current - step; break;
            case 'Home': width = minimum; break;
            case 'End': width = maximum; break;
            default: return;
        }
        event.preventDefault();
        width = Math.min(maximum, Math.max(minimum, width));
        pendingWidth = width;
        measure();
        await commit(width);
    }, { signal: listeners.signal });

    return {
        update(preferred, minimum, maximum, center, returnFocusId) {
            settings = { preferred, minimum, maximum, center, returnFocusId };
            if (pendingWidth === preferred) pendingWidth = null;
            const left = body.querySelector('.mb-workspace-shell__left-panel');
            if (left !== observedLeft) {
                if (observedLeft) observer.unobserve(observedLeft);
                if (left) observer.observe(left);
                observedLeft = left;
            }
            if (!splitter() || compact()) cancelDrag();
            measure();
            restoreFocus();
        },
        dispose() {
            disposed = true;
            cancelDrag();
            observer.disconnect();
            listeners.abort();
        }
    };
}
