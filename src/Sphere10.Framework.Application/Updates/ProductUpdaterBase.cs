// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.Framework.Application;

/// <summary>
/// Owns the whole update of a product through one channel: <see cref="CheckForUpdateAsync"/> finds the version this installation can
/// upgrade to and <see cref="InstallAsync"/> installs it. A channel derives from it and implements <see cref="FetchUpdateAsync"/> and
/// <see cref="InstallUpdateAsync"/>.
/// </summary>
/// <remarks>
/// <para>
/// The application registers one updater as <see cref="ProductUpdaterBase"/>. The Application module registers
/// <see cref="ReleaseServerProductUpdater"/>, which asks the application's <see cref="IProductUpdateService"/>, when the host registered
/// none. A platform registers the release-server updater that can install, and a distribution's module replaces the registration with
/// its own channel, as the Windows Store library does. <see cref="ProductUpdateChecker"/> checks through it and the platform's notice
/// installs through it.
/// </para>
/// <para>
/// A check may run on any thread. An installation starts on the caller's thread, so a channel that shows its own consent prompt shows it
/// from the notice's thread, and one installation runs at a time. A failed installation is logged and rethrown, and a cancelled one is
/// neither logged nor reported. <see cref="InstallProgressChanged"/> may be raised on any thread.
/// </para>
/// </remarks>
public abstract class ProductUpdaterBase {

	public event EventHandlerEx<ProductUpdateProgress> InstallProgressChanged;

	private int _installing;

	protected ProductUpdaterBase(ILogger logger = null) => Logger = logger ?? new NoOpLogger();

	protected ILogger Logger { get; }

	/// <summary>
	/// Asks the channel for the version this installation can upgrade to, and completes with <see cref="ProductUpdateCheckResult.NoUpdate"/>
	/// when there is none. A failed request throws, and the token cancels it.
	/// </summary>
	public async Task<ProductUpdateCheckResult> CheckForUpdateAsync(CancellationToken cancellationToken = default) {
		cancellationToken.ThrowIfCancellationRequested();
		return await FetchUpdateAsync(cancellationToken).ConfigureAwait(false) ?? ProductUpdateCheckResult.NoUpdate;
	}

	/// <summary>
	/// Installs an update this updater found. It completes when the channel has taken the update over, and answers whether the
	/// application must now exit (<see cref="ProductUpdateInstallResult.ExitRequired"/>), whether the channel installed it and restarts the
	/// application itself (<see cref="ProductUpdateInstallResult.Completed"/>) or whether the user declined in the channel's own prompt.
	/// </summary>
	/// <param name="update">The update a check of this updater answered.</param>
	/// <param name="ownerWindow">The native window that owns any prompt the installation shows, or zero for none.</param>
	/// <param name="cancellationToken">Cancels the installation while the channel still allows it.</param>
	public async Task<ProductUpdateInstallResult> InstallAsync(ProductUpdate update, nint ownerWindow = default, CancellationToken cancellationToken = default) {
		Guard.ArgumentNotNull(update, nameof(update));
		cancellationToken.ThrowIfCancellationRequested();
		Guard.Ensure(Interlocked.Exchange(ref _installing, 1) == 0, "An update is already being installed.");
		using (Tools.Scope.ExecuteOnDispose(() => Interlocked.Exchange(ref _installing, 0))) {
			try {
				return await InstallUpdateAsync(update, ownerWindow, cancellationToken).ConfigureAwait(false);
			} catch (Exception error) when (error is not OperationCanceledException) {
				Logger.Exception(error, $"The update to version {update.Version} could not be installed.");
				throw;
			}
		}
	}

	/// <summary>Asks the channel for the update this installation can upgrade to. Null is read as no update.</summary>
	protected abstract Task<ProductUpdateCheckResult> FetchUpdateAsync(CancellationToken cancellationToken);

	/// <summary>Installs an update of this channel, reporting its progress through <see cref="NotifyInstallProgressChanged"/>.</summary>
	protected abstract Task<ProductUpdateInstallResult> InstallUpdateAsync(ProductUpdate update, nint ownerWindow, CancellationToken cancellationToken);

	protected void NotifyInstallProgressChanged(ProductUpdateProgress progress) {
		OnInstallProgressChanged(progress);
		InstallProgressChanged?.Invoke(progress);
	}

	protected virtual void OnInstallProgressChanged(ProductUpdateProgress progress) {
	}
}
