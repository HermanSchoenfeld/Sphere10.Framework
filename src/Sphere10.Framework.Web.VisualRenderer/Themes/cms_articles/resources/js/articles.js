(function () {
    document.querySelectorAll('.ln-articles-portal').forEach(function (portal) {
        if (portal.dataset.lnArticlesInitialized)
            return;
        portal.dataset.lnArticlesInitialized = 'true';
        var topics = portal.querySelector('.ln-articles-topics');
        var desktop = window.matchMedia('(min-width: 992px)');
        function setTopicPresentation() {
            topics.open = desktop.matches;
        }
        setTopicPresentation();
        desktop.addEventListener('change', setTopicPresentation);

        // Observe the settled frame after load, keeping the existing page-loader lifecycle intact.
        function observeHeader() {
            var navbars = Array.from(document.querySelectorAll('.navbar'));
            function measureHeader() {
                var height = navbars.reduce(function (maximum, navbar) {
                    var style = window.getComputedStyle(navbar);
                    if (style.position !== 'sticky' && style.position !== 'fixed')
                        return maximum;
                    return Math.max(maximum, navbar.getBoundingClientRect().height + (parseFloat(style.top) || 0));
                }, 0);
                var value = height + 'px';
                if (portal.style.getPropertyValue('--ln-articles-measured-header-height') !== value)
                    portal.style.setProperty('--ln-articles-measured-header-height', value);
            }
            measureHeader();
            if (typeof ResizeObserver !== 'undefined') {
                var observer = new ResizeObserver(measureHeader);
                navbars.forEach(function (navbar) { observer.observe(navbar); });
            }
            window.addEventListener('resize', measureHeader, { passive: true });
        }
        if (document.readyState === 'complete')
            observeHeader();
        else
            window.addEventListener('load', observeHeader, { once: true });
    });
})();
