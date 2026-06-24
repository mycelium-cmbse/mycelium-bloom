namespace Mycelium.Bloom.Tests.Components.Layout
{
    using Bunit;

    using Mycelium.Bloom.Components.Layout;

    /// <summary>
    /// Tests the <see cref="ReconnectModal" /> component.
    /// </summary>
    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class ReconnectModalTestFixture : BunitContext
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
        /// Verifies that the reconnect modal displays the expected reconnect states.
        /// </summary>
        [Test]
        public void Render_DisplaysReconnectStates()
        {
            var component = this.Render<ReconnectModal>();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find("#components-reconnect-modal"), Is.Not.Null);
                Assert.That(component.Markup, Does.Contain("Rejoining the server..."));
                Assert.That(component.Markup, Does.Contain("Failed to rejoin."));
                Assert.That(component.Markup, Does.Contain("The session has been paused by the server."));
            }
        }
    }
}
