let elements = [];
let dotNetRef = null;
let currentStickyKey = undefined;
let scrollHandler = null;
let treeScrollRoot = null;

export function observe(treeScrollEl, dotNet) {
    if (!treeScrollEl || typeof treeScrollEl.querySelectorAll !== 'function') return;
    dispose();
    dotNetRef = dotNet;
    treeScrollRoot = treeScrollEl;
    elements = [...treeScrollEl.querySelectorAll('[data-namespace-key]')];

    let ticking = false;
    scrollHandler = () => {
        if (!ticking) {
            ticking = true;
            requestAnimationFrame(() => {
                updateSticky();
                ticking = false;
            });
        }
    };
    treeScrollEl.addEventListener('scroll', scrollHandler, { passive: true });
    updateSticky();
}

function updateSticky() {
    if (!treeScrollRoot || !dotNetRef) return;
    const rootTop = treeScrollRoot.getBoundingClientRect().top;
    let stickyKey = null;
    let highestTop = -Infinity;

    for (const el of elements) {
        const top = el.getBoundingClientRect().top;
        if (top < rootTop && top >= highestTop) {
            highestTop = top;
            stickyKey = el.dataset.namespaceKey;
        }
    }

    if (stickyKey !== currentStickyKey) {
        currentStickyKey = stickyKey;
        dotNetRef.invokeMethodAsync('SetStickyNamespace', stickyKey).catch(() => {});
    }
}

export function scrollToNamespace(treeScrollEl, key) {
    const el = treeScrollEl.querySelector(`[data-namespace-key="${CSS.escape(key)}"]`);
    el?.scrollIntoView({ behavior: 'smooth', block: 'start' });
}

export function dispose() {
    if (scrollHandler && treeScrollRoot) {
        treeScrollRoot.removeEventListener('scroll', scrollHandler);
    }
    scrollHandler = null;
    dotNetRef = null;
    currentStickyKey = undefined;
    elements = [];
    treeScrollRoot = null;
}
