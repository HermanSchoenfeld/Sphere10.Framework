// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Threading;
using System.Threading.Tasks;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Application;

/// <summary>Application interactions and command execution. Block membership and screen navigation belong to the borrowed host.</summary>
public class ApplicationUserInterfaceServices : Disposable, IUserInterfaceServices {
	public event EventHandlerEx Changed;

	private readonly CancellationTokenSource _lifetime = new();
	private readonly CancellationToken _lifetimeToken;
	private bool _disposed;
	private string _status;

	public ApplicationUserInterfaceServices(IApplicationScreenHost host, IServiceProvider services) {
		Guard.ArgumentNotNull(host, nameof(host));
		Guard.ArgumentNotNull(services, nameof(services));
		ScreenHost = host;
		Services = services;
		_lifetimeToken = _lifetime.Token;
	}

	public virtual IApplicationScreenHost ScreenHost { get; }

	public virtual bool ApplicationExiting { get; set; }

	public virtual string Status {
		get => _status;
		set {
			if (_status == value)
				return;
			_status = value;
			OnChanged();
		}
	}

	public virtual object PrimaryUIController => null;

	protected IServiceProvider Services { get; }

	public virtual async Task<bool> ExecuteMenuItemAsync(IApplicationMenuItem item, CancellationToken cancellationToken = default) {
		EnsureUsable();
		Guard.ArgumentNotNull(item, nameof(item));
		if (!IsMenuItemEnabled(item))
			return false;
		using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _lifetimeToken);
		if (item is IScreenMenuItem screen) {
			var block = item.Parent?.Parent;
			Guard.Argument(block != null, nameof(item), "The screen command must belong to a block's menu.");
			return await ScreenHost.ActivateScreenAsync(block, screen, cancellation.Token) != null;
		}
		// Actions may navigate through the host; UI command execution never holds its lifecycle gate.
		await ExecuteActionAsync(item, cancellation.Token);
		OnChanged();
		return true;
	}

	protected virtual bool IsMenuItemEnabled(IApplicationMenuItem item) => true;

	protected virtual Task ExecuteActionAsync(IApplicationMenuItem item, CancellationToken cancellationToken) {
		Guard.Argument(item is ApplicationAction, nameof(item), "The menu item is not an executable action.");
		var action = (ApplicationAction)item;
		if (action.AsyncAction != null)
			return action.ExecuteAsync(Services, cancellationToken);
		cancellationToken.ThrowIfCancellationRequested();
		action.Execute(this);
		return Task.CompletedTask;
	}

	protected virtual void OnChanged() {
		if (!_disposed)
			Changed?.Invoke();
	}

	protected void EnsureUsable() => Guard.Ensure(!_disposed, "The UI services have been disposed.");

	protected override void FreeManagedResources() {
		if (_disposed)
			return;
		_disposed = true;
		_lifetime.Cancel();
		_lifetime.Dispose();
		Changed = null;
	}

	protected override ValueTask FreeManagedResourcesAsync() {
		FreeManagedResources();
		return ValueTask.CompletedTask;
	}

	public virtual void Exit(bool force = false) => throw new NotSupportedException("The platform must implement application exit.");
	public virtual Task ShowNagScreen(string nagMessage) => throw new NotSupportedException();
	public virtual Task ShowSendCommentDialog() => throw new NotSupportedException();
	public virtual Task ShowSubmitBugReportDialog() => throw new NotSupportedException();
	public virtual Task ShowRequestFeatureDialog() => throw new NotSupportedException();
	public virtual Task ShowAboutBox() => throw new NotSupportedException();
	public virtual void ReportError(Exception error) => ReportError("Error", error.ToString());
	public virtual void ReportError(string message) => ReportError("Error", message);
	public virtual void ReportError(string title, string message) => throw new NotSupportedException();
	public virtual void ReportFatalError(string title, string message) => throw new NotSupportedException();
	public virtual void ReportInfo(string title, string message) => throw new NotSupportedException();
	public virtual bool AskYN(string question) => throw new NotSupportedException();
	public virtual void ExecuteInUIFriendlyContext(Action action, bool executeAsync = false) => throw new NotSupportedException();

	public virtual Task<bool> AskYNAsync(string question, CancellationToken cancellationToken = default) {
		cancellationToken.ThrowIfCancellationRequested();
		return Task.FromResult(AskYN(question));
	}

	public virtual Task ReportErrorAsync(string title, string message, CancellationToken cancellationToken = default) {
		cancellationToken.ThrowIfCancellationRequested();
		ReportError(title, message);
		return Task.CompletedTask;
	}

	public virtual Task ReportInfoAsync(string title, string message, CancellationToken cancellationToken = default) {
		cancellationToken.ThrowIfCancellationRequested();
		ReportInfo(title, message);
		return Task.CompletedTask;
	}

	public virtual Task ExecuteInUIFriendlyContextAsync(Func<Task> action, CancellationToken cancellationToken = default) =>
		UserInterfaceServicesDispatch.ExecuteAsync(this, action, cancellationToken);


}
