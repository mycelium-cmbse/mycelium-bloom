namespace Mycelium.Bloom.Tests.Components.Pages
{
    using Bunit;

    using Mycelium.Bloom.Components.Pages;

    using NUnit.Framework;

    /// <summary>
    /// Tests the <see cref="NotFound"/> page.
    /// </summary>
    [TestFixture]
    public sealed class NotFoundTests : BunitContext
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
        /// Verifies that the not found page displays the expected title and message.
        /// </summary>
        [Test]
        public void Render_DisplaysNotFoundContent()
        {
            var component = this.Render<NotFound>();

            Assert.That(component.Find("h3").TextContent, Is.EqualTo("Not Found"));
            Assert.That(
                component.Find("p").TextContent,
                Is.EqualTo("Sorry, the content you are looking for does not exist."));
        }
    }
}
