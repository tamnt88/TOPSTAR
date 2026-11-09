(() => {
    "use strict";

    const root = document.querySelector(".brand-landing");
    if (!root) return;

    const partnerSwiper = root.querySelector(".brand-partners-swiper");
    if (partnerSwiper && typeof Swiper !== "undefined") {
        const count = Number(partnerSwiper.dataset.partnerCount || 0);
        if (count > 1) {
            new Swiper(partnerSwiper, {
                slidesPerView: 2,
                spaceBetween: 12,
                loop: count > 6,
                rewind: count <= 6,
                speed: 650,
                grabCursor: true,
                watchOverflow: true,
                autoplay: { delay: 2200, disableOnInteraction: false, pauseOnMouseEnter: true },
                breakpoints: {
                    480: { slidesPerView: 3 },
                    768: { slidesPerView: 4 },
                    992: { slidesPerView: 5 },
                    1200: { slidesPerView: 6 }
                }
            });
        }
    }

    const selectors = [
        ".brand-landing__hero-copy > *",
        ".brand-landing__visual",
        ".brand-landing__trust p",
        ".brand-partners-swiper .swiper-slide",
        ".brand-landing__section-head > *",
        ".brand-landing__benefits article",
        ".brand-landing__steps li",
        ".brand-landing__governance article",
        ".brand-landing__cta > .brand-landing__container > *"
    ];

    const elements = [...root.querySelectorAll(selectors.join(","))];
    elements.forEach((element) => {
        const siblings = [...(element.parentElement?.children ?? [])];
        const index = Math.max(0, siblings.indexOf(element));
        element.classList.add("bl-reveal");
        element.style.setProperty("--bl-delay", `${Math.min(index * 75, 300)}ms`);
    });

    root.classList.add("is-motion-ready");

    if (window.matchMedia("(prefers-reduced-motion: reduce)").matches || !("IntersectionObserver" in window)) {
        elements.forEach((element) => element.classList.add("is-visible"));
        return;
    }

    const observer = new IntersectionObserver((entries) => {
        entries.forEach((entry) => {
            if (!entry.isIntersecting) return;
            entry.target.classList.add("is-visible");
            observer.unobserve(entry.target);
        });
    }, {
        threshold: 0.12,
        rootMargin: "0px 0px -7% 0px"
    });

    elements.forEach((element) => observer.observe(element));
})();
