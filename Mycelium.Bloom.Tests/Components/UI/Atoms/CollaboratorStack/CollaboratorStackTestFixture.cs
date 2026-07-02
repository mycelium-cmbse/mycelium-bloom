// ------------------------------------------------------------------------------------------------
// <copyright file="CollaboratorStackTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Components.UI.Atoms.CollaboratorStack
{
    using System.Collections.Generic;

    using Bunit;

    using Mycelium.Bloom.Model;

    using CollaboratorStackComponent = Mycelium.Bloom.Components.UI.Atoms.CollaboratorStack.CollaboratorStack;

    /// <summary>
    /// Tests the <see cref="CollaboratorStackComponent" /> component.
    /// </summary>
    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class CollaboratorStackTestFixture : BunitContext
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
        /// Verifies that visible collaborators, status indicators, and overflow are rendered.
        /// </summary>
        [Test]
        public void VerifyRenderDisplaysCollaboratorsAndOverflow()
        {
            var component = this.Render<CollaboratorStackComponent>(parameters => parameters
                .Add(component => component.Items, GetItems())
                .Add(component => component.MaxVisible, 2)
                .Add(component => component.Class, "custom-stack")
                .AddUnmatched("data-testid", "collaborators"));

            var stack = component.Find(".mb-collaborator-stack");
            var items = component.FindAll(".mb-collaborator-stack__item");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(stack.GetAttribute("class"), Does.Contain("custom-stack"));
                Assert.That(stack.GetAttribute("data-testid"), Is.EqualTo("collaborators"));
                Assert.That(items, Has.Count.EqualTo(3));
                Assert.That(items[0].GetAttribute("title"), Is.EqualTo("Model Reviewer - Participant"));
                Assert.That(items[0].GetAttribute("class"), Does.Contain("mb-collaborator-stack__item--current-user"));
                Assert.That(component.FindAll(".mb-collaborator-stack__status"), Has.Count.EqualTo(2));
                Assert.That(component.FindAll(".mb-collaborator-stack__status")[0].GetAttribute("class"), Does.Contain("mb-collaborator-stack__status--online"));
                Assert.That(component.FindAll(".mb-collaborator-stack__status")[1].GetAttribute("class"), Does.Contain("mb-collaborator-stack__status--offline"));
                Assert.That(component.Find(".mb-collaborator-stack__more").GetAttribute("title"), Is.EqualTo("1 more collaborator"));
                Assert.That(component.Markup, Does.Contain("+1"));
            }
        }

        /// <summary>
        /// Verifies that empty and hidden states render no stack content.
        /// </summary>
        [Test]
        public void VerifyRenderHidesEmptyOrFullyHiddenStack()
        {
            var emptyComponent = this.Render<CollaboratorStackComponent>();
            var hiddenComponent = this.Render<CollaboratorStackComponent>(parameters => parameters
                .Add(component => component.Items, GetItems())
                .Add(component => component.MaxVisible, -1)
                .Add(component => component.ShowOnlineIndicator, false)
                .Add(component => component.ShowCurrentUserRing, false));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(emptyComponent.FindAll(".mb-collaborator-stack"), Is.Empty);
                Assert.That(hiddenComponent.FindAll(".mb-collaborator-stack__status"), Is.Empty);
                Assert.That(hiddenComponent.FindAll(".mb-collaborator-stack__item--current-user"), Is.Empty);
                Assert.That(hiddenComponent.Find(".mb-collaborator-stack__more").GetAttribute("title"), Is.EqualTo("3 more collaborators"));
            }
        }

        /// <summary>
        /// Gets sample collaborator stack items.
        /// </summary>
        /// <returns>The sample collaborator stack items.</returns>
        private static IReadOnlyList<CollaboratorStackItem> GetItems()
        {
            return
            [
                new()
                {
                    Id = "reviewer",
                    Name = "Model Reviewer",
                    Initials = "MR",
                    Role = "Participant",
                    Color = "#123456",
                    IsOnline = true,
                    IsCurrentUser = true
                },
                new()
                {
                    Id = "observer",
                    Name = "Project Observer",
                    Initials = "PO",
                    Color = "#654321",
                    IsOnline = false
                },
                new()
                {
                    Id = "lead",
                    Name = "Project Lead",
                    Initials = "PL",
                    Color = "#abcdef",
                    IsOnline = true
                }
            ];
        }
    }
}
