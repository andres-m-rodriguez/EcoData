// Scroll Reveal - adds 'in-view' class when elements scroll into viewport
(function() {
    const observer = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.classList.add('in-view');
            }
        });
    }, {
        threshold: 0.3, // Trigger when 30% of element is visible
        rootMargin: '0px 0px -50px 0px' // Trigger slightly before fully in view
    });

    // Observe elements when DOM is ready and after Blazor updates
    function observeElements() {
        document.querySelectorAll('.scroll-reveal').forEach(el => {
            if (!el.dataset.observed) {
                observer.observe(el);
                el.dataset.observed = 'true';
            }
        });
    }

    // Initial observation
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', observeElements);
    } else {
        observeElements();
    }

    // Re-observe after Blazor navigations/updates
    const mutationObserver = new MutationObserver(() => {
        observeElements();
    });

    mutationObserver.observe(document.body, {
        childList: true,
        subtree: true
    });
})();
