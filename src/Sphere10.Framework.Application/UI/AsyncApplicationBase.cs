// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.Framework.Application.UI;

/// <summary>Awaits application lifecycle work and supplies blocking adapters for synchronous consumers.</summary>
public abstract class AsyncApplicationBase : ApplicationBase {
	protected AsyncApplicationBase() {
	}

	protected AsyncApplicationBase(IServiceProvider services, string settingsKey = null)
		: base(services, settingsKey) {
	}

	public override void Initialize() {
		EnsureCanInitializeSynchronously();
		Task.ExecuteSyncOverAsync(() => InitializeAsync());
	}

	public override Task InitializeAsync(CancellationToken cancellationToken = default)
		=> InitializeCore(OnInitializeAsync, token => Tools.Application.ExecuteInitializersAsync(Initializers, Logger, ReportProgress, token), cancellationToken);

#pragma warning disable CS0465 // Implements the existing synchronous application lifecycle contract.
	public override void Finalize() {
		EnsureCanFinalizeSynchronously();
		Task.ExecuteSyncOverAsync(() => FinalizeAsync());
	}
#pragma warning restore CS0465

	public override Task FinalizeAsync(CancellationToken cancellationToken = default)
		=> FinalizeCore(() => OnFinalizeAsync(CancellationToken.None), () => Tools.Application.ExecuteFinalizersAsync(Finalizers, Logger, ReportProgress), resource => {
			if (resource is IAsyncDisposable asyncResource)
				return asyncResource.DisposeAsync().AsTask();
			resource.Dispose();
			return Task.CompletedTask;
		}, cancellationToken);

	public override bool Exit(bool force = false, CancellationToken cancellationToken = default) {
		cancellationToken.ThrowIfCancellationRequested();
		EnsureCanExitSynchronously();
		return Task.ExecuteSyncOverAsync(() => ExitAsync(force, cancellationToken), cancellationToken);
	}

	public override Task<bool> ExitAsync(bool force = false, CancellationToken cancellationToken = default)
		=> ExitCore(force, cancellationToken, ConfirmExitAsync, CanExitAsync, OnExitAsync, token => ExitAsync(true, token));

	protected override bool ConfirmExit(CancellationToken cancellationToken)
		=> Task.ExecuteSyncOverAsync(() => ConfirmExitAsync(cancellationToken), cancellationToken);

	protected override Task<bool> ConfirmExitAsync(CancellationToken cancellationToken) {
		cancellationToken.ThrowIfCancellationRequested();
		Guard.Ensure(ScreenHost?.UserInterfaceServices != null, "Application exit requires user interface services for confirmation.");
		return ScreenHost.UserInterfaceServices.AskYNAsync("Are you sure you want to exit?", cancellationToken);
	}

	protected override bool CanExit(CancellationToken cancellationToken)
		=> Task.ExecuteSyncOverAsync(() => CanExitAsync(cancellationToken), cancellationToken);

	protected override async Task<bool> CanExitAsync(CancellationToken cancellationToken) {
		cancellationToken.ThrowIfCancellationRequested();
		var host = ScreenHost;
		if (host == null)
			return true;
		if (!await host.CanNavigateAsync(cancellationToken))
			return false;
		var screenBlock = host.ActiveScreen?.Block;
		if (screenBlock != null && !await screenBlock.CanExitAsync(this, cancellationToken))
			return false;
		var selectedBlock = host.SelectedBlock;
		return selectedBlock == null || ReferenceEquals(selectedBlock, screenBlock) || await selectedBlock.CanExitAsync(this, cancellationToken);
	}

	protected override bool OnExit(bool force, CancellationToken cancellationToken)
		=> Task.ExecuteSyncOverAsync(() => OnExitAsync(force, cancellationToken), cancellationToken);

	protected override async Task<bool> OnExitAsync(bool force, CancellationToken cancellationToken) {
		cancellationToken.ThrowIfCancellationRequested();
		try {
			await FinalizeAsync();
		} catch (Exception error) {
			if (!IsFinalizationCompleted)
				throw;
			Tools.Exceptions.ExecuteIgnoringException(() => Logger.Exception(error));
		}
		return true;
	}

	protected override bool OnInitialize()
		=> Task.ExecuteSyncOverAsync(() => OnInitializeAsync(CancellationToken.None));

	protected override void OnFinalize()
		=> Task.ExecuteSyncOverAsync(() => OnFinalizeAsync(CancellationToken.None));

	protected override void DisposeInternal() {
		EnsureCanDisposeSynchronously();
		Task.ExecuteSyncOverAsync(() => DisposeInternalAsync().AsTask());
	}

	protected override ValueTask DisposeInternalAsync() => new(DisposeCore(() => FinalizeAsync()));
}
