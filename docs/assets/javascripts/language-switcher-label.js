(() => {
  const LANGUAGE_NAMES = {
    en: "English",
    ja: "日本語",
  };

  const LABELS = {
    en: "Language",
    ja: "言語",
  };

  function applyLanguageLabel() {
    const lang = (document.documentElement.lang || "en").slice(0, 2).toLowerCase();
    const currentLanguageName = LANGUAGE_NAMES[lang] || lang.toUpperCase();
    const button = document.querySelector(".md-header__option .md-select > .md-header__button.md-icon");

    if (!button) {
      return;
    }

    let label = button.querySelector(".md-header__language-label");
    if (!label) {
      label = document.createElement("span");
      label.className = "md-header__language-label";
      button.appendChild(label);
    }

    label.textContent = currentLanguageName;

    const labelPrefix = LABELS[lang] || LABELS.en;
    const accessibleLabel = `${labelPrefix}: ${currentLanguageName}`;
    button.setAttribute("aria-label", accessibleLabel);
    button.setAttribute("title", accessibleLabel);
  }

  if (window.document$ && typeof window.document$.subscribe === "function") {
    window.document$.subscribe(applyLanguageLabel);
  } else {
    document.addEventListener("DOMContentLoaded", applyLanguageLabel);
  }
})();
