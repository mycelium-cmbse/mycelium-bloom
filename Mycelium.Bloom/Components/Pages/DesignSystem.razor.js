let activeOwnerId;
let previousTheme;
let previouslyHadTheme;
let previousDarkClass;

export function applyTheme(ownerId, themeName) {
    if (themeName !== "light" && themeName !== "dark") {
        throw new RangeError(`Unsupported Bloom preview theme: ${themeName}`);
    }

    const root = document.documentElement;

    if (activeOwnerId !== ownerId) {
        previouslyHadTheme = root.hasAttribute("data-theme");
        previousTheme = root.getAttribute("data-theme");
        previousDarkClass = root.classList.contains("dark");
        activeOwnerId = ownerId;
    }

    root.setAttribute("data-theme", themeName);
    root.classList.toggle("dark", themeName === "dark");
    root.setAttribute("data-mb-design-system-theme-owner", ownerId);
}

export function releaseTheme(ownerId) {
    if (activeOwnerId !== ownerId) {
        return;
    }

    const root = document.documentElement;

    if (previouslyHadTheme && previousTheme) {
        root.setAttribute("data-theme", previousTheme);
    } else {
        root.removeAttribute("data-theme");
    }

    root.classList.toggle("dark", previousDarkClass === true);
    root.removeAttribute("data-mb-design-system-theme-owner");

    activeOwnerId = undefined;
    previousTheme = undefined;
    previouslyHadTheme = undefined;
    previousDarkClass = undefined;
}
