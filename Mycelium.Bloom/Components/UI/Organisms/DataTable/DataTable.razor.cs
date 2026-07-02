// ------------------------------------------------------------------------------------------------
// <copyright file="DataTable.razor.cs" company="Starion Group S.A.">
// 
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
// 
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Organisms.DataTable
{
    using Microsoft.AspNetCore.Components;

    using Mycelium.Bloom.Components.Common;
    using Mycelium.Bloom.Model;

    /// <summary>
    /// Reusable Bloom data table for compact workspace lists.
    /// </summary>
    public partial class DataTable : ComponentBase
    {
        /// <summary>
        /// Gets or sets the table columns.
        /// </summary>
        [Parameter]
        public IReadOnlyList<DataTableColumn> Columns { get; set; } = [];

        /// <summary>
        /// Gets or sets the table row items.
        /// </summary>
        [Parameter]
        public IReadOnlyList<IReadOnlyDictionary<string, string>> Items { get; set; } = [];

        /// <summary>
        /// Gets or sets the text displayed when no data is available.
        /// </summary>
        [Parameter]
        public string EmptyText { get; set; } = "No data available.";

        /// <summary>
        /// Gets or sets whether the table should use compact density.
        /// </summary>
        [Parameter]
        public bool IsCompact { get; set; } = true;

        /// <summary>
        /// Gets or sets whether the table container should render a border.
        /// </summary>
        [Parameter]
        public bool HasBorder { get; set; } = true;

        /// <summary>
        /// Gets or sets the callback invoked when a row is selected.
        /// </summary>
        [Parameter]
        public EventCallback<IReadOnlyDictionary<string, string>> RowSelected { get; set; }

        /// <summary>
        /// Gets or sets additional CSS classes.
        /// </summary>
        [Parameter]
        public string Class { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets unmatched attributes passed to the data table element.
        /// </summary>
        [Parameter(CaptureUnmatchedValues = true)]
        public IReadOnlyDictionary<string, object> AdditionalAttributes { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets the final CSS class list applied to the data table.
        /// </summary>
        /// <returns>The data table CSS class list.</returns>
        private string GetCssClass()
        {
            var cssClass = CssClassBuilder.Build(
                "mb-data-table",
                CssClassBuilder.When("mb-data-table--compact", this.IsCompact),
                CssClassBuilder.When("mb-data-table--bordered", this.HasBorder),
                this.Class);

            return cssClass;
        }

        /// <summary>
        /// Gets the CSS class for a header cell.
        /// </summary>
        /// <param name="column">The column definition.</param>
        /// <returns>The header cell CSS class.</returns>
        private static string GetHeaderClass(DataTableColumn column)
        {
            var cssClass = CssClassBuilder.Build(
                "mb-data-table__header",
                CssClassBuilder.When("mb-data-table__cell--right", column.IsRightAligned));

            return cssClass;
        }

        /// <summary>
        /// Gets the CSS class for a body cell.
        /// </summary>
        /// <param name="column">The column definition.</param>
        /// <returns>The body cell CSS class.</returns>
        private static string GetCellClass(DataTableColumn column)
        {
            var cssClass = CssClassBuilder.Build(
                "mb-data-table__cell",
                CssClassBuilder.When("mb-data-table__cell--mono", column.IsMonospace),
                CssClassBuilder.When("mb-data-table__cell--right", column.IsRightAligned));

            return cssClass;
        }

        /// <summary>
        /// Gets the inline style for a column width.
        /// </summary>
        /// <param name="column">The column definition.</param>
        /// <returns>The column width style.</returns>
        private static string GetColumnStyle(DataTableColumn column)
        {
            var style = !string.IsNullOrWhiteSpace(column.Width)
                ? $"width: {column.Width};"
                : string.Empty;

            return style;
        }

        /// <summary>
        /// Gets the value displayed for a cell.
        /// </summary>
        /// <param name="item">The row item.</param>
        /// <param name="column">The column definition.</param>
        /// <returns>The cell value.</returns>
        private static string GetCellValue(IReadOnlyDictionary<string, string> item, DataTableColumn column)
        {
            var value = item.TryGetValue(column.Key, out var itemValue)
                ? itemValue
                : string.Empty;

            return value;
        }

        /// <summary>
        /// Gets the number of columns spanned by the empty state cell.
        /// </summary>
        /// <returns>The empty state column span.</returns>
        private int GetColumnSpan()
        {
            var columnSpan = Math.Max(this.Columns.Count, 1);

            return columnSpan;
        }

        /// <summary>
        /// Selects the provided row item.
        /// </summary>
        /// <param name="item">The row item to select.</param>
        private async Task SelectRowAsync(IReadOnlyDictionary<string, string> item)
        {
            await this.RowSelected.InvokeAsync(item);
        }
    }
}
