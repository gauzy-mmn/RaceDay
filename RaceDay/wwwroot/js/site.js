// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
/* RaceDay v2 progressive enhancement. No authentication or form interception. */
(() => {
    const reduceMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    if (reduceMotion || !('IntersectionObserver' in window)) return;
    const elements = document.querySelectorAll('.rd2-observe');
    if (!elements.length) return;
    // Add the hidden state only once the observer exists so content is never trapped invisible.
    const observer = new IntersectionObserver((entries, obs) => {
        for (const entry of entries) {
            if (entry.isIntersecting) {
                entry.target.classList.add('rd2-visible');
                obs.unobserve(entry.target);
            }
        }
    }, { rootMargin: '0px 0px -35px 0px', threshold: .08 });
    document.documentElement.classList.add('rd2-motion-ready');
    elements.forEach(element => observer.observe(element));
})();
