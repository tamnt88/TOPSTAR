(function ($) {
  $(function () {
    $(document).on('click', '.admin-tab-toggle', function (e) {
      var $toggle = $(this);
      if ($toggle.is(':disabled') || $toggle.hasClass('disabled')) return;
      var $menu = $toggle.next('.admin-tab-menu');
      if (!$menu.length) return;

      e.preventDefault();
      e.stopPropagation();

      $('.admin-tab-menu.show').not($menu).removeClass('show');
      $('.admin-tab-toggle[aria-expanded="true"]').not($toggle).attr('aria-expanded', 'false');

      $menu.toggleClass('show');
      $toggle.attr('aria-expanded', $menu.hasClass('show') ? 'true' : 'false');
    });

    $(document).on('click', '.admin-tab-menu', function (e) {
      e.stopPropagation();
    });

    $(document).on('click', function () {
      $('.admin-tab-menu.show').removeClass('show');
      $('.admin-tab-toggle[aria-expanded="true"]').attr('aria-expanded', 'false');
    });
  });
})(jQuery);
