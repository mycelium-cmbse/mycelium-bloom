// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramCanvasHost.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Organisms.Interconnection
{
    using Blazor.Diagrams.Components;

    /// <summary>Orders library canvas initialization before its observer and interop-reference cleanup.</summary>
    public sealed class DiagramCanvasHost : DiagramCanvas, IAsyncDisposable
    {
        /// <summary>The library's in-flight first-render initialization.</summary>
        private Task initialization = Task.CompletedTask;

        /// <summary>The shared asynchronous cleanup operation.</summary>
        private Task disposal;

        /// <summary>Prevents another render from starting after cleanup begins.</summary>
        private bool disposed;

        /// <summary>Tracks the library's initial bounds query and observer registration.</summary>
        /// <param name="firstRender">Whether this is the initial interactive render.</param>
        /// <returns>Completion of library canvas initialization.</returns>
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (this.disposed)
            {
                return;
            }

            if (firstRender)
            {
                this.initialization = base.OnAfterRenderAsync(true);
                await this.initialization;
            }
            else
            {
                await base.OnAfterRenderAsync(false);
            }
        }

        /// <summary>Releases the library resources after any pending registration completes.</summary>
        /// <returns>The idempotent cleanup operation.</returns>
        public new ValueTask DisposeAsync() => new(this.disposal ??= this.DisposeCoreAsync());

        /// <summary>Awaits initial registration before disposing the library reference.</summary>
        /// <returns>Completion of registration and owned library cleanup.</returns>
        private async Task DisposeCoreAsync()
        {
            this.disposed = true;
            try
            {
                await this.initialization;
            }
            finally
            {
                await base.DisposeAsync();
            }
        }
    }
}
