const api =
  "https://api.github.com/repos/linux-cmd/personal-navigator/releases?per_page=100";
const cacheKey = "personal-navigator-release-cache-v2";

function installer(release) {
  return release.assets?.find((asset) =>
    /^PersonalNavigator-Setup-[0-9].*-win-x64\.exe$/i.test(asset.name),
  );
}

function applyReleases(releases) {
  const published = releases.filter((release) => !release.draft);
  const stable = published.find(
    (release) => !release.prerelease && installer(release),
  );
  if (!stable) throw new Error("No stable installer release is available.");
  const asset = installer(stable);
  const version = stable.tag_name.replace(/^v/, "");
  const date = new Date(stable.published_at).toLocaleDateString(undefined, {
    year: "numeric",
    month: "long",
    day: "numeric",
  });
  const size = `${(asset.size / 1024 / 1024).toFixed(1)} MB`;
  document
    .querySelectorAll("#release-version,[data-release-version]")
    .forEach((node) => (node.textContent = `Version ${version}`));
  document
    .querySelectorAll("#release-size,[data-release-size]")
    .forEach((node) => (node.textContent = size));
  document
    .querySelectorAll("[data-release-date]")
    .forEach((node) => (node.textContent = date));
  document
    .querySelectorAll("[data-release-file]")
    .forEach((node) => (node.textContent = asset.name));
  document
    .querySelectorAll("[data-release-checksum]")
    .forEach(
      (node) =>
        (node.textContent = (asset.digest || "See SHA256SUMS.txt").replace(
          /^sha256:/,
          "",
        )),
    );
  document
    .querySelectorAll("[data-latest-download]")
    .forEach((link) => (link.href = asset.browser_download_url));
  document
    .querySelectorAll("[data-download-label]")
    .forEach(
      (label) => (label.textContent = "Download Windows installer (.exe)"),
    );
  document
    .querySelectorAll("[data-download-note]")
    .forEach(
      (label) =>
        (label.textContent =
          "Setup wizard available. No ZIP extraction or command line required."),
    );

  const history = document.querySelector("[data-release-history]");
  if (history)
    history.innerHTML = published
      .map((release) => {
        const setup = installer(release);
        const kind = release.prerelease ? "Prerelease" : "Stable";
        const notes = (release.body || "No release notes supplied.")
          .replace(/</g, "&lt;")
          .replace(/>/g, "&gt;");
        const download = setup
          ? `<a class="button button-primary" href="${setup.browser_download_url}">Download ${setup.name}</a>`
          : "<span>No setup installer was published for this version.</span>";
        return `<article class="panel release-card"><p>${kind} · ${new Date(release.published_at).toLocaleDateString()}</p><h2>${release.tag_name}</h2><pre>${notes}</pre><p>${download} <a href="${release.html_url}">GitHub Release</a></p></article>`;
      })
      .join("");
}

fetch(api, { headers: { Accept: "application/vnd.github+json" } })
  .then((response) =>
    response.ok
      ? response.json()
      : Promise.reject(new Error(`GitHub returned ${response.status}`)),
  )
  .then((releases) => {
    localStorage.setItem(cacheKey, JSON.stringify(releases));
    applyReleases(releases);
  })
  .catch(() => {
    const cached = localStorage.getItem(cacheKey);
    if (cached) applyReleases(JSON.parse(cached));
    else
      document
        .querySelectorAll("[data-download-note]")
        .forEach(
          (node) =>
            (node.textContent =
              "Release information is temporarily unavailable. Open GitHub Releases below."),
        );
  });

if (
  "IntersectionObserver" in window &&
  !window.matchMedia("(prefers-reduced-motion: reduce)").matches
) {
  const observer = new IntersectionObserver(
    (entries) =>
      entries.forEach((entry) => {
        if (entry.isIntersecting) {
          entry.target.classList.add("visible");
          observer.unobserve(entry.target);
        }
      }),
    { threshold: 0.12 },
  );
  document
    .querySelectorAll(".reveal")
    .forEach((element) => observer.observe(element));
} else
  document
    .querySelectorAll(".reveal")
    .forEach((element) => element.classList.add("visible"));
