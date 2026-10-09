const releaseApi = "https://api.github.com/repos/linux-cmd/personal-navigator/releases/latest";
const installerName = "PersonalNavigator-Setup-win-x64.exe";
const zipName = "PersonalNavigator-win-x64.zip";
const releaseFallback = "https://github.com/linux-cmd/personal-navigator/releases/latest";
const legacyUrl = "https://github.com/linux-cmd/personal-navigator/releases/latest/download/" + zipName;

fetch(releaseApi, { headers: { Accept: "application/vnd.github+json" } })
  .then((response) => response.ok ? response.json() : Promise.reject(new Error("Release unavailable")))
  .then((release) => {
    const version = document.querySelector("#release-version");
    if (version && release.tag_name) version.textContent = "Version " + release.tag_name.replace(/^v/, "");
    const setup = release.assets?.find((item) => item.name === installerName);
    const zip = release.assets?.find((item) => item.name === zipName);
    const selected = setup || zip;
    const size = document.querySelector("#release-size");
    if (size && selected?.size) size.textContent = "(" + Math.ceil(selected.size / 1024 / 1024) + " MB)";
    for (const link of document.querySelectorAll("[data-latest-download]")) {
      if (selected?.browser_download_url) link.href = selected.browser_download_url;
      else link.href = releaseFallback;
      const label = link.matches("[data-download-label]") ? link : link.querySelector("[data-download-label]");
      if (label) label.textContent = setup ? "Download Windows installer (.exe)" : "Download Windows ZIP (.zip)";
    }
    for (const label of document.querySelectorAll("[data-download-note]")) {
      label.textContent = setup ? "Setup wizard available. No ZIP extraction needed." : "This release requires extracting the ZIP and running the included PowerShell installer.";
    }
  })
  .catch(() => {
    for (const link of document.querySelectorAll("[data-latest-download]")) link.href = legacyUrl;
    for (const label of document.querySelectorAll("[data-download-note]")) label.textContent = "Check GitHub Releases for the available package type.";
  });

if ("IntersectionObserver" in window && !window.matchMedia("(prefers-reduced-motion: reduce)").matches) {
  const observer = new IntersectionObserver((entries) => {
    for (const entry of entries) if (entry.isIntersecting) {
      entry.target.classList.add("visible");
      observer.unobserve(entry.target);
    }
  }, { threshold: 0.12 });
  document.querySelectorAll(".reveal").forEach((element) => observer.observe(element));
} else {
  document.querySelectorAll(".reveal").forEach((element) => element.classList.add("visible"));
}
