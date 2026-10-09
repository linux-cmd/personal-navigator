const releaseApi = 'https://api.github.com/repos/linux-cmd/personal-navigator/releases/latest';

fetch(releaseApi, { headers: { Accept: 'application/vnd.github+json' } })
  .then((response) => response.ok ? response.json() : Promise.reject())
  .then((release) => {
    const version = document.querySelector('#release-version');
    if (version && release.tag_name) version.textContent = `Version ${release.tag_name.replace(/^v/, '')}`;

    const asset = release.assets?.find((item) => item.name === 'PersonalNavigator-win-x64.zip');
    const size = document.querySelector('#release-size');
    if (size && asset?.size) size.textContent = `(${Math.ceil(asset.size / 1024 / 1024)} MB)`;
  })
  .catch(() => {});

const observer = new IntersectionObserver((entries) => {
  for (const entry of entries) {
    if (entry.isIntersecting) {
      entry.target.classList.add('visible');
      observer.unobserve(entry.target);
    }
  }
}, { threshold: 0.12 });

document.querySelectorAll('.reveal').forEach((element) => observer.observe(element));
