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
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text.Json;
    using System.Text.RegularExpressions;

    using BlazorBlueprint.Components;
    using BlazorBlueprint.Icons.Lucide.Data;

    using Mycelium.Bloom.Tests.Common;

    /// <summary>
    /// Verifies the shared Bloom light/dark token foundation and Blueprint bridge.
    /// </summary>
    [TestFixture]
    public sealed class ThemeFoundationTestFixture
    {
        private static readonly string[] RequiredSemanticTokens =
        [
            "--background:",
            "--card:",
            "--popover:",
            "--muted:",
            "--input:",
            "--foreground:",
            "--foreground-muted:",
            "--muted-foreground:",
            "--border-subtle:",
            "--border:",
            "--border-strong:",
            "--primary:",
            "--primary-strong:",
            "--primary-deep:",
            "--primary-foreground:",
            "--accent:",
            "--secondary:",
            "--secondary-foreground:",
            "--ring:",
            "--success-subtle:",
            "--success-strong:",
            "--warning-subtle:",
            "--warning-vivid:",
            "--destructive-subtle:",
            "--destructive-strong:",
            "--destructive:",
            "--destructive-foreground:",
            "--info-subtle:",
            "--info-strong:",
            "--ownership-aocs-base:",
            "--collaborator-c06:",
            "--lifecycle-open:",
            "--font-sans:",
            "--font-mono:",
            "--radius:",
            "--sysml-structure-header:",
            "--sysml-attributes-header:",
            "--sysml-connections-header:",
            "--sysml-behavior-header:",
            "--sysml-requirements-header:",
            "--sysml-verification-header:",
            "--sysml-allocations-header:",
            "--sysml-metadata-header:",
            "--scrim-modal:",
            "--shadow-popover:",
            "--shadow-modal:"
        ];

        private static readonly string[] LightSysmlHeaderTokens =
        [
            "--sysml-structure-header",
            "--sysml-attributes-header",
            "--sysml-connections-header",
            "--sysml-behavior-header",
            "--sysml-requirements-header",
            "--sysml-verification-header",
            "--sysml-allocations-header",
            "--sysml-metadata-header"
        ];

        private static readonly IReadOnlyDictionary<string, string> RequiredPackageVersions =
            new Dictionary<string, string>
            {
                ["AngleSharp"] = "1.7.2",
                ["BlazorBlueprint.Components"] = "3.16.0",
                ["BlazorBlueprint.Icons.Lucide"] = "2.0.2",
                ["BlazorBlueprint.Primitives"] = "3.16.0",
                ["DynamicData"] = "9.4.33",
                ["HtmlSanitizer"] = "9.2.1039",
                ["ReactiveUI.Blazor"] = "24.2.0"
            };

        private static readonly string[] RequiredLucideIconNames =
        [
            "arrow-right",
            "check",
            "copy",
            "ellipsis",
            "eye",
            "file-text",
            "focus",
            "grid-2x2",
            "house",
            "info",
            "link-2",
            "log-out",
            "maximize",
            "menu",
            "minus",
            "mouse-pointer-2",
            "move",
            "panel-right-close",
            "panel-right-open",
            "pencil",
            "plus",
            "scan-line",
            "search",
            "settings-2",
            "share-2",
            "sticky-note",
            "trash-2",
            "undo-2",
            "user",
            "x"
        ];

        [Test]
        public void VerifyLightAndDarkSemanticTokensExist()
        {
            var runtime = File.ReadAllText(GetProjectFile("wwwroot", "css", "tokens.css"));
            var foundation = File.ReadAllText(GetProjectFile("Styles", "variables.css"));
            var darkStart = runtime.IndexOf(".dark {", StringComparison.Ordinal);
            Assert.That(darkStart, Is.GreaterThan(0));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(foundation, Does.Match(@"(?s):root\s*\{[^}]*color-scheme: light;"));
                Assert.That(foundation, Does.Match(@"(?s)\.dark\s*\{[^}]*color-scheme: dark;"));
                foreach (var token in RequiredSemanticTokens)
                {
                    Assert.That(GetTokenValue(runtime[..darkStart], token.TrimEnd(':')), Is.Not.Empty);
                    Assert.That(GetTokenValue(runtime[darkStart..], token.TrimEnd(':')), Is.Not.Empty);
                }
            }
        }

        [Test]
        public void VerifyBlueprintThemeBridgeUsesCanonicalTokens()
        {
            var bridge = File.ReadAllText(GetProjectFile("Styles", "blueprint-theme.css"));
            var runtime = File.ReadAllText(GetProjectFile("wwwroot", "css", "tokens.css"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(bridge, Does.Not.Contain("#"));
                Assert.That(bridge, Does.Contain("--radius: var(--radius-8) !important;"));
                Assert.That(bridge, Does.Contain("background: var(--card);"));
                Assert.That(bridge, Does.Contain("border-color: var(--border);"));
                Assert.That(bridge, Does.Contain("background: var(--secondary);"));
                Assert.That(bridge, Does.Contain("color: var(--secondary-foreground);"));
                Assert.That(runtime, Does.Contain("--secondary: var(--muted);"));
                Assert.That(runtime, Does.Contain("--secondary-foreground: var(--foreground);"));
                Assert.That(runtime, Does.Not.Contain("--secondary: var(--accent)"));
            }
        }

        /// <summary>
        /// Verifies editor drag feedback derives its contrast from shared light/dark semantic tokens.
        /// </summary>
        [Test]
        public void VerifyEditorDragFeedbackUsesSemanticThemeTokens()
        {
            var style = File.ReadAllText(GetProjectFile(
                "Components",
                "UI",
                "Organisms",
                "EditorWorkspace",
                "EditorWorkspace.razor.css"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    style,
                    Does.Match(
                        @"(?s)\.mb-editor-workspace__tab-item--dragging\s*\{[^}]*var\(--primary\)[^}]*var\(--shadow-popover\)"));
                Assert.That(
                    style,
                    Does.Match(
                        @"(?s)\.mb-editor-workspace__group-drop-surface--empty\.mb-editor-workspace__group-drop-surface--active\s*\{[^}]*var\(--accent\)[^}]*var\(--primary\)[^}]*var\(--shadow-popover\)"));
                Assert.That(
                    style,
                    Does.Match(
                        @"(?s)\.mb-editor-workspace__tab-split-drop-target--active\s*\{[^}]*var\(--primary\)[^}]*var\(--shadow-popover\)"));
                Assert.That(
                    style,
                    Does.Match(
                        @"(?s)\.mb-editor-workspace__tab-split-docking-plus\s*\{[^}]*box-shadow:\s*var\(--shadow-popover\)"));
                Assert.That(style, Does.Not.Match("#[0-9a-fA-F]{3,8}"));
                Assert.That(style, Does.Not.Contain("rgb("));
            }
        }

        [Test]
        public void VerifyDarkPaletteUsesNearBlackFoundation()
        {
            var runtime = File.ReadAllText(GetProjectFile("wwwroot", "css", "tokens.css"));
            var darkSource = runtime[runtime.IndexOf(".dark {", StringComparison.Ordinal)..];

            using (Assert.EnterMultipleScope())
            {
                Assert.That(GetRelativeLuminance(GetTokenValue(darkSource, "--background")), Is.LessThan(0.02d));
                Assert.That(GetRelativeLuminance(GetTokenValue(darkSource, "--card")), Is.LessThan(0.03d));
            }
        }

        [Test]
        public void VerifyDarkSysmlHeaderTokensMeetNonTextContrast()
        {
            var runtime = File.ReadAllText(GetProjectFile("wwwroot", "css", "tokens.css"));
            var darkSource = runtime[runtime.IndexOf(".dark {", StringComparison.Ordinal)..];
            var darkSurface = GetTokenValue(darkSource, "--card");

            using (Assert.EnterMultipleScope())
            {
                foreach (var token in LightSysmlHeaderTokens)
                {
                    var value = GetTokenValue(darkSource, token);
                    Assert.That(
                        GetContrastRatio(value, darkSurface),
                        Is.GreaterThanOrEqualTo(3d),
                        $"{token} must retain non-text contrast against the shared card surface.");
                }
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
                Assert.That(primitivesIndex, Is.GreaterThanOrEqualTo(0));
                Assert.That(generatedAppIndex, Is.GreaterThan(componentsIndex));
                var tokensIndex = app.IndexOf("css/tokens.css", StringComparison.Ordinal);
                Assert.That(tokensIndex, Is.GreaterThan(componentsIndex));
                Assert.That(generatedAppIndex, Is.GreaterThan(tokensIndex));
                Assert.That(tailwind, Does.Contain("@import \"./Generated/MyceliumTokens/theme.css\";"));
                Assert.That(componentsIndex, Is.GreaterThan(primitivesIndex));
                Assert.That(scopedStylesIndex, Is.GreaterThan(componentsIndex));
            }
        }

        /// <summary>
        /// Verifies the official styled package exposes each currently used representative component family.
        /// </summary>
        [Test]
        public void VerifyRepresentativeStyledComponentsAreAvailable()
        {
            var representativeTypes = new[]
            {
                typeof(BbButton),
                typeof(BbInputGroupInput),
                typeof(BbSelect<>),
                typeof(BbDropdownMenu),
                typeof(BbDialog),
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
        /// Verifies every Lucide icon name used directly or by a valid Bloom symbol remains available.
        /// </summary>
        [Test]
        public void VerifyRequiredLucideIconsAreAvailable()
        {
            Assert.That(
                RequiredLucideIconNames.Where(iconName => !LucideIconData.IconExists(iconName)),
                Is.Empty);
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
                Assert.That(
                    program,
                    Does.Match(
                        @"AddBlazorBlueprintComponents\(\s*configureTheme:\s*options\s*=>\s*\{\s*options\.DetectSystemPreference\s*=\s*false;\s*\}\)\s*\.AddApplicationServices\(\);"));
                Assert.That(program, Does.Not.Contain("AddBlazorBlueprintPrimitives();"));
                Assert.That(project, Does.Contain("BlazorBlueprint.Components\" Version=\"3.16.0\""));
                Assert.That(project, Does.Contain("BlazorBlueprint.Icons.Lucide\" Version=\"2.0.2\""));
                Assert.That(project, Does.Not.Contain("<PackageReference Include=\"BlazorBlueprint.Primitives\""));
                Assert.That(project, Does.Contain("HtmlSanitizer\" Version=\"9.2.1039\""));
                Assert.That(project, Does.Contain("must be reassessed when either dependency is upgraded"));
            }
        }

        /// <summary>
        /// Verifies the restored application graph keeps the approved direct and transitive package versions.
        /// </summary>
        [Test]
        public void VerifyResolvedDependencyGraphMatchesApprovedVersions()
        {
            using var assets = JsonDocument.Parse(File.ReadAllText(GetProjectFile("obj", "project.assets.json")));
            var libraries = assets.RootElement.GetProperty("libraries")
                .EnumerateObject()
                .Select(library => library.Name)
                .ToArray();

            using (Assert.EnterMultipleScope())
            {
                foreach (var requiredPackage in RequiredPackageVersions)
                {
                    var resolvedVersions = libraries
                        .Where(library => library.StartsWith($"{requiredPackage.Key}/", StringComparison.OrdinalIgnoreCase))
                        .Select(library => library[(library.IndexOf('/') + 1)..])
                        .ToArray();

                    Assert.That(
                        resolvedVersions,
                        Has.Length.EqualTo(1),
                        $"{requiredPackage.Key} must resolve exactly once.");
                    Assert.That(
                        resolvedVersions.ElementAtOrDefault(0),
                        Is.EqualTo(requiredPackage.Value),
                        $"{requiredPackage.Key} must resolve at the approved version.");
                }

                Assert.That(
                    libraries,
                    Has.None.Matches<string>(library =>
                        library.StartsWith("BlazorBlueprint.", StringComparison.OrdinalIgnoreCase)
                        && library.EndsWith("/3.14.1", StringComparison.Ordinal)));
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
        /// Verifies DesignSystem and workspace composition use Blueprint's single application theme authority.
        /// </summary>
        [Test]
        public void VerifyThemeRuntimeUsesBlueprintAuthority()
        {
            var program = File.ReadAllText(GetProjectFile("Program.cs"));
            var designSystem = File.ReadAllText(GetProjectFile("Components", "Pages", "DesignSystem.razor.cs"));
            var obsoleteModule = GetProjectFile("Components", "Pages", "DesignSystem.razor.js");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(program, Does.Contain("options.DetectSystemPreference = false"));
                Assert.That(designSystem, Does.Contain("private ThemeService ThemeService"));
                Assert.That(designSystem, Does.Contain("this.ThemeService.SetDarkModeAsync"));
                Assert.That(designSystem, Does.Not.Contain("IJSRuntime"));
                Assert.That(designSystem, Does.Not.Contain("DesignSystem.razor.js"));
                Assert.That(File.Exists(obsoleteModule), Is.False);
            }
        }

        private static string GetProjectFile(params string[] pathSegments)
        {
            return Path.Combine(
                TestRepository.GetRootPath(),
                "Mycelium.Bloom",
                Path.Combine(pathSegments));
        }


        private static string GetTokenValue(string source, string tokenName)
        {
            var tokens = Regex.Matches(source, @"(--[\w-]+)\s*:\s*([^;]+);")
                .ToDictionary(match => match.Groups[1].Value, match => match.Groups[2].Value.Trim());
            var visited = new HashSet<string>(StringComparer.Ordinal);
            var value = tokens[tokenName];

            while (value.StartsWith("var(", StringComparison.Ordinal))
            {
                if (!visited.Add(tokenName))
                {
                    throw new InvalidOperationException($"A theme alias cycle contains '{tokenName}'.");
                }
                tokenName = value[4..^1];
                value = tokens[tokenName];
            }

            return value;
        }

        private static double GetContrastRatio(string foreground, string background)
        {
            var foregroundLuminance = GetRelativeLuminance(foreground);
            var backgroundLuminance = GetRelativeLuminance(background);
            var lighter = Math.Max(foregroundLuminance, backgroundLuminance);
            var darker = Math.Min(foregroundLuminance, backgroundLuminance);

            return (lighter + 0.05d) / (darker + 0.05d);
        }


        private static double GetRelativeLuminance(string color)
        {
            var match = Regex.Match(color, @"^color\(srgb ([\d.]+) ([\d.]+) ([\d.]+) / [\d.]+\)$");
            Assert.That(match.Success, Is.True, "Expected a resolved shared sRGB color.");

            double LinearChannel(int index)
            {
                var channel = double.Parse(match.Groups[index].Value, CultureInfo.InvariantCulture);
                return channel <= 0.04045d
                    ? channel / 12.92d
                    : Math.Pow((channel + 0.055d) / 1.055d, 2.4d);
            }

            return (0.2126d * LinearChannel(1)) + (0.7152d * LinearChannel(2)) + (0.0722d * LinearChannel(3));
        }
    }
}
