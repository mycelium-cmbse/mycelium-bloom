// ------------------------------------------------------------------------------------------------
// <copyright file="OrganizationContextViewTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Components.UI.Molecules.OrganizationContextView
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Threading.Tasks;

    using Bunit;
    using Bunit.TestDoubles;

    using ErrorOr;

    using Microsoft.AspNetCore.Components;
    using Microsoft.Extensions.DependencyInjection;

    using Moq;

    using Mycelium.Bloom.Components.UI.Molecules.OrganizationContextView;
    using Mycelium.Bloom.Tests.Common;
    using Mycelium.Bloom.ViewModel.Organization;

    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class OrganizationContextViewTestFixture : BunitContext
    {
        private static readonly (string, bool)[] NoNavigation = [];

        private static readonly (string, bool)[] ErrorPageReplacement = [("/Error", true)];

        private Mock<IOrganizationContextViewModel> viewModel;

        private RenderFragment content;

        [SetUp]
        public void SetUp()
        {
            BlueprintTestSetup.Configure(this);
            this.viewModel = new Mock<IOrganizationContextViewModel>();
            this.viewModel.SetupGet(model => model.Errors).Returns([]);
            this.Services.AddSingleton(this.viewModel.Object);

            this.content = builder =>
            {
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "class", "page-content");
                builder.CloseElement();
            };
        }

        [TearDown]
        public async Task TearDown()
        {
            await this.DisposeAsync();
        }

        [TestCase(true)]
        [TestCase(false)]
        public void VerifyContentIsShownOnceLoaded(bool isLoading)
        {
            this.viewModel.SetupGet(model => model.IsLoading).Returns(isLoading);

            var component = this.Render<OrganizationContextView>(parameters => parameters.Add(view => view.ChildContent, this.content));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.FindAll("[role='status']"), Has.Count.EqualTo(isLoading ? 1 : 0));
                Assert.That(component.FindAll(".page-content"), Has.Count.EqualTo(isLoading ? 0 : 1));
            }
        }

        [TestCase(ErrorType.NotFound, false, true)]
        [TestCase(ErrorType.Forbidden, false, true)]
        [TestCase(ErrorType.Unexpected, false, false)]
        [TestCase(ErrorType.NotFound, true, true)]
        [TestCase(ErrorType.Unexpected, true, false)]
        public void VerifyErrorLeadsToItsPage(ErrorType type, bool reportedAfterRender, bool showsNotFound)
        {
            IReadOnlyList<Error> errors = [Error.Custom((int)type, "Organization.Code", "Refused.")];
            var navigation = (BunitNavigationManager)this.Services.GetRequiredService<NavigationManager>();
            var expectedHistory = showsNotFound ? NoNavigation : ErrorPageReplacement;
            var notFoundShown = false;
            navigation.OnNotFound += (_, _) => notFoundShown = true;
            this.viewModel.SetupGet(model => model.Errors).Returns(reportedAfterRender ? [] : errors);

            var component = this.Render<OrganizationContextView>(parameters => parameters.Add(view => view.ChildContent, this.content));

            if (reportedAfterRender)
            {
                this.viewModel.SetupGet(model => model.Errors).Returns(errors);
                this.viewModel.Raise(model => model.PropertyChanged += null, new PropertyChangedEventArgs(nameof(IOrganizationContextViewModel.Errors)));
            }

            component.WaitForAssertion(() =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(notFoundShown, Is.EqualTo(showsNotFound));
                    Assert.That(navigation.History.Select(entry => (entry.Uri, entry.Options.ReplaceHistoryEntry)), Is.EqualTo(expectedHistory));
                }
            });
        }
    }
}
