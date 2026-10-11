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

namespace Sphere10.Framework.Application.UI;

/// <summary>Awaits application lifecycle work and supplies blocking adapters for synchronous consumers.</summary>
public abstract class AsyncApplicationBase : ApplicationBase {
	protected AsyncApplicationBase() {
	}

	protected AsyncApplicationBase(IServiceProvider services)
		: base(services) {
	}

	public override void Initialize() {
		EnsureCanInitializeSynchronously();
		Task.Run(() => InitializeAsync()).GetAwaiter().GetResult();
	}

	public override Task InitializeAsync(CancellationToken cancellationToken = default)
		=> InitializeCore(OnInitializeAsync, token => Tools.Application.ExecuteInitializersAsync(Initializers, Logger, ReportProgress, token), cancellationToken);

#pragma warning disable CS0465 // Implements the existing synchronous application lifecycle contract.
	public override void Finalize() {
		EnsureCanFinalizeSynchronously();
		Task.Run(() => FinalizeAsync()).GetAwaiter().GetResult();
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
		return Task.Run(() => ExitAsync(force, cancellationToken), cancellationToken).GetAwaiter().GetResult();
	}

	public override Task<bool> ExitAsync(bool force = false, CancellationToken cancellationToken = default)
		=> ExitCore(force, cancellationToken, ConfirmExitAsync, CanExitAsync, OnExitAsync, token => ExitAsync(true, token));

	protected override bool ConfirmExit(CancellationToken cancellationToken)
		=> Task.Run(() => ConfirmExitAsync(cancellationToken), cancellationToken).GetAwaiter().GetResult();

	protected override Task<bool> ConfirmExitAsync(CancellationToken cancellationToken) {
		cancellationToken.ThrowIfCancellationRequested();
		Guard.Ensure(ScreenHost?.UserInterfaceServices != null, "Application exit requires user interface services for confirmation.");
		return ScreenHost.UserInterfaceServices.AskYNAsync("Are you sure you want to exit?", cancellationToken);
	}

	protected override bool CanExit(CancellationToken cancellationToken)
		=> Task.Run(() => CanExitAsync(cancellationToken), cancellationToken).GetAwaiter().GetResult();

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
		var activeBlock = host.ActiveBlock;
		return activeBlock == null || ReferenceEquals(activeBlock, screenBlock) || await activeBlock.CanExitAsync(this, cancellationToken);
	}

	protected override bool OnExit(bool force, CancellationToken cancellationToken)
		=> Task.Run(() => OnExitAsync(force, cancellationToken), cancellationToken).GetAwaiter().GetResult();

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
		=> Task.Run(() => OnInitializeAsync(CancellationToken.None)).GetAwaiter().GetResult();

	protected override void OnFinalize()
		=> Task.Run(() => OnFinalizeAsync(CancellationToken.None)).GetAwaiter().GetResult();

	protected override void FreeManagedResources() {
		EnsureCanDisposeSynchronously();
		Task.Run(() => FreeManagedResourcesAsync().AsTask()).GetAwaiter().GetResult();
	}

	protected override ValueTask FreeManagedResourcesAsync()
		=> new(DisposeCore(() => FinalizeAsync()));
}
