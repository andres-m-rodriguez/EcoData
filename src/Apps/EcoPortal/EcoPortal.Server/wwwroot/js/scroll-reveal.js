// Scroll Reveal - adds 'in-view' class when elements scroll into viewport
(function() {
    const observer = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.classList.add('in-view');
            }
        });
    }, {
        threshold: 0.3,
        rootMargin: '0px 0px -50px 0px'
    });

    function observeElements() {
        document.querySelectorAll('.scroll-reveal').forEach(el => {
            if (!el.dataset.observed) {
                observer.observe(el);
                el.dataset.observed = 'true';
            }
        });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', observeElements);
    } else {
        observeElements();
    }

    const mutationObserver = new MutationObserver(observeElements);
    mutationObserver.observe(document.body, {
        childList: true,
        subtree: true
    });
})();
