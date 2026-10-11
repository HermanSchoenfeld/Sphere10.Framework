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

/// <summary>Runs application lifecycle work synchronously and supplies worker-backed asynchronous adapters.</summary>
public abstract class SyncApplicationBase : ApplicationBase {
	protected SyncApplicationBase() {
	}

	protected SyncApplicationBase(IServiceProvider services)
		: base(services) {
	}

	public override void Initialize() {
		EnsureCanInitializeSynchronously();
		InitializeCore(_ => Task.FromResult(OnInitialize()), _ => {
			Tools.Application.ExecuteInitializers(Initializers, Logger, ReportProgress);
			return Task.CompletedTask;
		}, CancellationToken.None).GetAwaiter().GetResult();
	}

	public override Task InitializeAsync(CancellationToken cancellationToken = default)
		=> Task.Run(Initialize, cancellationToken);

#pragma warning disable CS0465 // Implements the existing synchronous application lifecycle contract.
	public override void Finalize() {
		EnsureCanFinalizeSynchronously();
		FinalizeCore(() => {
			OnFinalize();
			return Task.CompletedTask;
		}, () => {
			Tools.Application.ExecuteFinalizers(Finalizers, Logger, ReportProgress);
			return Task.CompletedTask;
		}, resource => {
			resource.Dispose();
			return Task.CompletedTask;
		}, CancellationToken.None).GetAwaiter().GetResult();
	}
#pragma warning restore CS0465

	public override Task FinalizeAsync(CancellationToken cancellationToken = default)
		=> Task.Run(Finalize, cancellationToken);

	public override bool Exit(bool force = false, CancellationToken cancellationToken = default) {
		cancellationToken.ThrowIfCancellationRequested();
		EnsureCanExitSynchronously();
		return ExitCore(force, cancellationToken, token => Task.FromResult(ConfirmExit(token)), token => Task.FromResult(CanExit(token)),
			(overridePermissions, token) => Task.FromResult(OnExit(overridePermissions, token)), token => Task.FromResult(Exit(true, token)))
			.GetAwaiter().GetResult();
	}

	public override Task<bool> ExitAsync(bool force = false, CancellationToken cancellationToken = default)
		=> Task.Run(() => Exit(force, cancellationToken), cancellationToken);

	protected override bool ConfirmExit(CancellationToken cancellationToken) {
		cancellationToken.ThrowIfCancellationRequested();
		Guard.Ensure(ScreenHost?.UserInterfaceServices != null, "Application exit requires user interface services for confirmation.");
		return ScreenHost.UserInterfaceServices.AskYN("Are you sure you want to exit?");
	}

	protected override Task<bool> ConfirmExitAsync(CancellationToken cancellationToken)
		=> Task.Run(() => ConfirmExit(cancellationToken), cancellationToken);

	protected override bool CanExit(CancellationToken cancellationToken) {
		cancellationToken.ThrowIfCancellationRequested();
		var host = ScreenHost;
		if (host == null)
			return true;
		// Existing host and block guards expose only asynchronous contracts; wait for those collaborators without selecting this application's async primitive.
		if (!Task.Run(() => host.CanNavigateAsync(cancellationToken), cancellationToken).WaitAsync(cancellationToken).GetAwaiter().GetResult())
			return false;
		var screenBlock = host.ActiveScreen?.Block;
		if (screenBlock != null && !Task.Run(() => screenBlock.CanExitAsync(this, cancellationToken), cancellationToken).WaitAsync(cancellationToken).GetAwaiter().GetResult())
			return false;
		var activeBlock = host.ActiveBlock;
		return activeBlock == null || ReferenceEquals(activeBlock, screenBlock)
			|| Task.Run(() => activeBlock.CanExitAsync(this, cancellationToken), cancellationToken).WaitAsync(cancellationToken).GetAwaiter().GetResult();
	}

	protected override Task<bool> CanExitAsync(CancellationToken cancellationToken)
		=> Task.Run(() => CanExit(cancellationToken), cancellationToken);

	protected override bool OnExit(bool force, CancellationToken cancellationToken) {
		cancellationToken.ThrowIfCancellationRequested();
		try {
			Finalize();
		} catch (Exception error) {
			if (!IsFinalizationCompleted)
				throw;
			Tools.Exceptions.ExecuteIgnoringException(() => Logger.Exception(error));
		}
		return true;
	}

	protected override Task<bool> OnExitAsync(bool force, CancellationToken cancellationToken)
		=> Task.Run(() => OnExit(force, cancellationToken), cancellationToken);

	protected override Task<bool> OnInitializeAsync(CancellationToken cancellationToken)
		=> Task.Run(OnInitialize, cancellationToken);

	protected override Task OnFinalizeAsync(CancellationToken cancellationToken)
		=> Task.Run(OnFinalize, cancellationToken);

	protected override void FreeManagedResources() {
		EnsureCanDisposeSynchronously();
		DisposeCore(() => {
			Finalize();
			return Task.CompletedTask;
		}).GetAwaiter().GetResult();
	}

	protected override ValueTask FreeManagedResourcesAsync() {
		FreeManagedResources();
		return ValueTask.CompletedTask;
	}
}
