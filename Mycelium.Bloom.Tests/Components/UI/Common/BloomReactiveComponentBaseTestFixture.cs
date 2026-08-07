// ------------------------------------------------------------------------------------------------
// <copyright file="BloomReactiveComponentBaseTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Components.UI.Common
{
    using Bunit;

    using Microsoft.AspNetCore.Components.Rendering;
    using Microsoft.Extensions.DependencyInjection;

    using Mycelium.Bloom.Components.UI.Common;

    using ReactiveUI;

    /// <summary>
    /// Tests the Bloom reactive component base classes.
    /// </summary>
    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class BloomReactiveComponentBaseTestFixture : BunitContext
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
        /// Verifies both reactive bases provide the shared Bloom parameter defaults.
        /// </summary>
        [Test]
        public void VerifyReactiveBasesProvideBloomDefaults()
        {
            var viewModel = new TestViewModel();
            this.Services.AddSingleton(viewModel);

            using var parameterBoundComponent = this.Render<TestReactiveComponent>(parameters => parameters
                .Add(testComponent => testComponent.ViewModel, viewModel));
            using var injectableComponent = this.Render<TestReactiveInjectableComponent>();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(parameterBoundComponent.Instance.Class, Is.Empty);
                Assert.That(parameterBoundComponent.Instance.AdditionalAttributes, Is.Empty);
                Assert.That(injectableComponent.Instance.Class, Is.Empty);
                Assert.That(injectableComponent.Instance.AdditionalAttributes, Is.Empty);
            }
        }

        /// <summary>
        /// Verifies the parameter-bound reactive base provides the shared Bloom contract and reactive rendering.
        /// </summary>
        [Test]
        public void VerifyParameterBoundBaseProvidesBloomContractAndReactiveRendering()
        {
            var viewModel = new TestViewModel();
            using var component = this.Render<TestReactiveComponent>(parameters => parameters
                .Add(testComponent => testComponent.ViewModel, viewModel)
                .Add(testComponent => testComponent.Class, "custom-reactive")
                .AddUnmatched("data-testid", "parameter-bound"));

            var root = component.Find("div");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Instance, Is.AssignableTo<IBloomComponentBase>());
                Assert.That(root.ClassList.Contains("mb-test-reactive"), Is.True);
                Assert.That(root.ClassList.Contains("custom-reactive"), Is.True);
                Assert.That(root.GetAttribute("data-testid"), Is.EqualTo("parameter-bound"));
                Assert.That(root.TextContent, Is.EqualTo("Initial"));
            }

            viewModel.Text = "Updated";

            component.WaitForAssertion(() => Assert.That(component.Find("div").TextContent, Is.EqualTo("Updated")));
        }

        /// <summary>
        /// Verifies the injectable reactive base resolves its ViewModel and provides the shared Bloom contract.
        /// </summary>
        [Test]
        public void VerifyInjectableBaseProvidesBloomContractAndReactiveRendering()
        {
            var viewModel = new TestViewModel();
            this.Services.AddSingleton(viewModel);

            using var component = this.Render<TestReactiveInjectableComponent>(parameters => parameters
                .Add(testComponent => testComponent.Class, "custom-injectable")
                .AddUnmatched("data-testid", "injectable"));

            var root = component.Find("div");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Instance, Is.AssignableTo<IBloomComponentBase>());
                Assert.That(component.Instance.ViewModel, Is.SameAs(viewModel));
                Assert.That(root.ClassList.Contains("mb-test-reactive-injectable"), Is.True);
                Assert.That(root.ClassList.Contains("custom-injectable"), Is.True);
                Assert.That(root.GetAttribute("data-testid"), Is.EqualTo("injectable"));
                Assert.That(root.TextContent, Is.EqualTo("Initial"));
            }

            viewModel.Text = "Updated";

            component.WaitForAssertion(() => Assert.That(component.Find("div").TextContent, Is.EqualTo("Updated")));
        }

        /// <summary>
        /// A minimal reactive ViewModel used to verify component observation.
        /// </summary>
        private sealed class TestViewModel : ReactiveObject
        {
            /// <summary>
            /// The rendered text.
            /// </summary>
            private string text = "Initial";

            /// <summary>
            /// Gets or sets the rendered text.
            /// </summary>
            public string Text
            {
                get => this.text;
                set => this.RaiseAndSetIfChanged(ref this.text, value);
            }
        }

        /// <summary>
        /// A parameter-bound reactive component used to exercise the Bloom base.
        /// </summary>
        private sealed class TestReactiveComponent : BloomReactiveComponentBase<TestViewModel>
        {
            /// <inheritdoc />
            protected override void BuildRenderTree(RenderTreeBuilder builder)
            {
                builder.OpenElement(0, "div");
                builder.AddMultipleAttributes(1, this.AdditionalAttributes);
                builder.AddAttribute(2, "class", this.BuildRootCssClass("mb-test-reactive"));
                builder.AddContent(3, this.ViewModel?.Text);
                builder.CloseElement();
            }
        }

        /// <summary>
        /// An injectable reactive component used to exercise the Bloom base.
        /// </summary>
        private sealed class TestReactiveInjectableComponent : BloomReactiveInjectableComponentBase<TestViewModel>
        {
            /// <inheritdoc />
            protected override void BuildRenderTree(RenderTreeBuilder builder)
            {
                builder.OpenElement(0, "div");
                builder.AddMultipleAttributes(1, this.AdditionalAttributes);
                builder.AddAttribute(2, "class", this.BuildRootCssClass("mb-test-reactive-injectable"));
                builder.AddContent(3, this.ViewModel?.Text);
                builder.CloseElement();
            }
        }
    }
}
