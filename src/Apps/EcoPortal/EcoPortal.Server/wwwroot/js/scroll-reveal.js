// Scroll Reveal - adds 'in-view' class when elements scroll into viewport
(function() {
    const ANIMATIONS_SEEN_KEY = 'ecoportal-home-animations-seen';

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

    // Check if we should skip hero animations on home page
    function checkHeroAnimations() {
        const isHomePage = window.location.pathname === '/';
        if (isHomePage) {
            if (sessionStorage.getItem(ANIMATIONS_SEEN_KEY) === 'true') {
                // Skip animations - add class to disable them
                document.body.classList.add('animations-seen');
            } else {
                // First visit - mark as seen
                sessionStorage.setItem(ANIMATIONS_SEEN_KEY, 'true');
            }
        }
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', () => {
            checkHeroAnimations();
            observeElements();
        });
    } else {
        checkHeroAnimations();
        observeElements();
    }

    const mutationObserver = new MutationObserver(() => {
        checkHeroAnimations();
        observeElements();
    });

    mutationObserver.observe(document.body, {
        childList: true,
        subtree: true
    });
})();
