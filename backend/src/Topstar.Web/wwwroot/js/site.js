(function ($) {
  $(function () {
    function animateCounter(el) {
      if (!el || el.dataset.counted === '1') {
        return;
      }

      var target = parseInt(el.dataset.target || '0', 10);
      if (isNaN(target) || target <= 0) {
        return;
      }

      var suffix = el.dataset.suffix || '';
      var duration = 1600;
      var startTime = null;
      el.dataset.counted = '1';

      function tick(timestamp) {
        if (!startTime) {
          startTime = timestamp;
        }

        var progress = Math.min((timestamp - startTime) / duration, 1);
        var easedProgress = 1 - Math.pow(1 - progress, 3);
        var currentValue = Math.round(target * easedProgress);
        el.textContent = currentValue.toLocaleString('vi-VN') + suffix;

        if (progress < 1) {
          window.requestAnimationFrame(tick);
        }
      }

      window.requestAnimationFrame(tick);
    }

    if (typeof Swiper !== 'undefined' && $('.hero-banner-swiper').length) {
      new Swiper('.hero-banner-swiper', {
        slidesPerView: 1,
        loop: true,
        speed: 700,
        autoplay: {
          delay: 3000,
          disableOnInteraction: false
        },
        pagination: {
          el: '.hero-banner-pagination',
          clickable: true
        }
      });
    }

    if (typeof Swiper !== 'undefined' && $('.creator-swiper').length) {
      new Swiper('.creator-swiper', {
        slidesPerView: 1,
        spaceBetween: 16,
        pagination: {
          el: '.creator-swiper .swiper-pagination',
          clickable: true
        },
        breakpoints: {
          768: { slidesPerView: 2 },
          1200: { slidesPerView: 3 }
        }
      });
    }

    var revealEls = document.querySelectorAll('.reveal-up');
    if (revealEls.length) {
      var io = new IntersectionObserver(function (entries) {
        entries.forEach(function (entry) {
          if (entry.isIntersecting) {
            entry.target.classList.add('in-view');

            var flowItems = entry.target.querySelectorAll('.flow-item');
            flowItems.forEach(function (item, idx) {
              if (!item.dataset.flowDelay) {
                item.style.transitionDelay = (idx * 0.08 + 0.08).toFixed(2) + 's';
                item.dataset.flowDelay = '1';
              }
            });

            var counters = entry.target.querySelectorAll('.counter-value');
            counters.forEach(function (counterEl) {
              animateCounter(counterEl);
            });
          }
        });
      }, { threshold: 0.2 });

      revealEls.forEach(function (el) {
        io.observe(el);
      });
    }
  });
})(jQuery);
