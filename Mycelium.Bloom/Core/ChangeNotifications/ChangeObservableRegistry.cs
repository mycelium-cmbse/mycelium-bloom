// ------------------------------------------------------------------------------------------------
// <copyright file="ChangeObservableRegistry.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Core.ChangeNotifications
{
    using System.Reactive.Disposables;
    using System.Reactive.Linq;
    using System.Reactive.Subjects;
    using Microsoft.Extensions.Logging;

    /// <summary>Owns synchronous target registration, subject lifetimes and isolated subscriber delivery.</summary>
    /// <param name="logger">The application logger for isolated callback failures.</param>
    internal sealed class ChangeObservableRegistry(ILogger<ChangeNotificationService> logger)
    {
        /// <summary>Protects registration and delivery leases without invoking consumers.</summary>
        private readonly object gate = new();

        /// <summary>Stores one canonical subject per subscribed target.</summary>
        private readonly Dictionary<ChangeTarget, ChangeObservable> observables = new();

        /// <summary>Retains subjects until their serialized completion runs.</summary>
        private List<(ChangeObservable Channel, IDisposable Lease)> completing = [];

        /// <summary>Rejects subscriptions after the service stops.</summary>
        private bool isStopped;

        /// <summary>Gets the number of materialized target subjects.</summary>
        internal int Count
        {
            get
            {
                lock (this.gate)
                {
                    return this.observables.Count;
                }
            }
        }

        /// <summary>Reacquires the canonical target on every subscription, including saved-stream reuse.</summary>
        /// <param name="target">The validated target.</param>
        /// <returns>The deferred shared stream.</returns>
        internal IObservable<ChangeEvent> Listen(ChangeTarget target)
        {
            return Observable.Create<ChangeEvent>(observer =>
            {
                lock (this.gate)
                {
                    ObjectDisposedException.ThrowIf(this.isStopped, typeof(ChangeNotificationService));
                    if (!this.observables.TryGetValue(target, out var channel))
                    {
                        channel = this.CreateObservable(target);
                        this.observables.Add(target, channel);
                    }
                    var subscription = channel.Observable.Subscribe(
                        change => this.InvokeSubscriber(() => observer.OnNext(change)),
                        error => this.InvokeSubscriber(() => observer.OnError(error)),
                        () => this.InvokeSubscriber(observer.OnCompleted));
                    return Disposable.Create(() =>
                    {
                        lock (this.gate)
                        {
                            subscription.Dispose();
                        }
                    });
                }
            });
        }

        /// <summary>Delivers a frozen event outside all registry synchronization.</summary>
        /// <param name="change">The frozen event.</param>
        /// <param name="canDeliver">The owner's validity check, called outside the registry gate.</param>
        internal void Deliver(ChangeEvent change, Func<bool> canDeliver)
        {
            foreach (var target in GetTargets(change))
            {
                if (!canDeliver())
                {
                    return;
                }
                ChangeObservable channel;
                IDisposable lease;
                lock (this.gate)
                {
                    if (!this.observables.TryGetValue(target, out channel))
                    {
                        continue;
                    }
                    lease = channel.Lifetime.GetDisposable();
                }
                using (lease)
                {
                    channel.Subject.OnNext(change);
                }
            }
        }

        /// <summary>Closes registration while retaining subjects for scheduled completion.</summary>
        internal void Stop()
        {
            lock (this.gate)
            {
                this.isStopped = true;
                foreach (var channel in this.observables.Values)
                {
                    this.completing.Add((channel, channel.Lifetime.GetDisposable()));
                    channel.Lifetime.Dispose();
                }
                this.observables.Clear();
            }
        }

        /// <summary>Completes retired subjects on the service's serialized delivery context.</summary>
        internal void Complete()
        {
            List<(ChangeObservable Channel, IDisposable Lease)> retired;
            lock (this.gate)
            {
                retired = this.completing;
                this.completing = [];
            }
            try
            {
                foreach (var (channel, _) in retired)
                {
                    channel.Subject.OnCompleted();
                }
            }
            finally
            {
                foreach (var (_, lease) in retired)
                {
                    lease.Dispose();
                }
            }
        }

        /// <summary>Composes shared subscriptions with final-subscriber removal.</summary>
        /// <param name="target">The canonical key.</param>
        /// <returns>The newly materialized entry.</returns>
        private ChangeObservable CreateObservable(ChangeTarget target)
        {
            var subject = new Subject<ChangeEvent>();
            var lifetime = new RefCountDisposable(subject);
            var observable = Observable.Create<ChangeEvent>(observer =>
                new CompositeDisposable(subject.Subscribe(observer),
                    Disposable.Create(() => this.Remove(target, subject))))
                .Publish().RefCount();
            return new ChangeObservable(subject, observable, lifetime);
        }

        /// <summary>Removes only the disconnected canonical subject.</summary>
        /// <param name="target">The disconnected key.</param>
        /// <param name="subject">The subject whose final subscriber detached.</param>
        private void Remove(ChangeTarget target, Subject<ChangeEvent> subject)
        {
            lock (this.gate)
            {
                if (this.observables.TryGetValue(target, out var current) && ReferenceEquals(current.Subject, subject))
                {
                    this.observables.Remove(target);
                    current.Lifetime.Dispose();
                }
            }
        }

        /// <summary>Isolates each consumer before its failure reaches peers or the scheduler.</summary>
        /// <param name="callback">The individual consumer callback.</param>
        private void InvokeSubscriber(Action callback)
        {
            try
            {
                callback();
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "A change notification subscriber failed.");
            }
        }

        /// <summary>Builds direct lookup keys from immutable event identity, containment and metaclass ancestry.</summary>
        /// <param name="changeEvent">The event being dispatched.</param>
        /// <returns>Distinct global, element, subtree and metaclass targets.</returns>
        private static IEnumerable<ChangeTarget> GetTargets(ChangeEvent changeEvent)
        {
            yield return ChangeTarget.Global;
            yield return ChangeTarget.Element(changeEvent.ElementId);
            yield return ChangeTarget.Subtree(changeEvent.ElementId);

            var namespaces = changeEvent.Containment.NamespaceIds.AsEnumerable();

            if (changeEvent.PreviousContainment != null)
            {
                namespaces = namespaces.Concat(changeEvent.PreviousContainment.NamespaceIds);
            }

            foreach (var namespaceId in namespaces.Distinct())
            {
                yield return ChangeTarget.Subtree(namespaceId);
            }

            foreach (var type in SysmlMetaclass.GetHierarchy(changeEvent.ElementType))
            {
                yield return ChangeTarget.ForType(type);
            }
        }

    }
}
