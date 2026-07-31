// ------------------------------------------------------------------------------------------------
// <copyright file="SymbolIconNameExtensions.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Model.Enum
{
    /// <summary>
    /// Maps application-owned symbol identifiers to Lucide icon names.
    /// </summary>
    public static class SymbolIconNameExtensions
    {
        /// <summary>
        /// Gets the Lucide icon name for an application symbol.
        /// </summary>
        /// <param name="symbol">The application-owned symbol identifier.</param>
        /// <returns>The corresponding Lucide icon name.</returns>
        public static string ToLucideName(this SymbolIconName symbol)
        {
            return symbol switch
            {
                SymbolIconName.Home => "house",
                SymbolIconName.ComponentGrid => "grid-2x2",
                SymbolIconName.Document => "file-text",
                SymbolIconName.MoreHorizontal => "ellipsis",
                SymbolIconName.Inspect => "eye",
                SymbolIconName.Copy => "copy",
                SymbolIconName.User => "user",
                SymbolIconName.Preferences => "settings-2",
                SymbolIconName.SignOut => "log-out",
                SymbolIconName.Delete => "trash-2",
                SymbolIconName.Menu => "menu",
                SymbolIconName.Close => "x",
                SymbolIconName.Share => "share-2",
                SymbolIconName.Undo => "undo-2",
                SymbolIconName.Cursor => "mouse-pointer-2",
                SymbolIconName.Note => "sticky-note",
                SymbolIconName.Edit => "pencil",
                SymbolIconName.Link => "link-2",
                SymbolIconName.Move => "move",
                SymbolIconName.Center => "focus",
                SymbolIconName.Minus => "minus",
                SymbolIconName.Plus => "plus",
                SymbolIconName.ActualSize => "scan-line",
                SymbolIconName.Fit => "maximize",
                SymbolIconName.Information => "info",
                SymbolIconName.Check => "check",
                _ => "circle-help"
            };
        }
    }
}
