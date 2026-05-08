let observer = null;
let dotNetRef = null;
const elementStates = new Map();

export function observe(treeScrollEl, dotNet) {
    if (observer) {
        observer.disconnect();
        elementStates.clear();
    }
    dotNetRef = dotNet;

    observer = new IntersectionObserver((entries) => {
        for (const entry of entries) {
            const aboveFold =
                !entry.isIntersecting &&
                entry.rootBounds != null &&
                entry.boundingClientRect.top < entry.rootBounds.top;
            elementStates.set(entry.target, {
                key: entry.target.dataset.namespaceKey,
                aboveFold,
                top: entry.boundingClientRect.top
            });
        }

        let stickyKey = null;
        let highestTop = -Infinity;
        for (const [, state] of elementStates) {
            if (state.aboveFold && state.top > highestTop) {
                highestTop = state.top;
                stickyKey = state.key;
            }
        }

        dotNetRef?.invokeMethodAsync('SetStickyNamespace', stickyKey).catch(() => {});
    }, { root: treeScrollEl, threshold: 0 });

    for (const el of treeScrollEl.querySelectorAll('[data-namespace-key]')) {
        observer.observe(el);
    }
}

export function scrollToNamespace(treeScrollEl, key) {
    const el = treeScrollEl.querySelector(`[data-namespace-key="${CSS.escape(key)}"]`);
    el?.scrollIntoView({ behavior: 'smooth', block: 'start' });
}

export function dispose() {
    observer?.disconnect();
    observer = null;
    dotNetRef = null;
    elementStates.clear();
}
