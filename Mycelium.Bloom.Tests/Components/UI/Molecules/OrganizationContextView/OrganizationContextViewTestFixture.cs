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
    public sealed class OrganizationContextViewTestFixture
    {
        private BunitContext context;

        private static readonly (string, bool)[] NoNavigation = [];

        private static readonly (string, bool)[] ErrorPageReplacement = [("/Error", true)];

        private Mock<IOrganizationContextViewModel> viewModel;

        private RenderFragment content;

        [SetUp]
        public void SetUp()
        {
            this.context = new BunitContext();
            BlueprintTestSetup.Configure(this.context);
            this.viewModel = new Mock<IOrganizationContextViewModel>();
            this.viewModel.SetupGet(model => model.Errors).Returns([]);
            this.context.Services.AddSingleton(this.viewModel.Object);

            this.content = builder => builder.AddContent(0, "Page content");
        }

        [TearDown]
        public async Task TearDown()
        {
            await this.context.DisposeAsync();
        }

        [TestCase(true)]
        [TestCase(false)]
        public void VerifyContentIsShownOnceLoaded(bool isLoading)
        {
            this.viewModel.SetupGet(model => model.IsLoading).Returns(isLoading);

            var component = this.context.Render<OrganizationContextView>(parameters => parameters.Add(view => view.ChildContent, this.content));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.FindAll("[role='status']"), Has.Count.EqualTo(isLoading ? 1 : 0));
                Assert.That(component.Markup, isLoading ? Does.Not.Contain("Page content") : Does.Contain("Page content"));
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
            var navigation = (BunitNavigationManager)this.context.Services.GetRequiredService<NavigationManager>();
            var expectedHistory = showsNotFound ? NoNavigation : ErrorPageReplacement;
            var notFoundShown = false;
            navigation.OnNotFound += (_, _) => notFoundShown = true;
            this.viewModel.SetupGet(model => model.Errors).Returns(reportedAfterRender ? [] : errors);

            var component = this.context.Render<OrganizationContextView>(parameters => parameters.Add(view => view.ChildContent, this.content));

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
