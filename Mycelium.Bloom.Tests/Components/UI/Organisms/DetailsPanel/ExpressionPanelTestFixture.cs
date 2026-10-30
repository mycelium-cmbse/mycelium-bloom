// ------------------------------------------------------------------------------------------------
// <copyright file="ExpressionPanelTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Components.UI.Organisms.DetailsPanel
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Bunit;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using Mycelium.Bloom.Tests.Common;
    using Mycelium.Bloom.ViewModel.DetailsPanel;
    using SysML2.NET.Core.POCO.Kernel.Expressions;
    using static Mycelium.Bloom.Tests.Common.ExpressionModelTestFactory;
    using Panel = Mycelium.Bloom.Components.UI.Organisms.DetailsPanel.DetailsPanel;

    /// <summary>Tests expression fields and reference dispatch without a real selection service or ViewModel.</summary>
    [TestFixture, FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class ExpressionPanelTestFixture : BunitContext
    {
        [TearDown]
        public void TearDown() => this.Dispose();

        [TestCase("dimensions", "mRef")]
        [TestCase("orderSum", "contravariantOrder")]
        [TestCase("boundMatch", "isBound")]
        public async Task VerifySupportedExpressionsAndCanonicalReferences(string name, string targetName)
        {
            BlueprintTestSetup.Configure(this);
            var selected = Descendants(LoadQuantities()).Single(element => element.name == name);
            var expected = Descendants(selected).OfType<IFeatureReferenceExpression>()
                .First(expression => expression.referent?.name == targetName).referent;
            var viewModel = new Mock<IDetailsPanelViewModel>();
            viewModel.SetupGet(vm => vm.Presentation).Returns(new DetailsPanelPresentation(selected));
            this.Services.AddSingleton<Func<IDetailsPanelViewModel>>(() => viewModel.Object);
            var component = this.Render<Panel>();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find("section[aria-label='Expressions'], section[aria-label='Value']").TextContent, Does.Contain(targetName));
                Assert.That(component.FindAll("pre"), Is.Empty);
                Assert.That(component.Markup, Does.Not.Contain("SDK textual notation").And.Not.Contain("Advanced model links"));
            }
            await component.FindAll("button[aria-label^='Inspect']")
                .First(button => button.GetAttribute("aria-label").EndsWith(": " + targetName, StringComparison.Ordinal)).ClickAsync();
            viewModel.Verify(vm => vm.Inspect(expected), Times.Once);
        }
    }
}
