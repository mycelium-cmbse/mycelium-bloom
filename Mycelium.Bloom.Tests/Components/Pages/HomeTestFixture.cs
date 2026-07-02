// ------------------------------------------------------------------------------------------------
// <copyright file="HomeTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Components.Pages
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Bunit;

    using Microsoft.Extensions.DependencyInjection;

    using Moq;

    using Mycelium.Bloom.Components.Pages;
    using Mycelium.Bloom.ViewModel.ProjectBrowser;

    using SysML2.NET.Core.POCO.Root.Namespaces;

    /// <summary>
    /// Tests the <see cref="Home" /> page.
    /// </summary>
    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class HomeTestFixture : BunitContext
    {
        /// <summary>
        /// Disposes the bUnit test context after each test.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            this.Dispose();
        }

        /// <summary>
        /// Verifies that the home page displays the expected workspace content.
        /// </summary>
        [Test]
        public void Render_DisplaysHomeContent()
        {
            object shortcutOptions = null;

            var projectBrowserService = this.RegisterProjectBrowserServices();

            var module = this.JSInterop.SetupModule("./Components/UI/Atoms/SearchInput/SearchInput.razor.js");
            var registerHandler = module.SetupVoid(
                "registerSearchShortcut",
                invocation =>
                {
                    if (invocation.Arguments.Count != 2 || !Equals(invocation.Arguments[0], "global-search"))
                    {
                        return false;
                    }

                    shortcutOptions = invocation.Arguments[1];

                    return true;
                });

            var disposeHandler = module.SetupVoid("disposeSearchShortcut");

            registerHandler.SetVoidResult();
            disposeHandler.SetVoidResult();

            var keyboardNavigationModule = this.JSInterop.SetupModule("/js/keyboardNavigation.js");

            keyboardNavigationModule.SetupVoid("registerNavigationKeyPrevention", _ => true).SetVoidResult();
            keyboardNavigationModule.SetupVoid("disposeNavigationKeyPrevention", _ => true).SetVoidResult();

            var component = this.Render<Home>();

            component.Find("input").Input("model");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Markup, Does.Contain("Quantities model workspace"));
                Assert.That(component.Markup, Does.Contain("Quantities"));
                Assert.That(component.Markup, Does.Contain("Loading Quantities model"));
                Assert.That(component.Markup, Does.Contain("Workspace in progress"));
                Assert.That(component.Markup, Does.Contain("Details in progress"));
                Assert.That(component.Find(".mb-project-browser"), Is.Not.Null);
                Assert.That(component.Find(".mb-project-browser__tree"), Is.Not.Null);
                Assert.That(component.Find("input").GetAttribute("id"), Is.EqualTo("global-search"));
                Assert.That(registerHandler.Invocations, Has.Count.EqualTo(1));
                Assert.That(GetPropertyValue(shortcutOptions, "key"), Is.EqualTo("k"));
                Assert.That(GetPropertyValue(shortcutOptions, "requiresControlOrMeta"), Is.True);
                Assert.That(GetPropertyValue(shortcutOptions, "requiresAlt"), Is.False);
                Assert.That(GetPropertyValue(shortcutOptions, "requiresShift"), Is.False);
                projectBrowserService.Verify(x => x.CreateQuantitiesProjectBrowserViewModel(), Times.Once);
            }
        }

        /// <summary>
        /// Gets a property value from an object passed to JavaScript interop.
        /// </summary>
        /// <param name="instance">The object instance.</param>
        /// <param name="propertyName">The property name.</param>
        /// <returns>The property value.</returns>
        private static object GetPropertyValue(object instance, string propertyName)
        {
            var property = instance.GetType().GetProperty(propertyName);

            Assert.That(property, Is.Not.Null);

            return property.GetValue(instance);
        }

        private Mock<IProjectBrowserViewModelService> RegisterProjectBrowserServices()
        {
            var viewModel = new ProjectBrowserViewModelStub
            {
                IsLoading = true
            };

            var projectBrowserService = new Mock<IProjectBrowserViewModelService>();

            projectBrowserService
                .Setup(x => x.CreateQuantitiesProjectBrowserViewModel())
                .Returns(viewModel);

            this.Services.AddSingleton(projectBrowserService.Object);

            return projectBrowserService;
        }

        private sealed class ProjectBrowserViewModelStub : IProjectBrowserViewModel
        {
            public IReadOnlyList<ProjectBrowserNodeViewModel> RootNodes { get; } = [];

            public ProjectBrowserNodeViewModel SelectedNode { get; private set; }

            public bool IsLoading { get; set; }

            public bool IsLoaded { get; set; }

            public string ErrorMessage { get; set; } = string.Empty;

            public Task InitializeAsync()
            {
                return Task.CompletedTask;
            }

            public void Initialize(INamespace model)
            {
                this.IsLoaded = true;
            }

            public void ToggleNode(ProjectBrowserNodeViewModel node)
            {
            }

            public void SelectNode(ProjectBrowserNodeViewModel node)
            {
                this.SelectedNode = node;
            }
        }
    }
}
