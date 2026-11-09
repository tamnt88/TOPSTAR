(() => {
  const items = document.querySelectorAll('.reveal-up');
  if (!('IntersectionObserver' in window)) return items.forEach(x => x.classList.add('is-visible'));
  const observer = new IntersectionObserver(entries => entries.forEach(e => { if (e.isIntersecting) { e.target.classList.add('is-visible'); observer.unobserve(e.target); } }), { threshold: .12 });
  items.forEach(x => observer.observe(x));
})();
