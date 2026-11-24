// ------------------------------------------------------------------------------------------------
// <copyright file="DiagramPointerBehavior.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Components.UI.Organisms.Diagram
{
    using System.Reactive.Disposables;
    using System.Reactive.Linq;

    using Blazor.Diagrams.Core;
    using Blazor.Diagrams.Core.Behaviors;
    using Blazor.Diagrams.Core.Events;
    using Blazor.Diagrams.Core.Models.Base;

    using Microsoft.AspNetCore.Components;

    using Mycelium.Bloom.Components.UI.Organisms.Diagram.Tools;
    using Mycelium.Bloom.ViewModel.Diagram;
    using Mycelium.Bloom.ViewModel.Diagram.Tools;

    using ReactiveUI;

    using BrowserKeyboardEventArgs = Microsoft.AspNetCore.Components.Web.KeyboardEventArgs;

    /// <summary>Routes native input to the tool runtimes for one mounted diagram.</summary>
    public sealed class DiagramPointerBehavior : Behavior, IDiagramPointerBehavior
    {
        /// <summary>The tool selection owned by the tab.</summary>
        private readonly IDiagramToolState tools;

        /// <summary>The native context shared by the mounted runtimes.</summary>
        private readonly DiagramToolContext context;

        /// <summary>Runtimes created on first use and released with the renderer.</summary>
        private readonly Dictionary<DiagramToolPlacement, IDiagramTool> runtimes = new();

        /// <summary>Resolves runtime factories without owning their instances.</summary>
        private readonly IDiagramToolRuntimeProvider runtimeProvider;

        /// <summary>Owns observation of tab state for this mount.</summary>
        private readonly CompositeDisposable toolSubscriptions = new();

        /// <summary>The currently activated runtime.</summary>
        private IDiagramTool current;

        /// <summary>The starting event of the accepted pointer gesture.</summary>
        private PointerEventArgs pointerStart;

        /// <summary>Whether the physical Space key remains held.</summary>
        private bool spaceHeld;

        /// <summary>The focused selection target eligible for activation when Space is released.</summary>
        private DiagramElementViewModel spaceActivationTarget;

        /// <summary>Prevents stale events from changing tab state after this mount is disposed.</summary>
        private bool disposed;

        /// <summary>Notifies the canvas of runtime presentation changes.</summary>
        public event Action Changed;

        /// <summary>Gets the runtime cursor for the canvas.</summary>
        public string Cursor => this.current?.Cursor(this.pointerStart is not null);

        /// <summary>Gets the runtime preview for the native diagram layer.</summary>
        public RenderFragment Preview => this.current?.Preview;

        /// <summary>Gets whether a gesture or temporary tool owns input.</summary>
        public bool IsInteracting => this.pointerStart is not null || this.tools.Temporary is not null;

        /// <summary>Installs routing ahead of the native movable behavior.</summary>
        /// <param name="diagram">The mounted native graph.</param>
        /// <param name="owner">The tab-owned presentation authority.</param>
        /// <param name="runtimeProvider">The registry of renderer runtime factories.</param>
        public DiagramPointerBehavior(Diagram diagram, IDiagramViewModel owner, IDiagramToolRuntimeProvider runtimeProvider) : base(diagram)
        {
            ArgumentNullException.ThrowIfNull(owner);
            ArgumentNullException.ThrowIfNull(runtimeProvider);
            this.tools = owner.Tools;
            this.runtimeProvider = runtimeProvider;
            this.context = new DiagramToolContext(diagram, owner, model => model switch
            {
                DiagramNodeModel node => node.ViewModel,
                DiagramLinkModel link => link.ViewModel,
                IDiagramBoundaryPortModel port => port.ViewModel,
                _ => null
            });
            this.toolSubscriptions.Add(this.tools.WhenAnyValue(state => state.Active).Skip(1).Subscribe(_ =>
            {
                this.Cancel();
                this.ApplyTool();
            }));
            this.toolSubscriptions.Add(this.tools.WhenAnyValue(state => state.Temporary).Subscribe(_ => this.ApplyTool()));
            this.toolSubscriptions.Add(this.tools.WhenAnyValue(state => state.CanEdit).Skip(1)
                .Where(_ => this.IsInteracting && this.tools.CanActivate(this.tools.Active)).Subscribe(_ => this.Cancel()));
            diagram.PointerDown += this.PointerDown;
            diagram.PointerMove += this.PointerMove;
            diagram.PointerUp += this.PointerUp;
        }

        /// <summary>Records a focused selection target without activating it on Space keydown.</summary>
        /// <param name="args">The key press captured by the focusable element before it bubbles to the canvas.</param>
        /// <param name="target">The element that can be activated by a Space tap.</param>
        public void PrepareSpaceActivation(BrowserKeyboardEventArgs args, DiagramElementViewModel target)
        {
            if (!this.disposed && !this.spaceHeld && this.pointerStart is null
                && args is { Code: "Space", Repeat: false, AltKey: false, CtrlKey: false, MetaKey: false, ShiftKey: false })
            {
                this.spaceActivationTarget = target;
            }
        }

        /// <summary>Discards a pending activation when its focused element loses focus.</summary>
        /// <param name="target">The element that lost focus.</param>
        public void CancelSpaceActivation(DiagramElementViewModel target)
        {
            if (ReferenceEquals(this.spaceActivationTarget, target))
            {
                this.spaceActivationTarget = null;
            }
        }

        /// <summary>Routes keyboard input from the focused canvas.</summary>
        /// <param name="key">The browser key.</param>
        /// <param name="code">The physical key code.</param>
        /// <param name="pressed">Whether the key was pressed or released.</param>
        /// <param name="modified">Whether a shortcut modifier is held.</param>
        public void KeyChanged(string key, string code, bool pressed, bool modified = false)
        {
            if (this.disposed)
            {
                return;
            }

            if (code == "Space" && (!modified || !pressed))
            {
                this.ChangeSpace(pressed);
                return;
            }

            if (pressed && key == "Escape")
            {
                this.Cancel();
                return;
            }

            if (!pressed || modified || !this.TryActivateShortcut(key))
            {
                this.current?.KeyChanged(key, pressed);
                this.Changed?.Invoke();
            }
        }

        /// <summary>Cancels the specific pointer whose browser gesture was interrupted.</summary>
        /// <param name="pointer">The interrupted pointer.</param>
        /// <returns>Whether the interrupted pointer belonged to this dispatcher.</returns>
        public bool CancelPointer(long pointer)
        {
            if (pointer != this.pointerStart?.PointerId)
            {
                return false;
            }

            this.Cancel();
            return true;
        }

        /// <summary>Cancels interaction and restores the retained tool.</summary>
        public void Cancel()
        {
            if (this.disposed)
            {
                return;
            }

            var start = this.pointerStart;
            this.pointerStart = null;
            this.spaceHeld = false;
            this.spaceActivationTarget = null;

            this.current?.Cancel(start);
            this.tools.Temporary = null;
            this.Changed?.Invoke();
        }

        /// <summary>Detaches input and disposes all runtime instances owned by this mount.</summary>
        public override void Dispose()
        {
            if (this.disposed)
            {
                return;
            }

            this.toolSubscriptions.Dispose();
            this.Cancel();
            this.disposed = true;
            this.Diagram.PointerDown -= this.PointerDown;
            this.Diagram.PointerMove -= this.PointerMove;
            this.Diagram.PointerUp -= this.PointerUp;
            this.current?.Deactivate();

            foreach (var runtime in this.runtimes.Values)
            {
                runtime.Dispose();
            }

            this.runtimes.Clear();
            this.current = null;
        }

        /// <summary>Applies the temporary navigation override and completes an unconsumed Space tap.</summary>
        /// <param name="pressed">Whether Space is held.</param>
        private void ChangeSpace(bool pressed)
        {
            var activation = !pressed && this.spaceHeld ? this.spaceActivationTarget : null;
            this.spaceHeld = pressed;

            if (!pressed)
            {
                this.spaceActivationTarget = null;
            }

            if (this.pointerStart is null)
            {
                this.tools.Temporary = pressed ? this.tools.Navigation : null;
                activation?.Select();
            }
        }

        /// <summary>Activates an enabled tool whose layout shortcut matches the pressed key.</summary>
        /// <param name="key">The unmodified browser key.</param>
        /// <returns>Whether a toolbox shortcut was handled.</returns>
        private bool TryActivateShortcut(string key)
        {
            var shortcut = this.tools.Toolbox.FirstOrDefault(entry => this.tools.CanActivate(entry)
                && string.Equals(entry.Item.Shortcut ?? entry.Tool.DefaultShortcut, key, StringComparison.OrdinalIgnoreCase));

            return this.tools.TryActivate(shortcut);
        }

        /// <summary>Accepts one primary gesture before the native drag listener runs.</summary>
        /// <param name="model">The native hit model.</param>
        /// <param name="args">The native pointer event.</param>
        private void PointerDown(Model model, PointerEventArgs args)
        {
            if (this.pointerStart is not null)
            {
                this.Cancel();
            }

            if (!args.IsPrimary || args.Button is not (0 or 1))
            {
                return;
            }

            this.spaceActivationTarget = null;

            if (args.Button == 1)
            {
                this.tools.Temporary = this.tools.Navigation;

                if (this.tools.Temporary is null)
                {
                    return;
                }
            }

            this.pointerStart = args;
            this.current?.PointerDown(model, args);
            this.Changed?.Invoke();
        }

        /// <summary>Routes motion and hover input to the active runtime.</summary>
        /// <param name="model">The native hit model.</param>
        /// <param name="args">The native pointer event.</param>
        private void PointerMove(Model model, PointerEventArgs args)
        {
            if (this.pointerStart is { } start && start.PointerId != args.PointerId)
            {
                this.Cancel();
                return;
            }

            var preview = this.Preview;
            var cursor = this.Cursor;
            this.current?.PointerMove(model, args);

            if (preview is not null || this.Preview is not null || cursor != this.Cursor)
            {
                this.Changed?.Invoke();
            }
        }

        /// <summary>Finishes a gesture and restores the retained tool unless Space remains held.</summary>
        /// <param name="model">The native hit model.</param>
        /// <param name="args">The native pointer event.</param>
        private void PointerUp(Model model, PointerEventArgs args)
        {
            if (this.pointerStart is null)
            {
                return;
            }

            if (this.pointerStart.PointerId != args.PointerId)
            {
                this.Cancel();
                return;
            }

            this.pointerStart = null;
            this.current?.PointerUp(model, args);
            this.tools.Temporary = this.spaceHeld ? this.tools.Navigation : null;
            this.Changed?.Invoke();
        }

        /// <summary>Activates the effective tool, creating its runtime on first use.</summary>
        private void ApplyTool()
        {
            IDiagramTool next = null;

            if (this.tools.Effective is { } placement && !this.runtimes.TryGetValue(placement, out next))
            {
                next = this.runtimeProvider.Create(placement, this.context);
                this.runtimes.Add(placement, next);
            }

            if (!ReferenceEquals(this.current, next))
            {
                this.current?.Deactivate();
                this.current = next;
                this.current?.Activate();
            }

            this.Changed?.Invoke();
        }
    }
}
