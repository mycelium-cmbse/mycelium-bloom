// ------------------------------------------------------------------------------------------------
// <copyright file="ServerConnectionTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Components.Pages
{
    using System;
    using System.ComponentModel;
    using System.Linq;
    using System.Threading.Tasks;

    using BlazorBlueprint.Components;

    using Bunit;

    using ErrorOr;

    using Microsoft.AspNetCore.Components;
    using Microsoft.Extensions.DependencyInjection;

    using Moq;

    using Mycelium.Bloom.Components.Pages;
    using Mycelium.Bloom.Core;
    using Mycelium.Bloom.Tests.Common;
    using Mycelium.Bloom.ViewModel.Session;

    using ReactiveUI;

    using Error = ErrorOr.Error;

    [TestFixture]
    public sealed class ServerConnectionTestFixture
    {
        private BunitContext context;

        private Mock<IConnectViewModel> viewModel;

        private int connectCount;

        [SetUp]
        public void SetUp()
        {
            this.context = new BunitContext();
            BlueprintTestSetup.Configure(this.context);
            this.connectCount = 0;
            this.viewModel = new Mock<IConnectViewModel>();
            this.viewModel.SetupProperty(model => model.ServerAddress, "https://fabric.example.org");
            this.viewModel.SetupGet(model => model.Errors).Returns([]);
            this.viewModel.SetupGet(model => model.Connect).Returns(ReactiveCommand.Create(() => { this.connectCount++; }));
            this.context.Services.AddSingleton<Func<IConnectViewModel>>(() => this.viewModel.Object);
        }

        [TearDown]
        public async Task TearDown()
        {
            await this.context.DisposeAsync();
        }

        [TestCase(ErrorType.Validation, 1, 0)]
        [TestCase(ErrorType.Failure, 0, 1)]
        public async Task VerifyARefusedAddressStaysUnderTheFieldAndAServerFailureInTheAlert(ErrorType type, int fieldErrorCount, int alertCount)
        {
            this.viewModel.SetupGet(model => model.Errors).Returns([Error.Custom((int)type, ErrorCodes.Validation, "Refused")]);
            var page = this.context.Render<ServerConnection>();

            await page.Find("form").SubmitAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(this.connectCount, Is.EqualTo(1));
                Assert.That(page.FindComponent<BbFieldError>().Instance.Errors, Has.Exactly(fieldErrorCount).Items);
                Assert.That(this.context.JSInterop.Invocations.Count(invocation => invocation.Identifier.Contains("focus", StringComparison.OrdinalIgnoreCase)), Is.EqualTo(fieldErrorCount));
                Assert.That(page.FindComponents<BbAlert>(), Has.Count.EqualTo(alertCount));
                Assert.That(page.FindComponent<BbField>().Instance.IsInvalid, Is.EqualTo(fieldErrorCount > 0));
            }
        }

        [TestCase("6f1d2c3a-0b4e-4c1a-9a51-6d2e8f0a1c01", "http://localhost/workspace/modeling?project=6f1d2c3a-0b4e-4c1a-9a51-6d2e8f0a1c01")]
        [TestCase("%2F%2Fexample.org", "http://localhost/account")]
        public void VerifyAnOpenConnectionReturnsOnlyToTheWorkspaceOfAProject(string returnProject, string expectedUri)
        {
            var navigation = this.context.Services.GetRequiredService<NavigationManager>();
            navigation.NavigateTo($"/?returnProject={returnProject}");
            this.viewModel.SetupGet(model => model.IsConnected).Returns(true);

            this.context.Render<ServerConnection>();

            Assert.That(navigation.Uri, Is.EqualTo(expectedUri));
        }

        [TestCase(true, false, "http://localhost/account")]
        [TestCase(false, true, "http://localhost/account")]
        [TestCase(false, false, "http://localhost/")]
        public async Task VerifyAnOpenConnectionLeavesForTheHomePageAndThePageReleasesItsViewModel(bool connectedAtStart, bool connectedLater, string expectedUri)
        {
            this.viewModel.SetupGet(model => model.IsConnected).Returns(connectedAtStart);

            this.context.Render<ServerConnection>();
            this.viewModel.SetupGet(model => model.IsConnected).Returns(connectedAtStart || connectedLater);
            this.viewModel.Raise(model => model.PropertyChanged += null, new PropertyChangedEventArgs(nameof(IConnectViewModel.IsConnected)));
            var uri = this.context.Services.GetRequiredService<NavigationManager>().Uri;
            await this.context.DisposeComponentsAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(uri, Is.EqualTo(expectedUri));
                this.viewModel.Verify(model => model.Dispose(), Times.Once);
            }
        }
    }
}
