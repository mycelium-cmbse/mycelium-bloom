// ------------------------------------------------------------------------------------------------
// <copyright file="CallbackLogger.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.Tests.Common
{
    using System;
    using System.Reactive.Disposables;
    using Microsoft.Extensions.Logging;

    internal sealed class CallbackLogger<T>(LogLevel enabledLevel, Action<LogLevel, EventId, string, Exception> callback)
        : ILogger<T>
    {
        public IDisposable BeginScope<TState>(TState state) where TState : notnull
        {
            return Disposable.Empty;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel == enabledLevel;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
            Func<TState, Exception, string> formatter)
        {
            if (this.IsEnabled(logLevel))
            {
                callback(logLevel, eventId, formatter(state, exception), exception);
            }
        }
    }
}
