// ------------------------------------------------------------------------------------------------
// <copyright file="ModelOpeningViewModel.cs" company="Starion Group S.A.">
//
//   Copyright 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace Mycelium.Bloom.ViewModel
{
    using Mycelium.Bloom.Core.Context;
    using Mycelium.Bloom.Model;
    using ReactiveUI;

    /// <summary>Owns bounded browser uploads and session-only model replacement.</summary>
    public sealed class ModelOpeningViewModel(
        WorkspaceModelSession session,
        ILogger<ModelOpeningViewModel> logger,
        WorkspaceConnectionHandler connection,
        IHostEnvironment environment) : ReactiveObject, IDisposable
    {
        private readonly object gate = new();
        private CancellationTokenSource operation;
        private bool disposed;
        private WorkspaceModelLease openedContext;

        /// <summary>Transfers the accepted context to the invoking tab composition.</summary>
        /// <returns>The retained context, or null when no opening completed.</returns>
        public WorkspaceModelLease TakeOpenedContext()
        {
            lock (this.gate)
            {
                var result = this.openedContext;
                this.openedContext = null;
                return result;
            }
        }

        /// <summary>Gets the upload limit in bytes.</summary>
        public const long MaximumBytes = 67_108_864;

        /// <summary>Gets the coherent operation state.</summary>
        public ModelOpeningState State
        {
            get;
            private set => this.RaiseAndSetIfChanged(ref field, value);
        } = new();

        /// <summary>Opens the explicitly selected bundled structural example through upload validation and candidate acceptance.</summary>
        /// <param name="independent">Retains an additive tab-owned context instead of replacing the default workspace.</param>
        /// <returns>Whether the example became the active session model.</returns>
        public async Task<bool> OpenExampleAsync(bool independent = false)
        {
            try
            {
                var path = Path.Combine(environment.ContentRootPath, "Resources", "Prototypes", "Interconnection.json");
                return await this.OpenAsync("Interconnection.json", new FileInfo(path).Length, _ => File.OpenRead(path),
                    independent);
            }
            catch (IOException error)
            {
                logger.LogWarning(error, "The example model could not be read");
                this.Publish(
                    new(Error: "The example model could not be opened. Please try again or choose a JSON file."));
                return false;
            }
        }

        /// <summary>Uploads a selected file or returns to the bundled Quantities library.</summary>
        /// <param name="name">The display name, used only for extension validation.</param>
        /// <param name="size">The declared upload size.</param>
        /// <param name="openStream">Opens the browser upload, or null for Quantities.</param>
        /// <param name="independent">Retains an additive tab-owned context instead of replacing the default workspace.</param>
        /// <returns>Whether the model was made current.</returns>
        public Task<bool> OpenAsync(string name, long size, Func<CancellationToken, Stream> openStream,
            bool independent = false) =>
            this.OpenStreamAsync(name, size,
                openStream is null ? null : token => ValueTask.FromResult(openStream(token)), independent);

        /// <summary>Owns asynchronous acquisition, bounded reading and disposal of the upload stream.</summary>
        /// <param name="name">The display name used for format validation.</param>
        /// <param name="size">The declared upload size.</param>
        /// <param name="openStream">Acquires the operation-owned stream.</param>
        /// <param name="independent">Whether to retain an additive context.</param>
        /// <returns>Whether the model was accepted.</returns>
        public async Task<bool> OpenStreamAsync(string name, long size,
            Func<CancellationToken, ValueTask<Stream>> openStream, bool independent = false)
        {
            CancellationTokenSource cancellation;
            Guid context;
            lock (this.gate)
            {
                if (this.disposed || this.operation is not null)
                {
                    return false;
                }

                if (openStream is not null &&
                    (!string.Equals(Path.GetExtension(name), ".json", StringComparison.OrdinalIgnoreCase)
                     || size is <= 0 or > MaximumBytes))
                {
                    this.State = new(Error: "Select a non-empty .json SysML v2 model up to 64 MiB.");
                    return false;
                }

                context = session.Current.Id;
                this.operation = cancellation = connection.CreateUploadCancellation();
                this.State = new(true, openStream is null ? "Opening Quantities…" : "Uploading model…");
            }

            try
            {
                bool opened;
                if (openStream is null)
                {
                    opened = await session.OpenUploadedAsync(null, cancellation.Token, context);
                }
                else
                {
                    var path = Path.Combine(Path.GetTempPath(), "bloom-model-" + Path.GetRandomFileName());
                    var temporaryOptions = new FileStreamOptions
                    {
                        Mode = FileMode.CreateNew,
                        Access = FileAccess.ReadWrite,
                        Share = FileShare.None,
                        BufferSize = 65_536,
                        Options = FileOptions.Asynchronous | FileOptions.DeleteOnClose
                    };
                    if (!OperatingSystem.IsWindows())
                    {
                        temporaryOptions.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
                    }

                    await using var temporary = new FileStream(path, temporaryOptions);
                    await using var source = await openStream(cancellation.Token);
                    var buffer = new byte[65_536];
                    long total = 0;
                    var reported = -1;
                    int read;
                    while ((read = await source.ReadAsync(buffer, cancellation.Token)) != 0)
                    {
                        total += read;
                        if (total > size || total > MaximumBytes)
                        {
                            throw new InvalidDataException("The uploaded file exceeds its declared size.");
                        }

                        await temporary.WriteAsync(buffer.AsMemory(0, read), cancellation.Token);
                        var percent = (int)(total * 100 / size);
                        if (percent != reported)
                        {
                            reported = percent;
                            this.Publish(new(true, "Uploading model…", percent));
                        }
                    }

                    if (total != size)
                    {
                        throw new InvalidDataException("The upload was incomplete. Please select the file again.");
                    }

                    await temporary.FlushAsync(cancellation.Token);
                    this.Publish(new(true, "Validating and opening model…", 100));
                    if (independent)
                    {
                        var accepted = await session.OpenContextAsync(temporary, cancellation.Token);
                        lock (this.gate)
                        {
                            if (this.disposed || cancellation.IsCancellationRequested)
                            {
                                accepted.Dispose();
                                throw new OperationCanceledException(cancellation.Token);
                            }

                            this.openedContext?.Dispose();
                            this.openedContext = accepted;
                        }

                        opened = true;
                    }
                    else
                    {
                        opened = await session.OpenUploadedAsync(temporary, cancellation.Token, context);
                    }
                }

                this.Publish(opened
                    ? new(Status: "Model opened for this session.")
                    : new(Error: "The workspace changed or opening was cancelled. Please try again."));
                return opened;
            }
            catch (OperationCanceledException)
            {
                this.Publish(new(Status: "Opening cancelled. The upload was not made current."));
                return false;
            }
            catch (Exception error)
            {
                logger.LogWarning(error, "Session model opening failed");
                this.Publish(new(Error: error is InvalidDataException
                    ? error.Message
                    : "The model could not be opened. Check that it is a complete SysML v2 JSON model and try again."));
                return false;
            }
            finally
            {
                lock (this.gate)
                {
                    this.operation = null;
                }

                cancellation.Dispose();
            }
        }

        /// <summary>Cancels upload or prevents an obsolete result from becoming current.</summary>
        public void Cancel()
        {
            lock (this.gate)
            {
                this.operation?.Cancel();
            }
        }

        /// <summary>Cancels owned work and rejects late UI publications.</summary>
        public void Dispose()
        {
            lock (this.gate)
            {
                this.disposed = true;
                this.operation?.Cancel();
                this.openedContext?.Dispose();
                this.openedContext = null;
            }
        }

        private void Publish(ModelOpeningState state)
        {
            lock (this.gate)
            {
                if (!this.disposed)
                {
                    this.State = state;
                }
            }
        }
    }
}