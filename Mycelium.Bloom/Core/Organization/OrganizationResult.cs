// ------------------------------------------------------------------------------------------------
// <copyright file="OrganizationResult.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Core.Organization
{
    using Mycelium.Bloom.Model;

    /// <summary>
    /// Carries either the value of an organization operation or the expected refusal that prevented it.
    /// </summary>
    /// <typeparam name="TValue">The type of the value of a successful operation.</typeparam>
    public sealed class OrganizationResult<TValue>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OrganizationResult{TValue}" /> class.
        /// </summary>
        /// <param name="value">The value of a successful operation.</param>
        /// <param name="error">The refusal of a failed operation, or <see langword="null" /> on success.</param>
        private OrganizationResult(TValue value, OrganizationError error)
        {
            this.Value = value;
            this.Error = error;
        }

        /// <summary>
        /// Gets a value indicating whether the operation succeeded.
        /// </summary>
        public bool IsSuccess => this.Error is null;

        /// <summary>
        /// Gets the value of a successful operation, or the default value when the operation failed.
        /// </summary>
        public TValue Value { get; }

        /// <summary>
        /// Gets the refusal of a failed operation, or <see langword="null" /> when the operation succeeded.
        /// </summary>
        public OrganizationError Error { get; }

        /// <summary>
        /// Creates the result of a successful operation.
        /// </summary>
        /// <param name="value">The value of the operation.</param>
        /// <returns>The successful result.</returns>
        public static OrganizationResult<TValue> Success(TValue value)
        {
            return new OrganizationResult<TValue>(value, null);
        }

        /// <summary>
        /// Creates the result of a refused operation.
        /// </summary>
        /// <param name="error">The refusal.</param>
        /// <returns>The failed result.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="error" /> is <see langword="null" />.</exception>
        public static OrganizationResult<TValue> Failure(OrganizationError error)
        {
            ArgumentNullException.ThrowIfNull(error);

            return new OrganizationResult<TValue>(default, error);
        }
    }
}
