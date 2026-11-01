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
using Microsoft.Extensions.DependencyInjection;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Web.AspNetCore.Blazor;

/// <summary>A renderer-owned application screen with asynchronous activation and navigation guards.</summary>
public abstract class BlazorApplicationScreen : ComponentBase, IBlazorApplicationScreen, IApplicationSettingsParticipant, IAsyncDisposable {
	private readonly object _settingsSource = new();
	private Guid? _attachedSessionId;
	private ApplicationSettingsProfile _settingsProfile;
	private Task _settingsUpdates = Task.CompletedTask;
	private long _settingsRevision;
	private long _settingsVersion;
	private long _savedSettingsVersion;
	private string _settingsError;
	private bool _disposed;

	[CascadingParameter]
	public BlazorApplicationScreenSession Session { get; set; }

	[Inject]
	public IBlazorApplicationScreenHost ScreenHost { get; set; }

	[Inject]
	public IUserInterfaceServices UserInterfaceServices { get; set; }

	[Inject]
	private IServiceProvider Services { get; set; }

	public IBlazorApplicationScreenHost Host { get; set; }

	public IBlazorApplicationBlock Block { get; set; }

	/// <summary>Override for a dynamic tab title and call NotifyScreenChanged after changing it.</summary>
	public virtual string Title => null;

	public virtual IBlazorApplicationMenu[] Menus => Array.Empty<IBlazorApplicationMenu>();

	public virtual IBlazorApplicationMenuItem[] ToolBarItems => Array.Empty<IBlazorApplicationMenuItem>();

	public virtual bool HasUnsavedChanges => false;

	public virtual bool CanRestoreAtStartup => true;

	public virtual HelpType Type => HelpType.None;

	public virtual string FileName => null;

	public virtual string Url => null;

	public virtual int? PageNumber => null;

	public virtual int? HelpTopicID => null;

	public virtual int? HelpTopicAlias => null;

	/// <summary>A retryable preference save or application error; the screen's ordinary data remains usable.</summary>
	public string SettingsError => _settingsError ?? _settingsProfile?.LastSaveError?.Message;

	/// <summary>The owning registration's profile; null means this screen declares no preferences.</summary>
	protected ApplicationSettingsProfile SettingsProfile {
		get {
			if (Session?.Definition.Settings == null)
				return null;
			return _settingsProfile ??= Services.GetRequiredService<IBlazorApplication>().Settings.GetProfile(Session.Block, Session.Definition.Registration);
		}
	}

	/// <summary>Captures changed values on the renderer owner, then awaits ordered persistence at accepted close or shutdown.</summary>
	public async Task SaveSettingsAsync(CancellationToken cancellationToken = default) {
		if (_settingsProfile == null)
			return;

		while (!_disposed) {
			// A pending preference hook must finish before its accepted revision can qualify a new local capture.
			cancellationToken.ThrowIfCancellationRequested();
			await CompletePendingSettingsUpdatesAsync(cancellationToken);
			if (_disposed || _settingsVersion <= _savedSettingsVersion)
				return;
			var revision = _settingsRevision;
			var version = _settingsVersion;
			var values = CaptureSettings();
			if (values == null)
				return;
			var capture = _settingsProfile.Capture(values, _settingsSource, revision, version);
			try {
				if (await _settingsProfile.SaveCaptureAsync(capture, cancellationToken)) {
					_savedSettingsVersion = Math.Max(_savedSettingsVersion, version);
					_settingsError = null;
					return;
				}
			} catch (Exception error) {
				_settingsError = error.Message;
				NotifyScreenChanged();
				throw;
			}

			// A worker may initialize a section during capture; process that rebase before retrying an obsolete snapshot.
			await CompletePendingSettingsUpdatesAsync(cancellationToken);
			if (_settingsVersion > _savedSettingsVersion)
				await Task.Yield();
		}
	}

	public async ValueTask DisposeAsync() {
		if (_disposed)
			return;
		_disposed = true;
		if (_settingsProfile != null)
			_settingsProfile.Changed -= OnSettingsChanged;
		if (_attachedSessionId.HasValue)
			ScreenHost.DetachScreen(_attachedSessionId.Value, this);
		await DisposeAsyncCore();
		GC.SuppressFinalize(this);
	}

	protected override async Task OnInitializedAsync() {
		await base.OnInitializedAsync();
		if (SettingsProfile != null) {
			_settingsProfile.Changed += OnSettingsChanged;
			await QueueSettingsUpdateAsync(null);
			await CompletePendingSettingsUpdatesAsync();
		}
		if (Session != null) {
			await ScreenHost.AttachScreenAsync(Session.Id, this);
			if (_disposed) {
				ScreenHost.DetachScreen(Session.Id, this);
				return;
			}
			_attachedSessionId = Session.Id;
		}
	}

