let observer = null;
let dotNetRef = null;

export function initStickySearch(element, dotNetReference) {
    dotNetRef = dotNetReference;

    observer = new IntersectionObserver(
        (entries) => {
            entries.forEach(entry => {
                const isSticky = !entry.isIntersecting;
                dotNetRef.invokeMethodAsync('SetSticky', isSticky);
            });
        },
        {
            root: null,
            rootMargin: '-64px 0px 0px 0px',
            threshold: 0
        }
    );

    observer.observe(element);
}

export function destroyStickySearch() {
    if (observer) {
        observer.disconnect();
        observer = null;
    }
    dotNetRef = null;
}