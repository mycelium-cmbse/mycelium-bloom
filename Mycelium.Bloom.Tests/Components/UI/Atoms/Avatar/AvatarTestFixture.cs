// // ------------------------------------------------------------------------------------------------
// // <copyright file="AvatarTestFixture.cs" company="Starion Group S.A.">
// //
// //   Copyright 2026 Starion Group S.A.
// //   SPDX-License-Identifier: Apache-2.0
// //
// // </copyright>
// // ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Components.UI.Atoms.Avatar
{
    using Bunit;

    using Mycelium.Bloom.Model.Enum;

    using AvatarComponent = Mycelium.Bloom.Components.UI.Atoms.Avatar.Avatar;

    /// <summary>
    /// Tests the <see cref="AvatarComponent" /> component.
    /// </summary>
    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class AvatarTestFixture : BunitContext
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
        /// Verifies that the avatar displays configured text, classes, title, and custom colors.
        /// </summary>
        [Test]
        public void Render_DisplaysConfiguredAvatar()
        {
            var component = this.Render<AvatarComponent>(parameters => parameters
                .Add(component => component.Text, "OH")
                .Add(component => component.Title, "Omrane Haj Mohamed")
                .Add(component => component.BackgroundColor, "#123456")
                .Add(component => component.BorderColor, "#654321")
                .Add(component => component.Size, AvatarSize.Large)
                .Add(component => component.Variant, AvatarVariant.More)
                .Add(component => component.Class, "custom-avatar")
                .AddUnmatched("data-testid", "avatar"));

            var avatar = component.Find(".mb-avatar");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find(".mb-avatar__text").TextContent.Trim(), Is.EqualTo("OH"));
                Assert.That(avatar.GetAttribute("title"), Is.EqualTo("Omrane Haj Mohamed"));
                Assert.That(avatar.GetAttribute("data-testid"), Is.EqualTo("avatar"));
                Assert.That(avatar.GetAttribute("class"), Does.Contain("mb-avatar--large"));
                Assert.That(avatar.GetAttribute("class"), Does.Contain("mb-avatar--more"));
                Assert.That(avatar.GetAttribute("class"), Does.Contain("custom-avatar"));
                Assert.That(avatar.GetAttribute("style"), Does.Contain("--mb-avatar-background: #123456"));
                Assert.That(avatar.GetAttribute("style"), Does.Contain("--mb-avatar-border: #654321"));
            }
        }

        /// <summary>
        /// Verifies that the avatar defaults to a medium user avatar without custom styles.
        /// </summary>
        [Test]
        public void Render_UsesDefaultAvatarClassesWithoutStyle()
        {
            var component = this.Render<AvatarComponent>(parameters => parameters
                .Add(component => component.Text, "OH"));

            var avatar = component.Find(".mb-avatar");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(avatar.GetAttribute("class"), Does.Contain("mb-avatar--medium"));
                Assert.That(avatar.GetAttribute("class"), Does.Contain("mb-avatar--user"));
                Assert.That(avatar.HasAttribute("style"), Is.False);
            }
        }

        /// <summary>
        /// Verifies that the avatar uses the expected size class.
        /// </summary>
        /// <param name="size">The avatar size.</param>
        /// <param name="expectedCssClass">The expected CSS class.</param>
        [TestCase(AvatarSize.Small, "mb-avatar--small")]
        [TestCase(AvatarSize.Medium, "mb-avatar--medium")]
        [TestCase(AvatarSize.Large, "mb-avatar--large")]
        public void Render_UsesExpectedSizeClass(AvatarSize size, string expectedCssClass)
        {
            var component = this.Render<AvatarComponent>(parameters => parameters
                .Add(component => component.Text, "OH")
                .Add(component => component.Size, size));

            Assert.That(component.Find(".mb-avatar").GetAttribute("class"), Does.Contain(expectedCssClass));
        }
    }
}
