(function ($) {
  $(function () {
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
  });
})(jQuery);
