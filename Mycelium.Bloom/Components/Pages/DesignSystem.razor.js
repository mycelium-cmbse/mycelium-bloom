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
        previouslyHadTheme = Object.hasOwn(root.dataset, "theme");
        previousTheme = root.dataset.theme;
        previousDarkClass = root.classList.contains("dark");
        activeOwnerId = ownerId;
    }

    root.dataset.theme = themeName;
    root.classList.toggle("dark", themeName === "dark");
    root.dataset.mbDesignSystemThemeOwner = ownerId;
}

export function releaseTheme(ownerId) {
    if (activeOwnerId !== ownerId) {
        return;
    }

    const root = document.documentElement;

    if (previouslyHadTheme) {
        root.dataset.theme = previousTheme;
    } else {
        delete root.dataset.theme;
    }

    root.classList.toggle("dark", previousDarkClass === true);
    delete root.dataset.mbDesignSystemThemeOwner;

    activeOwnerId = undefined;
    previousTheme = undefined;
    previouslyHadTheme = undefined;
    previousDarkClass = undefined;
}
