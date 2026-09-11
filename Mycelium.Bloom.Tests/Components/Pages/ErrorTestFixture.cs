// ------------------------------------------------------------------------------------------------
// <copyright file="ErrorTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Components.Pages
{
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;

    using Bunit;

    using Microsoft.AspNetCore.Http;

    using Mycelium.Bloom.Components.Pages;
    using Mycelium.Bloom.Model.Enum;
    using Mycelium.Bloom.Tests.Common;

    using ErrorStateComponent = Mycelium.Bloom.Components.UI.Molecules.ApplicationErrorState.ApplicationErrorState;

    /// <summary>
    /// Tests the <see cref="Error" /> page.
    /// </summary>
    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class ErrorTestFixture : BunitContext
    {
        /// <summary>
        /// Configures the page's Blueprint controls.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            BlueprintTestSetup.Configure(this);
        }

        /// <summary>
        /// Disposes the bUnit test context after each test.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            this.Dispose();
        }

        /// <summary>
        /// Verifies the safe error presentation and request reference fallback.
        /// </summary>
        [Test]
        public void VerifyErrorPresentation()
        {
            Activity.Current = null;

            var httpContext = new DefaultHttpContext
            {
                TraceIdentifier = "test-request-id"
            };

            var component = this.Render<Error>(parameters => parameters.AddCascadingValue<HttpContext>(httpContext));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find("h1").TextContent, Is.EqualTo("Something failed to resolve."));
                Assert.That(component.Find("p").TextContent, Is.EqualTo("Mycelium encountered an unexpected error while processing this request."));
                Assert.That(component.Find("dt").TextContent, Is.EqualTo("Reference ID"));
                Assert.That(component.Find("code").TextContent, Is.EqualTo("test-request-id"));
                Assert.That(component.Find("a").GetAttribute("href"), Is.EqualTo("/workspace/modeling"));
                Assert.That(component.Find("a").TextContent.Trim(), Is.EqualTo("Back to Modelling"));
                Assert.That(component.Find("button").TextContent.Trim(), Is.EqualTo("Reload"));
                Assert.That(component.Find("button").GetAttribute("type"), Is.EqualTo("button"));
                Assert.That(component.Markup, Does.Not.Contain("Development Mode").And.Not.Contain("ASPNETCORE_ENVIRONMENT"));
            }
        }

        /// <summary>
        /// Verifies that an active trace takes precedence over the HTTP request identifier.
        /// </summary>
        [Test]
        public void VerifyRequestIdUsesCurrentActivity()
        {
            using var activity = new Activity("error-page-test").Start();
            var component = this.Render<Error>(parameters => parameters.AddCascadingValue<HttpContext>(new DefaultHttpContext
            {
                TraceIdentifier = "fallback-request-id"
            }));

            Assert.That(component.Find("code").TextContent, Is.EqualTo(activity.Id));
        }

        /// <summary>
        /// Verifies that rendering without a request context remains safe.
        /// </summary>
        [Test]
        public void VerifyMissingReferenceIsOmitted()
        {
            Activity.Current = null;
            var component = this.Render<Error>();

            Assert.That(component.FindAll("dl"), Is.Empty);
        }

        /// <summary>
        /// Verifies that request references render as text instead of executable markup.
        /// </summary>
        [Test]
        public void VerifyReferenceIsEncoded()
        {
            Activity.Current = null;
            const string reference = "<script>unexpected()</script>";
            var component = this.Render<Error>(parameters => parameters.AddCascadingValue<HttpContext>(new DefaultHttpContext
            {
                TraceIdentifier = reference
            }));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find("code").TextContent, Is.EqualTo(reference));
                Assert.That(component.FindAll("script"), Is.Empty);
            }
        }

        /// <summary>
        /// Verifies that reload remains available on a static exception response.
        /// </summary>
        [Test]
        public void VerifyReload()
        {
            var component = this.Render<Error>();

            Assert.That(component.Find("button").GetAttribute("data-error-action"), Is.EqualTo("reload"));
        }

        /// <summary>
        /// Verifies framework persistence and the View share one page-owned reference authority.
        /// </summary>
        [Test]
        public async Task VerifyPageOwnsReactiveReferenceState()
        {
            var component = this.Render<Error>();
            var viewModel = component.FindComponent<ErrorStateComponent>().Instance.ViewModel;

            Assert.That(viewModel.Kind, Is.EqualTo(ApplicationErrorKind.ServerError));

            await component.InvokeAsync(() => component.Instance.RequestId = "persisted-reference");

            await component.WaitForAssertionAsync(() => Assert.That(component.Find("code").TextContent, Is.EqualTo("persisted-reference")));
            Assert.That(viewModel.ReferenceId, Is.EqualTo("persisted-reference"));

            await component.InvokeAsync(() => viewModel.ReferenceId = " ");

            await component.WaitForAssertionAsync(() => Assert.That(component.FindAll("dl"), Is.Empty));
            Assert.That(component.Instance.RequestId, Is.Null);

            await this.DisposeComponentsAsync();

            Assert.That(() => viewModel.ReferenceId = "late-reference", Throws.TypeOf<ObjectDisposedException>());
        }
    }
}
