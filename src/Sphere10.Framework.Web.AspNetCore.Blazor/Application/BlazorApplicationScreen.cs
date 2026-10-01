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
using Microsoft.AspNetCore.Components;
using Sphere10.Framework.Application;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Logic;

/// <summary>A renderer-owned application screen with asynchronous activation and navigation guards.</summary>
public abstract class ApplicationScreen : ComponentBase, IApplicationScreen, IAsyncDisposable {
	private Guid? _attachedSessionId;
	private bool _disposed;

	[CascadingParameter]
	public ApplicationScreenSession Session { get; set; }

	[Inject]
	public IApplicationScreenHost ScreenHost { get; set; }

	public virtual bool HasUnsavedChanges => false;

	public virtual HelpType Type => HelpType.None;

	public virtual string FileName => null;

	public virtual string Url => null;

	public virtual int? PageNumber => null;

	public virtual int? HelpTopicID => null;

	public virtual int? HelpTopicAlias => null;

	public async ValueTask DisposeAsync() {
		if (_disposed)
			return;
		_disposed = true;
		if (_attachedSessionId.HasValue)
			ScreenHost.DetachScreen(_attachedSessionId.Value, this);
		await DisposeAsyncCore();
		GC.SuppressFinalize(this);
	}

	protected override async Task OnInitializedAsync() {
		await base.OnInitializedAsync();
		if (Session != null) {
			await ScreenHost.AttachScreenAsync(Session.Id, this);
			if (_disposed) {
				ScreenHost.DetachScreen(Session.Id, this);
				return;
			}
			_attachedSessionId = Session.Id;
		}
	}

	protected virtual Task<bool> CanDeactivateAsync(CancellationToken cancellationToken) => Task.FromResult(true);

	protected virtual Task OnActivatedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

	protected virtual Task OnDeactivatedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

	protected virtual ValueTask DisposeAsyncCore() => ValueTask.CompletedTask;

	protected void NotifyScreenChanged() {
		if (!_disposed && _attachedSessionId.HasValue)
			ScreenHost.NotifyScreenChanged(_attachedSessionId.Value);
	}

	Task<bool> IApplicationScreen.CanDeactivateAsync(CancellationToken cancellationToken) => CanDeactivateAsync(cancellationToken);

	Task IApplicationScreen.OnActivatedAsync(CancellationToken cancellationToken) => OnActivatedAsync(cancellationToken);

	Task IApplicationScreen.OnDeactivatedAsync(CancellationToken cancellationToken) => OnDeactivatedAsync(cancellationToken);
}

