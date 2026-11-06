// ------------------------------------------------------------------------------------------------
// <copyright file="ApplicationHostingTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Hosting
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;

    using AngleSharp.Html.Parser;

    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.HttpOverrides;
    using Microsoft.AspNetCore.Mvc.Testing;
    using Microsoft.Extensions.Hosting;

    using Mycelium.Bloom.Components;

    [TestFixture]
    public sealed class ApplicationHostingTestFixture
    {
        private WebApplicationFactory<App> factory;
        private WebApplicationFactory<App> application;

        [OneTimeSetUp]
        public void SetUp()
        {
            this.factory = new WebApplicationFactory<App>();
            this.application = this.factory.WithWebHostBuilder(builder => builder
                .UseEnvironment(Environments.Production)
                .UseSetting("https_port", "443"));
        }

        [OneTimeTearDown]
        public async Task TearDown()
        {
            await this.application.DisposeAsync();
            await this.factory.DisposeAsync();
        }

        [TestCase("/healthz")]
        [TestCase("/ready")]
        public async Task VerifyHealthEndpointReturnsHealthy(string path)
        {
            using var client = this.application.CreateClient(new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost"),
                AllowAutoRedirect = false
            });

            using var response = await client.GetAsync(path);
            var body = await response.Content.ReadAsStringAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(body, Is.EqualTo("Healthy"));
            }
        }

        /// <summary>
        /// Verifies the rendered application root loads Blueprint, shared tokens, Tailwind and scoped styles in order.
        /// </summary>
        [Test]
        public async Task VerifyApplicationRootRendersStylesheetsInContractOrder()
        {
            using var client = this.application.CreateClient(new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost"),
                AllowAutoRedirect = false
            });

            using var response = await client.GetAsync("/Error");
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            using var document = await new HtmlParser().ParseDocumentAsync(await response.Content.ReadAsStringAsync());
            var stylesheets = document.QuerySelectorAll("head link[rel='stylesheet']")
                .Select(link => link.GetAttribute("href"))
                .ToArray();

            Assert.That(stylesheets, Has.Length.EqualTo(5));
            using (Assert.EnterMultipleScope())
            {
                Assert.That(stylesheets[0], Is.EqualTo("_content/BlazorBlueprint.Primitives/css/primitives.css"));
                Assert.That(stylesheets[1], Is.EqualTo("_content/BlazorBlueprint.Components/blazorblueprint.css"));
                Assert.That(stylesheets[2], Does.Match(@"^css/tokens(?:\.[\w-]+)?\.css$"));
                Assert.That(stylesheets[3], Does.Match(@"^css/app(?:\.[\w-]+)?\.css$"));
                Assert.That(stylesheets[4], Does.Match(@"^Mycelium\.Bloom(?:\.[\w-]+)?\.styles\.css$"));
            }
        }

        [Test]
        public async Task VerifyForwardedHttpsIsAppliedBeforeHttpsRedirection()
        {
            var directRequest = await this.application.Server.SendAsync(context =>
            {
                context.Connection.RemoteIpAddress = IPAddress.Loopback;
                context.Request.Scheme = "http";
                context.Request.Host = new HostString("localhost");
                context.Request.Path = "/healthz";
            });

            using (Assert.EnterMultipleScope())
            {
                Assert.That(directRequest.Response.StatusCode, Is.EqualTo(StatusCodes.Status307TemporaryRedirect));
                Assert.That(directRequest.Response.Headers.Location.ToString(), Is.EqualTo("https://localhost/healthz"));
            }

            var forwardedRequest = await this.application.Server.SendAsync(context =>
            {
                context.Connection.RemoteIpAddress = IPAddress.Loopback;
                context.Request.Scheme = "http";
                context.Request.Host = new HostString("localhost");
                context.Request.Path = "/healthz";
                context.Request.Headers[ForwardedHeadersDefaults.XForwardedForHeaderName] = "198.51.100.20";
                context.Request.Headers[ForwardedHeadersDefaults.XForwardedProtoHeaderName] = "https";
            });

            using (Assert.EnterMultipleScope())
            {
                Assert.That(forwardedRequest.Response.StatusCode, Is.EqualTo(StatusCodes.Status200OK));
                Assert.That(forwardedRequest.Request.Scheme, Is.EqualTo("https"));
                Assert.That(forwardedRequest.Response.Headers.Location, Is.Empty);
            }
        }
    }
}
