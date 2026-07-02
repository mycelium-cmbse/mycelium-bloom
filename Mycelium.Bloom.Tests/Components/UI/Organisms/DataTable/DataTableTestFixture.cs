// ------------------------------------------------------------------------------------------------
// <copyright file="DataTableTestFixture.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Components.UI.Organisms.DataTable
{
    using System.Collections.Generic;

    using Bunit;

    using Mycelium.Bloom.Model;

    using DataTableComponent = Mycelium.Bloom.Components.UI.Organisms.DataTable.DataTable;

    /// <summary>
    /// Tests the <see cref="DataTableComponent" /> component.
    /// </summary>
    [TestFixture]
    [FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
    public sealed class DataTableTestFixture : BunitContext
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
        /// Verifies that configured columns, rows, classes, and row callbacks are rendered.
        /// </summary>
        [Test]
        public void VerifyRenderDisplaysRowsAndInvokesSelection()
        {
            IReadOnlyDictionary<string, string> selectedRow = null;

            var rows = GetRows();

            var component = this.Render<DataTableComponent>(parameters => parameters
                .Add(component => component.Columns, GetColumns())
                .Add(component => component.Items, rows)
                .Add(component => component.IsCompact, false)
                .Add(component => component.HasBorder, false)
                .Add(component => component.RowSelected, row => selectedRow = row)
                .Add(component => component.Class, "custom-table")
                .AddUnmatched("data-testid", "data-table"));

            var table = component.Find(".mb-data-table");
            var bodyRows = component.FindAll("tbody tr");

            bodyRows[0].Click();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(selectedRow, Is.SameAs(rows[0]));
                Assert.That(table.GetAttribute("data-testid"), Is.EqualTo("data-table"));
                Assert.That(table.GetAttribute("class"), Does.Contain("custom-table"));
                Assert.That(table.GetAttribute("class"), Does.Not.Contain("mb-data-table--compact"));
                Assert.That(table.GetAttribute("class"), Does.Not.Contain("mb-data-table--bordered"));
                Assert.That(component.FindAll("th"), Has.Count.EqualTo(3));
                Assert.That(component.FindAll("th")[1].GetAttribute("style"), Is.EqualTo("width: 8rem;"));
                Assert.That(component.FindAll("th")[2].GetAttribute("class"), Does.Contain("mb-data-table__cell--right"));
                Assert.That(component.FindAll("td")[1].GetAttribute("class"), Does.Contain("mb-data-table__cell--mono"));
                Assert.That(component.FindAll("td")[2].GetAttribute("class"), Does.Contain("mb-data-table__cell--right"));
                Assert.That(component.FindAll("td")[0].TextContent.Trim(), Is.EqualTo("Requirement"));
                Assert.That(component.FindAll("td")[2].TextContent.Trim(), Is.Empty);
            }
        }

        /// <summary>
        /// Verifies that empty state spans at least one column.
        /// </summary>
        [Test]
        public void VerifyRenderDisplaysEmptyState()
        {
            var component = this.Render<DataTableComponent>(parameters => parameters
                .Add(component => component.EmptyText, "No rows."));

            var emptyCell = component.Find(".mb-data-table__empty");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find(".mb-data-table").GetAttribute("class"), Does.Contain("mb-data-table--compact"));
                Assert.That(component.Find(".mb-data-table").GetAttribute("class"), Does.Contain("mb-data-table--bordered"));
                Assert.That(emptyCell.TextContent.Trim(), Is.EqualTo("No rows."));
                Assert.That(emptyCell.GetAttribute("colspan"), Is.EqualTo("1"));
            }
        }

        /// <summary>
        /// Gets sample table columns.
        /// </summary>
        /// <returns>The sample table columns.</returns>
        private static IReadOnlyList<DataTableColumn> GetColumns()
        {
            return
            [
                new() { Key = "type", Header = "Type" },
                new() { Key = "identifier", Header = "Identifier", Width = "8rem", IsMonospace = true },
                new() { Key = "status", Header = "Status", IsRightAligned = true }
            ];
        }

        /// <summary>
        /// Gets sample table rows.
        /// </summary>
        /// <returns>The sample table rows.</returns>
        private static IReadOnlyList<IReadOnlyDictionary<string, string>> GetRows()
        {
            return
            [
                new Dictionary<string, string>
                {
                    ["type"] = "Requirement",
                    ["identifier"] = "REQ-001"
                }
            ];
        }
    }
}
