// ------------------------------------------------------------------------------------------------
// <copyright file="ThemeFoundationTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.CodeQuality
{
    using System;
    using System.IO;
    using System.Linq;

    using BlazorBlueprint.Components;

    using Mycelium.Bloom.Tests.Common;

    /// <summary>
    /// Verifies the shared Bloom light/dark token foundation and Blueprint bridge.
    /// </summary>
    [TestFixture]
    public sealed class ThemeFoundationTestFixture
    {
        private static readonly string[] RequiredSemanticTokens =
        [
            "--mb-color-background:",
            "--mb-color-workspace-background:",
            "--mb-color-surface:",
            "--mb-color-surface-elevated:",
            "--mb-color-surface-muted:",
            "--mb-color-popover:",
            "--mb-color-input-background:",
            "--mb-color-input-border:",
            "--mb-color-text-primary:",
            "--mb-color-text-secondary:",
            "--mb-color-text-muted:",
            "--mb-color-text-subtle:",
            "--mb-color-text-disabled:",
            "--mb-color-text-inverted:",
            "--mb-color-border-subtle:",
            "--mb-color-border:",
            "--mb-color-border-strong:",
            "--mb-color-component-border:",
            "--mb-color-action-primary:",
            "--mb-color-action-primary-hover:",
            "--mb-color-action-primary-active:",
            "--mb-color-action-primary-foreground:",
            "--mb-color-action-primary-soft:",
            "--mb-color-surface-selected:",
            "--mb-color-surface-hover:",
            "--mb-color-surface-active:",
            "--mb-color-border-selected:",
            "--mb-color-focus-ring:",
            "--mb-color-success-surface:",
            "--mb-color-success-text:",
            "--mb-color-success-border:",
            "--mb-color-warning-surface:",
            "--mb-color-warning-text:",
            "--mb-color-warning-border:",
            "--mb-color-danger-surface:",
            "--mb-color-danger-text:",
            "--mb-color-danger-border:",
            "--mb-color-danger-action:",
            "--mb-color-danger-action-foreground:",
            "--mb-color-info-surface:",
            "--mb-color-info-text:",
            "--mb-color-info-border:",
            "--mb-color-tooltip-background:",
            "--mb-color-tooltip-foreground:",
            "--mb-color-header-background:",
            "--mb-color-footer-background:",
            "--mb-color-status-background:",
            "--mb-color-panel-background:",
            "--mb-color-canvas-background:",
            "--mb-color-canvas-grid:",
            "--mb-color-toolbar-background:",
            "--mb-color-model-tree-hover:",
            "--mb-color-model-tree-selected:",
            "--mb-color-detail-section-background:",
            "--mb-color-overlay-scrim:",
            "--mb-shadow-lg:"
        ];

        /// <summary>
        /// Verifies the light root and dark override each define every required semantic concept.
        /// </summary>
        [Test]
        public void VerifyLightAndDarkSemanticTokensExist()
        {
            var variables = File.ReadAllText(GetProjectFile("Styles", "variables.css"));
            var darkStart = variables.IndexOf("[data-theme=\"dark\"]", StringComparison.Ordinal);
            var lightSource = variables[..darkStart];
            var darkSource = variables[darkStart..];

            using (Assert.EnterMultipleScope())
            {
                Assert.That(darkStart, Is.GreaterThan(0));
                Assert.That(lightSource, Does.Contain("color-scheme: light"));
                Assert.That(darkSource, Does.Contain("color-scheme: dark"));
                Assert.That(darkSource, Does.Contain(":root.dark"));
                Assert.That(RequiredSemanticTokens.All(lightSource.Contains), Is.True, "A required light token is missing.");
                Assert.That(RequiredSemanticTokens.All(darkSource.Contains), Is.True, "A required dark token is missing.");
            }
        }

        /// <summary>
        /// Verifies Blueprint semantic aliases derive from Bloom tokens rather than copied color literals.
        /// </summary>
        [Test]
        public void VerifyBlueprintThemeBridgeUsesBloomTokens()
        {
            var bridge = File.ReadAllText(GetProjectFile("Styles", "blueprint-theme.css"));
            var expectedAliases = new[]
            {
                "--background:",
                "--foreground:",
                "--card:",
                "--card-foreground:",
                "--popover:",
                "--popover-foreground:",
                "--primary:",
                "--primary-foreground:",
                "--secondary:",
                "--secondary-foreground:",
                "--muted:",
                "--muted-foreground:",
                "--accent:",
                "--accent-foreground:",
                "--destructive:",
                "--destructive-foreground:",
                "--border:",
                "--input:",
                "--ring:",
                "--alert-success:",
                "--alert-success-foreground:",
                "--alert-success-bg:",
                "--alert-info:",
                "--alert-info-foreground:",
                "--alert-info-bg:",
                "--alert-warning:",
                "--alert-warning-foreground:",
                "--alert-warning-bg:",
                "--alert-danger:",
                "--alert-danger-foreground:",
                "--alert-danger-bg:",
                "--sidebar:",
                "--sidebar-foreground:",
                "--sidebar-primary:",
                "--sidebar-primary-foreground:",
                "--sidebar-accent:",
                "--sidebar-accent-foreground:",
                "--sidebar-border:",
                "--sidebar-ring:",
                "--radius:",
                "--font-sans:",
                "--font-mono:"
            };

            using (Assert.EnterMultipleScope())
            {
                Assert.That(expectedAliases.All(bridge.Contains), Is.True);
                Assert.That(bridge, Does.Not.Contain("#"));
                Assert.That(bridge, Does.Not.Contain("rgb("));
                Assert.That(bridge, Does.Contain("var(--mb-"));
            }
        }

        /// <summary>
        /// Verifies the corrective dark palette retains its near-black foundation and accessible action colors.
        /// </summary>
        [Test]
        public void VerifyDarkPaletteUsesNearBlackFoundation()
        {
            var variables = File.ReadAllText(GetProjectFile("Styles", "variables.css"));
            var darkStart = variables.IndexOf("[data-theme=\"dark\"]", StringComparison.Ordinal);
            var darkSource = variables[darkStart..];

            using (Assert.EnterMultipleScope())
            {
                Assert.That(darkSource, Does.Contain("--mb-color-background: #0d1117;"));
                Assert.That(darkSource, Does.Contain("--mb-color-surface: #161b22;"));
                Assert.That(darkSource, Does.Contain("--mb-color-surface-muted: #21262d;"));
                Assert.That(darkSource, Does.Contain("--mb-color-border-subtle: #21262d;"));
                Assert.That(darkSource, Does.Contain("--mb-color-border: #30363d;"));
                Assert.That(darkSource, Does.Contain("--mb-color-border-strong: #484f58;"));
                Assert.That(darkSource, Does.Contain("--mb-color-action-primary: var(--mb-color-brand-400);"));
                Assert.That(darkSource, Does.Contain("--mb-color-danger-action: #ef4444;"));
                Assert.That(darkSource, Does.Contain("--mb-color-danger-action-foreground: #0d1117;"));
            }
        }

        /// <summary>
        /// Verifies theme sources and runtime styles load in their deliberate cascade order.
        /// </summary>
        [Test]
        public void VerifyStylesheetOrderIsDeliberate()
        {
            var tailwind = File.ReadAllText(GetProjectFile("Styles", "tailwind.css"));
            var app = File.ReadAllText(GetProjectFile("Components", "App.razor"));

            var variablesIndex = tailwind.IndexOf("@import \"./variables.css\";", StringComparison.Ordinal);
            var bridgeIndex = tailwind.IndexOf("@import \"./blueprint-theme.css\";", StringComparison.Ordinal);
            var overlaysIndex = tailwind.IndexOf("@import \"./blueprint-overlays.css\";", StringComparison.Ordinal);
            var generatedAppIndex = app.IndexOf("css/app.css", StringComparison.Ordinal);
            var primitivesIndex = app.IndexOf("BlazorBlueprint.Primitives/css/primitives.css", StringComparison.Ordinal);
            var componentsIndex = app.IndexOf("BlazorBlueprint.Components/blazorblueprint.css", StringComparison.Ordinal);
            var scopedStylesIndex = app.IndexOf("Mycelium.Bloom.styles.css", StringComparison.Ordinal);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(variablesIndex, Is.GreaterThanOrEqualTo(0));
                Assert.That(bridgeIndex, Is.GreaterThan(variablesIndex));
                Assert.That(overlaysIndex, Is.GreaterThan(bridgeIndex));
                Assert.That(primitivesIndex, Is.GreaterThan(generatedAppIndex));
                Assert.That(componentsIndex, Is.GreaterThan(primitivesIndex));
                Assert.That(scopedStylesIndex, Is.GreaterThan(componentsIndex));
            }
        }

        /// <summary>
        /// Verifies the official styled package exposes each representative Phase 1 component family.
        /// </summary>
        [Test]
        public void VerifyRepresentativeStyledComponentsAreAvailable()
        {
            var representativeTypes = new[]
            {
                typeof(BbButton),
                typeof(BbInput),
                typeof(BbSelect<>),
                typeof(BbDropdownMenu),
                typeof(BbDialog),
                typeof(BbTooltip),
                typeof(BbTabs),
                typeof(BbSwitch)
            };

            Assert.That(
                representativeTypes.All(type =>
                    string.Equals(
                        type.Assembly.GetName().Name,
                        "BlazorBlueprint.Components",
                        StringComparison.Ordinal)),
                Is.True);
        }

        /// <summary>
        /// Verifies the application uses the official Components registration and minimal package graph.
        /// </summary>
        [Test]
        public void VerifyBlueprintComponentsFoundationRegistration()
        {
            var program = File.ReadAllText(GetProjectFile("Program.cs"));
            var project = File.ReadAllText(GetProjectFile("Mycelium.Bloom.csproj"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(program, Does.Contain("using BlazorBlueprint.Components;"));
                Assert.That(program, Does.Contain("AddBlazorBlueprintComponents();"));
                Assert.That(program, Does.Not.Contain("AddBlazorBlueprintPrimitives();"));
                Assert.That(project, Does.Contain("BlazorBlueprint.Components\" Version=\"3.14.1\""));
                Assert.That(project, Does.Not.Contain("<PackageReference Include=\"BlazorBlueprint.Primitives\""));
                Assert.That(project, Does.Contain("HtmlSanitizer\" Version=\"9.1.973\""));
                Assert.That(project, Does.Contain("must be reassessed when Blueprint is upgraded"));
            }
        }

        /// <summary>
        /// Verifies the exact Blueprint attribution is shipped with application output.
        /// </summary>
        [Test]
        public void VerifyBlueprintNoticeIsDistributed()
        {
            var notice = File.ReadAllText(Path.Combine(TestRepository.GetRootPath(), "NOTICE"));
            var project = File.ReadAllText(GetProjectFile("Mycelium.Bloom.csproj"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(notice, Does.Contain("Blazor Blueprint"));
                Assert.That(notice, Does.Contain("Copyright 2025-present Mathew Taylor"));
                Assert.That(notice, Does.Contain("Original project: https://github.com/blazorblueprintui/ui"));
                Assert.That(project, Does.Contain("<Content Include=\"..\\NOTICE\" Link=\"NOTICE\">"));
                Assert.That(project, Does.Contain("<CopyToPublishDirectory>PreserveNewest</CopyToPublishDirectory>"));
            }
        }

        /// <summary>
        /// Verifies the page preview applies both theme conventions to the document root for portalled content.
        /// </summary>
        [Test]
        public void VerifyThemePreviewTargetsDocumentRoot()
        {
            var module = File.ReadAllText(GetProjectFile("Components", "Pages", "DesignSystem.razor.js"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(module, Does.Contain("document.documentElement"));
                Assert.That(module, Does.Contain("root.setAttribute(\"data-theme\", themeName)"));
                Assert.That(module, Does.Contain("root.classList.toggle(\"dark\""));
                Assert.That(module, Does.Contain("releaseTheme"));
            }
        }

        private static string GetProjectFile(params string[] pathSegments)
        {
            return Path.Combine(
                TestRepository.GetRootPath(),
                "Mycelium.Bloom",
                Path.Combine(pathSegments));
        }

    }
}
