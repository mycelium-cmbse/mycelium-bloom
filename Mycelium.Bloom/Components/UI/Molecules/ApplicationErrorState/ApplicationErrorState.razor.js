export async function initializeErrorRecovery(
    root = document,
    browser = window,
    loadThemeModule = () => import(new URL("_content/BlazorBlueprint.Components/js/theme.js", root.baseURI).href)
) {
    root.addEventListener("click", event => {
        if (!(event.target instanceof browser.Element)) {
            return;
        }

        const action = event.target.closest(".application-error-state [data-error-action]")?.dataset.errorAction;

        if (action === "back") {
            browser.history.back();
        } else if (action === "reload") {
            browser.location.reload();
        }
    });

    if (root.querySelector(".application-error-state")) {
        const theme = await loadThemeModule();
        const saved = theme.loadTheme();

        if (saved) {
            theme.applyDarkMode(saved.isDarkMode);
        }
    }
}

await initializeErrorRecovery();