	/// <summary>Loads detached typed values and their matching accepted baseline before the screen's first query.</summary>
	protected TSettings GetSettings<TSettings>() {
		Guard.Ensure(SettingsProfile != null, "Declare UseSettings on this screen registration before requesting its preferences.");
		return SettingsProfile.GetValues<TSettings>(out _settingsRevision);
	}

	/// <summary>Applies initial or newly accepted preferences on the renderer owner without replacing business drafts.</summary>
	protected virtual Task OnSettingsChangedAsync(object values) {
		return Task.CompletedTask;
	}

	/// <summary>Returns the screen's current plain preference values; the profile immediately makes its own detached copy.</summary>
	protected virtual object CaptureSettings() {
		return null;
	}

	/// <summary>Marks a deliberate local preference edit without writing during layout or pointer movement.</summary>
	protected void MarkSettingsChanged() {
		_settingsVersion++;
	}

	/// <summary>Awaits a completed control edit's save and keeps failures visible and dirty for retry on close or Options Apply.</summary>
	protected async Task SaveSettingsChangesAsync() {
		await Tools.Exceptions.ExecuteCapturingExceptionAsync<Exception>(() => SaveSettingsAsync());
		if (!_disposed)
			StateHasChanged();
	}

	protected virtual Task<bool> CanDeactivateAsync(CancellationToken cancellationToken) => Task.FromResult(true);

	protected virtual Task OnActivatedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

	protected virtual Task OnDeactivatedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

	protected virtual ValueTask DisposeAsyncCore() => ValueTask.CompletedTask;

	protected void NotifyScreenChanged() {
		// Closing removes the session before the renderer disposes this component.
		if (!_disposed && _attachedSessionId.HasValue && Host != null && Session?.Host == Host && ReferenceEquals(Session.Screen, this))
			Host.NotifyScreenChanged(_attachedSessionId.Value);
	}

	// Provider notifications may arrive from a worker; each settings application is serialized on this renderer owner.
	private void OnSettingsChanged(ApplicationSettingsChangedEventArgs change) {
		if (!_disposed)
			_ = InvokeAsync(() => QueueSettingsUpdateAsync(change));
	}

	// One task tail prevents a slower earlier apply from replacing a newer accepted page after an asynchronous grid refresh.
	private Task QueueSettingsUpdateAsync(ApplicationSettingsChangedEventArgs change) {
		var previous = _settingsUpdates;
		var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		_settingsUpdates = completion.Task;
		return CompleteSettingsUpdateAsync(previous, completion, change);
	}

	// Publish the tail before invoking user hooks, while allowing initial values to apply before the first render.
	private async Task CompleteSettingsUpdateAsync(Task previous, TaskCompletionSource completion, ApplicationSettingsChangedEventArgs change) {
		try {
			await ApplySettingsUpdateAsync(previous, change);
		} finally {
			completion.TrySetResult();
		}
	}

	// Join the current tail and any initialization work queued by its asynchronous preference hook.
	private async Task CompletePendingSettingsUpdatesAsync(CancellationToken cancellationToken = default) {
		Task pending;
		do {
			pending = _settingsUpdates;
			await pending.WaitAsync(cancellationToken);
		} while (!ReferenceEquals(pending, _settingsUpdates));
	}

	// Own saves acknowledge local versions; foreign Options revisions replace stale local preferences, never operational drafts.
	private async Task ApplySettingsUpdateAsync(Task previous, ApplicationSettingsChangedEventArgs change) {
		await previous;
		if (_disposed)
			return;
		var initializing = change?.IsSectionInitialization == true;
		if (initializing && change.Revision <= _settingsRevision)
			return;
		if (ReferenceEquals(change?.Source, _settingsSource)) {
			_settingsRevision = Math.Max(_settingsRevision, change.Revision);
			_savedSettingsVersion = Math.Max(_savedSettingsVersion, change.SourceVersion);
			return;
		}

		// A newly imported role advances this instance's baseline without accepting or replacing its other working edits.
		object values;
		if (initializing && CaptureSettings() is { } working) {
			change.ApplyInitialization(working);
			values = working;
			_settingsRevision = change.Revision;
		} else
			values = _settingsProfile.GetValues(out _settingsRevision);
		if (!initializing)
			_savedSettingsVersion = _settingsVersion;
		try {
			await OnSettingsChangedAsync(values);
			if (!initializing)
				_settingsError = null;
		} catch (Exception error) when (change != null) {
			_settingsError = error.Message;
			Tools.Exceptions.ExecuteIgnoringException(() => SystemLog.Exception(error));
		}
		if (!_disposed)
			StateHasChanged();
	}

	Task<bool> IApplicationScreen.CanDeactivateAsync(CancellationToken cancellationToken) => CanDeactivateAsync(cancellationToken);

	Task IApplicationScreen.OnActivatedAsync(CancellationToken cancellationToken) => OnActivatedAsync(cancellationToken);

	Task IApplicationScreen.OnDeactivatedAsync(CancellationToken cancellationToken) => OnDeactivatedAsync(cancellationToken);
}
