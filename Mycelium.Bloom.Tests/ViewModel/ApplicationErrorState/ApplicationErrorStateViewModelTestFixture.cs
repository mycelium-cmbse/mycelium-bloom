// ------------------------------------------------------------------------------------------------
// <copyright file="ApplicationErrorStateViewModelTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.ViewModel.ApplicationErrorState
{
    using System;
    using System.Collections.Generic;
    using System.Reactive.Linq;

    using Mycelium.Bloom.Model.Enum;
    using Mycelium.Bloom.ViewModel.ApplicationErrorState;

    using ReactiveUI;

    /// <summary>
    /// Verifies page-owned error content, recovery metadata and reactive reference state.
    /// </summary>
    [TestFixture]
    public sealed class ApplicationErrorStateViewModelTestFixture
    {
        /// <summary>
        /// Verifies the missing-route presentation and browser-history recovery contract.
        /// </summary>
        [Test]
        public void VerifyNotFoundState()
        {
            using var viewModel = new ApplicationErrorStateViewModel(ApplicationErrorKind.NotFound);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.Kind, Is.EqualTo(ApplicationErrorKind.NotFound));
                Assert.That(viewModel.Title, Is.EqualTo("This branch leads nowhere."));
                Assert.That(viewModel.Description, Is.EqualTo("The page may have moved or the route no longer exists."));
                Assert.That(viewModel.PageTitle, Is.EqualTo("Page unavailable | Mycelium Bloom"));
                Assert.That(viewModel.ReferenceId, Is.Null);
                Assert.That(viewModel.HasReferenceId, Is.False);
                Assert.That(viewModel.PrimaryAction.Label, Is.EqualTo("Back to Modelling"));
                Assert.That(viewModel.PrimaryAction.Href, Is.EqualTo("/workspace/modeling"));
                Assert.That(viewModel.PrimaryAction.BrowserAction, Is.Null);
                Assert.That(viewModel.SecondaryAction.Label, Is.EqualTo("Go back"));
                Assert.That(viewModel.SecondaryAction.Href, Is.Null);
                Assert.That(viewModel.SecondaryAction.BrowserAction, Is.EqualTo("back"));
            }
        }

        /// <summary>
        /// Verifies the server-error presentation and circuit-independent reload contract.
        /// </summary>
        [Test]
        public void VerifyServerErrorState()
        {
            using var viewModel = new ApplicationErrorStateViewModel(ApplicationErrorKind.ServerError, "request-reference");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.Kind, Is.EqualTo(ApplicationErrorKind.ServerError));
                Assert.That(viewModel.Title, Is.EqualTo("Something failed to resolve."));
                Assert.That(viewModel.Description, Is.EqualTo("Mycelium encountered an unexpected error while processing this request."));
                Assert.That(viewModel.PageTitle, Is.EqualTo("Request error | Mycelium Bloom"));
                Assert.That(viewModel.ReferenceId, Is.EqualTo("request-reference"));
                Assert.That(viewModel.HasReferenceId, Is.True);
                Assert.That(viewModel.PrimaryAction.Label, Is.EqualTo("Reload"));
                Assert.That(viewModel.PrimaryAction.Href, Is.Null);
                Assert.That(viewModel.PrimaryAction.BrowserAction, Is.EqualTo("reload"));
                Assert.That(viewModel.SecondaryAction.Label, Is.EqualTo("Back to Modelling"));
                Assert.That(viewModel.SecondaryAction.Href, Is.EqualTo("/workspace/modeling"));
                Assert.That(viewModel.SecondaryAction.BrowserAction, Is.Null);
            }
        }

        /// <summary>
        /// Verifies normalization only removes absent references and preserves opaque reference text.
        /// </summary>
        /// <param name="reference">The supplied request reference.</param>
        /// <param name="expected">The normalized reference.</param>
        [TestCase(null, null)]
        [TestCase("", null)]
        [TestCase(" \t\r\n", null)]
        [TestCase("00-trace-span", "00-trace-span")]
        [TestCase(" opaque-reference ", " opaque-reference ")]
        public void VerifyReferenceNormalization(string reference, string expected)
        {
            using var viewModel = new ApplicationErrorStateViewModel(ApplicationErrorKind.ServerError, reference);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(viewModel.ReferenceId, Is.EqualTo(expected));
                Assert.That(viewModel.HasReferenceId, Is.EqualTo(expected is not null));
            }
        }

        /// <summary>
        /// Verifies reference changes notify consumers and derive availability without duplicate notifications.
        /// </summary>
        [Test]
        public void VerifyReferenceNotificationsAndDerivedAvailability()
        {
            using var viewModel = new ApplicationErrorStateViewModel(ApplicationErrorKind.ServerError);
            var changes = new List<string>();
            var availability = new List<bool>();
            using var changeSubscription = viewModel.Changed.Subscribe(change => changes.Add(change.PropertyName));
            using var availabilitySubscription = viewModel.WhenAnyValue(value => value.HasReferenceId).Subscribe(availability.Add);

            viewModel.ReferenceId = "first-reference";

            Assert.That(changes, Is.EquivalentTo(new[] { nameof(viewModel.ReferenceId), nameof(viewModel.HasReferenceId) }));
            changes.Clear();

            viewModel.ReferenceId = "first-reference";

            Assert.That(changes, Is.Empty);

            viewModel.ReferenceId = "second-reference";

            Assert.That(changes, Is.EqualTo(new[] { nameof(viewModel.ReferenceId) }));
            changes.Clear();

            viewModel.ReferenceId = " ";

            Assert.That(changes, Is.EquivalentTo(new[] { nameof(viewModel.ReferenceId), nameof(viewModel.HasReferenceId) }));
            changes.Clear();

            viewModel.ReferenceId = null;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(changes, Is.Empty);
                Assert.That(availability, Is.EqualTo(new[] { false, true, false }));
            }
        }

        /// <summary>
        /// Verifies page disposal prevents publication from an abandoned ViewModel.
        /// </summary>
        [Test]
        public void VerifyDispose()
        {
            var viewModel = new ApplicationErrorStateViewModel(ApplicationErrorKind.ServerError);

            viewModel.Dispose();
            viewModel.Dispose();

            Assert.That(() => viewModel.ReferenceId = "late-reference", Throws.TypeOf<ObjectDisposedException>());
        }

        /// <summary>
        /// Verifies unsupported error kinds are rejected instead of selecting an arbitrary presentation.
        /// </summary>
        [Test]
        public void VerifyUnsupportedKindIsRejected()
        {
            Assert.That(() => new ApplicationErrorStateViewModel((ApplicationErrorKind)(-1)),
                Throws.TypeOf<ArgumentOutOfRangeException>());
        }
    }
}
