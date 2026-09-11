// ------------------------------------------------------------------------------------------------
// <copyright file="ErrorPipelineTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Hosting
{
    using System;
    using System.Net;
    using System.Threading.Tasks;

    using AngleSharp.Html.Parser;

    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc.Testing;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;

    using Moq;

    using Mycelium.Bloom.Components;

    /// <summary>
    /// Exercises the production error middleware with failures injected only into the test host.
    /// </summary>
    [TestFixture]
    public sealed class ErrorPipelineTestFixture
    {
        /// <summary>
        /// Verifies that status-code re-execution preserves a real missing-route response.
        /// </summary>
        /// <param name="path">The unknown request path.</param>
        [TestCase("/route-that-does-not-exist")]
        [TestCase("/workspace/missing/nested?source=test")]
        public async Task VerifyUnknownRouteReturnsStyled404(string path)
        {
            await using var application = new ErrorApplicationFactory();
            using var client = application.CreateClient(new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost"),
                AllowAutoRedirect = false
            });

            using var response = await client.GetAsync(path);
            using var document = await new HtmlParser().ParseDocumentAsync(await response.Content.ReadAsStringAsync());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
                Assert.That(response.Headers.Location, Is.Null);
                Assert.That(document.QuerySelector(".error-layout header")?.TextContent, Does.Contain("Mycelium Bloom"));
                Assert.That(document.QuerySelector(".error-layout footer")?.TextContent, Does.Contain("© 2026 Starion Group S.A."));
                Assert.That(document.QuerySelectorAll("aside, nav, .application-error-state__status"), Is.Empty);
                Assert.That(document.Title, Is.EqualTo("Page unavailable | Mycelium Bloom"));
                Assert.That(document.QuerySelector("main h1")?.TextContent, Is.EqualTo("This branch leads nowhere."));
                Assert.That(document.QuerySelector("main p")?.TextContent, Is.EqualTo("The page may have moved or the route no longer exists."));
                Assert.That(document.QuerySelector("section a")?.GetAttribute("href"), Is.EqualTo("/workspace/modeling"));
                Assert.That(document.QuerySelector("section button")?.TextContent.Trim(), Is.EqualTo("Go back"));
                Assert.That(document.Body.TextContent, Does.Not.Contain("Sorry, the content").And.Not.Contain("Not Found"));
            }
        }

        /// <summary>
        /// Verifies that exception-handler re-execution emits a safe server-error response.
        /// </summary>
        [Test]
        public async Task VerifyUnhandledExceptionReturnsStyled500()
        {
            await using var application = new ErrorApplicationFactory();
            using var client = application.CreateClient(new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost"),
                AllowAutoRedirect = false
            });

            using var response = await client.GetAsync("/test-only-failure?original=query");
            var body = await response.Content.ReadAsStringAsync();
            using var document = await new HtmlParser().ParseDocumentAsync(body);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
                Assert.That(response.Headers.Location, Is.Null);
                Assert.That(document.QuerySelector(".error-layout header")?.TextContent, Does.Contain("Mycelium Bloom"));
                Assert.That(document.QuerySelector(".error-layout footer")?.TextContent, Does.Contain("© 2026 Starion Group S.A."));
                Assert.That(document.QuerySelectorAll("aside, nav, .application-error-state__status"), Is.Empty);
                Assert.That(document.Title, Is.EqualTo("Request error | Mycelium Bloom"));
                Assert.That(document.QuerySelector("main h1")?.TextContent, Is.EqualTo("Something failed to resolve."));
                Assert.That(document.QuerySelector("main p")?.TextContent, Is.EqualTo("Mycelium encountered an unexpected error while processing this request."));
                Assert.That(document.QuerySelector("section a")?.GetAttribute("href"), Is.EqualTo("/workspace/modeling"));
                Assert.That(document.QuerySelector("section button")?.TextContent.Trim(), Is.EqualTo("Reload"));
                Assert.That(document.QuerySelector("dt")?.TextContent, Is.EqualTo("Reference ID"));
                Assert.That(document.QuerySelector("code")?.TextContent, Is.Not.Null.And.Not.Empty);
                Assert.That(body, Does.Not.Contain("test-only exception detail").And.Not.Contain("InvalidOperationException")
                    .And.Not.Contain("Development Mode").And.Not.Contain("ASPNETCORE_ENVIRONMENT").And.Not.Contain("StackTrace"));
            }
        }

        /// <summary>
        /// Hosts the unchanged application pipeline with a downstream test-only failure.
        /// </summary>
        internal sealed class ErrorApplicationFactory : WebApplicationFactory<App>
        {
            /// <summary>
            /// Configures production middleware and adds the isolated failure probe.
            /// </summary>
            /// <param name="builder">The test application builder.</param>
            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                builder.UseEnvironment(Environments.Production).UseSetting("https_port", "443");
                builder.ConfigureServices(services =>
                {
                    var filter = new Mock<IStartupFilter>();
                    filter.Setup(value => value.Configure(It.IsAny<Action<IApplicationBuilder>>()))
                        .Returns<Action<IApplicationBuilder>>(next => app =>
                        {
                            next(app);
                            app.Use(async (context, continuation) =>
                            {
                                if (context.Request.Path == "/test-only-failure")
                                {
                                    throw new InvalidOperationException("test-only exception detail");
                                }

                                await continuation(context);
                            });
                        });
                    services.AddSingleton(filter.Object);
                });
            }
        }
    }
}
