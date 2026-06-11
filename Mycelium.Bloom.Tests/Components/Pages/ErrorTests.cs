namespace Mycelium.Bloom.Tests.Components.Pages
{
    using System.Diagnostics;

    using Bunit;

    using Microsoft.AspNetCore.Components;
    using Microsoft.AspNetCore.Http;

    using Mycelium.Bloom.Components.Pages;

    using NUnit.Framework;

    /// <summary>
    /// Tests the <see cref="Error"/> page.
    /// </summary>
    [TestFixture]
    public sealed class ErrorTests : BunitContext
    {
        /// <summary>
        /// Renders the <see cref="Error"/> component with the provided HTTP context as a cascading value.
        /// </summary>
        /// <param name="httpContext">
        /// The HTTP context to provide to the component.
        /// </param>
        /// <returns>
        /// The rendered <see cref="Error"/> component.
        /// </returns>
        private IRenderedComponent<Error> RenderErrorComponent(HttpContext httpContext)
        {
            RenderFragment renderFragment = builder =>
            {
                builder.OpenComponent<CascadingValue<HttpContext>>(0);
                builder.AddAttribute(1, nameof(CascadingValue<HttpContext>.Value), httpContext);
                builder.AddAttribute(2, nameof(CascadingValue<HttpContext>.ChildContent), (RenderFragment)(childBuilder =>
                {
                    childBuilder.OpenComponent<Error>(3);
                    childBuilder.CloseComponent();
                }));
                builder.CloseComponent();
            };
     
            var renderedFragment = this.Render(renderFragment);
     
            return renderedFragment.FindComponent<Error>();
        }


        /// <summary>
        /// Verifies that the error page displays the expected default error content.
        /// </summary>
        [Test]
        public void Render_DisplaysDefaultErrorContent()
        {
            Activity.Current = null;

            var httpContext = new DefaultHttpContext
            {
                TraceIdentifier = "test-request-id"
            };

            var component = this.RenderErrorComponent(httpContext);

            Assert.That(component.Find("h1").TextContent, Is.EqualTo("Error."));
            Assert.That(component.Find("h2").TextContent, Is.EqualTo("An error occurred while processing your request."));
            Assert.That(component.Find("h3").TextContent, Is.EqualTo("Development Mode"));
            Assert.That(component.Find("code").TextContent, Is.EqualTo("test-request-id"));
            Assert.That(component.Markup, Does.Contain("The Development environment shouldn't be enabled for deployed applications."));
        }

        /// <summary>
        /// Disposes the bUnit test context after each test.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            this.Dispose();
        }
    }
}