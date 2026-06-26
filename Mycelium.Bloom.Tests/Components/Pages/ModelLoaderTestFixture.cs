// ------------------------------------------------------------------------------------------------
// <copyright file="ModelLoaderTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Components.Pages
{
    using System;

    using Bunit;

    using Microsoft.Extensions.DependencyInjection;

    using Moq;

    using Mycelium.Bloom.Components.Pages;
    using Mycelium.Bloom.Core.ModelLoading;

    using SysML2.NET.Core.POCO.Root.Namespaces;

    /// <summary>
    /// Tests the <see cref="ModelLoaderTest" /> page.
    /// </summary>
    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class ModelLoaderTestFixture : BunitContext
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
        /// Verifies that the page displays the successful model loading result.
        /// </summary>
        [Test]
        public void Render_WhenModelLoads_DisplaysSuccessResult()
        {
            var modelLoaderService = new Mock<IModelLoaderService>();
            modelLoaderService.Setup(x => x.LoadQuantitiesModel()).Returns(new Namespace());
            this.Services.AddSingleton(modelLoaderService.Object);

            var component = this.Render<ModelLoaderTest>();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find("h1").TextContent.Trim(), Is.EqualTo("Model Loader Test"));
                Assert.That(component.Markup, Does.Contain("Loaded successfully: SysML2.NET.Core.POCO.Root.Namespaces.Namespace"));
                Assert.That(component.Markup, Does.Contain("Elapsed:"));
            }

            modelLoaderService.Verify(x => x.LoadQuantitiesModel(), Times.Once);
        }

        /// <summary>
        /// Verifies that the page displays the exception when model loading fails.
        /// </summary>
        [Test]
        public void Render_WhenModelLoadFails_DisplaysException()
        {
            var modelLoaderService = new Mock<IModelLoaderService>();
            modelLoaderService.Setup(x => x.LoadQuantitiesModel()).Throws(new InvalidOperationException("Failed to load test model."));
            this.Services.AddSingleton(modelLoaderService.Object);

            var component = this.Render<ModelLoaderTest>();

            Assert.That(component.Markup, Does.Contain("Failed to load test model."));
            modelLoaderService.Verify(x => x.LoadQuantitiesModel(), Times.Once);
        }
    }
}
