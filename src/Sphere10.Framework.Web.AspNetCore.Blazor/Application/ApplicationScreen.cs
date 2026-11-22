// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;
using SharedUI = global::Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Web.AspNetCore.Blazor;

/// <summary>A renderer-owned screen using its registration's actual settings and existing activation lifecycle.</summary>
/// <remarks>
/// After each render the screen compares the values its shell presents (title, unsaved state and the captions, visibility, availability
/// and checked state of its menus and toolbar items) with those it last published, and notifies its host when they differ. A screen
/// therefore needs <see cref="NotifyScreenChanged"/> only for a change that is not followed by a render of the screen itself.
/// A screen contributes commands either by declaring <see cref="ScreenCommand"/>s with <see cref="Command"/> and placing them
/// with <see cref="UseCommands"/>, which the default <see cref="Menus"/> and <see cref="ToolBarItems"/> publish, or by
/// overriding those two properties with menu items of its own.
/// </remarks>
public abstract partial class ApplicationScreen : ComponentBase, IApplicationScreen, IAsyncDisposable, IHandleAfterRender {
	private Guid? _attachedSessionId;
	private bool _settingsDirty;
	private string _settingsError;
	private bool _disposed;
	private bool _rendered;
	// Set once a renderer has initialized the screen; only then can the screen render itself.
	private bool _initialized;
	private object[] _publishedPresentation;

	/// <summary>The registration instance owning this renderer's screen and settings.</summary>
	[CascadingParameter]
	public ApplicationScreenSession Session { get; set; }

	/// <summary>The circuit's existing presentation host.</summary>
	[Inject]
	public IApplicationScreenHost ScreenHost { get; set; }

	/// <summary>The circuit's application UI operations.</summary>
	[Inject]
	public IUserInterfaceServices UserInterfaceServices { get; set; }

	/// <inheritdoc />
	public IApplicationScreenHost Host { get; set; }

	/// <inheritdoc />
	public IApplicationBlock Block { get; set; }

	/// <summary>Override for a dynamic tab title and call NotifyScreenChanged after changing it.</summary>
	public virtual string Title => null;

	/// <summary>Returns the actual registration settings; a typed override declares this screen's settings class.</summary>
	public virtual SettingsObject ScreenSettings => Session?.Definition.Settings;

	/// <inheritdoc />
	public virtual IApplicationMenu[] Menus => GetCommandMenus();

	/// <inheritdoc />
	public virtual IApplicationMenuItem[] ToolBarItems => GetCommandToolBarItems();

	/// <inheritdoc />
	public virtual bool HasUnsavedChanges => false;

	/// <inheritdoc />
	public virtual bool CanRestoreAtStartup => true;

	/// <inheritdoc />
	public virtual HelpType Type => HelpType.None;

	/// <inheritdoc />
	public virtual string FileName => null;

	/// <inheritdoc />
	public virtual string Url => null;

	/// <inheritdoc />
	public virtual int? PageNumber => null;

	/// <inheritdoc />
	public virtual int? HelpTopicID => null;

	/// <inheritdoc />
	public virtual int? HelpTopicAlias => null;

	/// <summary>The most recent settings failure, cleared after a successful save or settings application.</summary>
	public string SettingsError => _settingsError;

	// Closing removes the session before the renderer disposes this component.
	private bool IsPresented => !_disposed && _attachedSessionId.HasValue && Host != null && Session?.Host == Host && ReferenceEquals(Session.Screen, this);

	/// <summary>Saves a completed local settings edit through the actual object's configured provider.</summary>
	public async Task SaveSettingsAsync(CancellationToken cancellationToken = default) {
		if (!_settingsDirty || ScreenSettings == null)
			return;
		try {
			// A later edit must remain pending while the current snapshot is being saved.
			_settingsDirty = false;
			await ScreenSettings.SaveAsync(cancellationToken);
			_settingsError = null;
		} catch (Exception error) {
			_settingsDirty = true;
			_settingsError = error.Message;
			NotifyScreenChanged();
			throw;
		}
	}

	/// <summary>Detaches the screen and its settings subscriptions without saving rejected edits.</summary>
	public async ValueTask DisposeAsync() {
		if (_disposed)
			return;
		_disposed = true;
		foreach (var settings in GetObservedSettings())
			settings.OnChanged -= SettingsChanged;
		if (_attachedSessionId.HasValue)
			ScreenHost.DetachScreen(_attachedSessionId.Value, this);
		await DisposeAsyncCore();
		GC.SuppressFinalize(this);
	}

	/// <inheritdoc />
	Task<bool> SharedUI.IApplicationScreen.CanDeactivateAsync(CancellationToken cancellationToken) => CanDeactivateAsync(cancellationToken);

	/// <inheritdoc />
	Task SharedUI.IApplicationScreen.OnActivatedAsync(CancellationToken cancellationToken) => OnActivatedAsync(cancellationToken);

	/// <inheritdoc />
	Task SharedUI.IApplicationScreen.OnDeactivatedAsync(CancellationToken cancellationToken) => OnDeactivatedAsync(cancellationToken);

	/// <summary>Runs the component's after-render hooks, then publishes changed shell presentation.</summary>
	/// <remarks>
	/// This re-implements the renderer's after-render callback of <see cref="ComponentBase"/>, which has no further logic, so a derived
	/// screen's <see cref="ComponentBase.OnAfterRender"/> override cannot suppress the publication by omitting its base call.
	/// </remarks>
	Task IHandleAfterRender.OnAfterRenderAsync() {
		var firstRender = !_rendered;
		_rendered = true;
		OnAfterRender(firstRender);
		PublishPresentationChanges();
		return OnAfterRenderAsync(firstRender);
	}

	/// <summary>Applies restored settings before attaching or constructing operational child controls.</summary>
	protected override async Task OnInitializedAsync() {
		_initialized = true;
		await base.OnInitializedAsync();
		foreach (var settings in GetObservedSettings())
			settings.OnChanged += SettingsChanged;
		await OnSettingsChangedAsync(ScreenSettings);
		if (Session != null) {
			await ScreenHost.AttachScreenAsync(Session.Id, this);
			if (_disposed) {
				ScreenHost.DetachScreen(Session.Id, this);
				return;
			}
			_attachedSessionId = Session.Id;
		}
	}

	/// <summary>Returns the active block's actual settings object.</summary>
	protected TSettings GetBlockSettings<TSettings>()
		where TSettings : SettingsObject {
		return Guard.ArgumentCast<TSettings>((Block ?? Session?.Block)?.BlockSettings, nameof(TSettings));
	}

	/// <summary>Returns the active plugin owner's actual settings object for this circuit.</summary>
	protected TSettings GetPluginSettings<TSettings>()
		where TSettings : SettingsObject {
		return Guard.ArgumentCast<TSettings>((Block ?? Session?.Block)?.Source?.PluginSettings, nameof(TSettings));
	}

	/// <summary>Refreshes screen presentation after its own, block or plugin settings are saved.</summary>
	protected virtual Task OnSettingsChangedAsync(SettingsObject values) => Task.CompletedTask;

	/// <summary>Marks a completed local preference edit for the accepted close/save lifecycle.</summary>
	protected void MarkSettingsChanged() => _settingsDirty = true;

	/// <summary>Saves a completed control edit while keeping a recoverable failure visible on the screen.</summary>
	protected async Task SaveSettingsChangesAsync() {
		await Tools.Exceptions.ExecuteCapturingExceptionAsync<Exception>(() => SaveSettingsAsync());
		if (!_disposed)
			StateHasChanged();
	}

	/// <summary>Allows derived screens to veto navigation without saving rejected state.</summary>
	protected virtual Task<bool> CanDeactivateAsync(CancellationToken cancellationToken) => Task.FromResult(true);

	/// <summary>Runs when the existing host activates this screen.</summary>
	protected virtual Task OnActivatedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

	/// <summary>Runs when the existing host deactivates this screen.</summary>
	protected virtual Task OnDeactivatedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

	/// <summary>Releases resources owned by a derived screen.</summary>
	protected virtual ValueTask DisposeAsyncCore() => ValueTask.CompletedTask;

	/// <summary>Refreshes the current host's screen presentation when the screen remains attached.</summary>
	/// <remarks>Rendering the screen publishes changed commands, title and unsaved state automatically; call this after a change made without a render.</remarks>
	protected void NotifyScreenChanged() {
		if (IsPresented)
			Host.NotifyScreenChanged(_attachedSessionId.Value);
		if (!_disposed)
			CommandsChanged?.Invoke();
	}

	// Notifies the host only when a presented value changed, so the shell render that follows a notification cannot cause another one.
	private void PublishPresentationChanges() {
		if (!IsPresented)
			return;
		var presentation = CapturePresentation();
		if (_publishedPresentation != null && _publishedPresentation.SequenceEqual(presentation))
			return;
		_publishedPresentation = presentation;
		Host.NotifyScreenChanged(_attachedSessionId.Value);
	}

	// Captures values rather than command references: screens may rebuild equivalent commands on every read.
	private object[] CapturePresentation() {
		var values = new List<object> { Title, HasUnsavedChanges };
		var menus = Menus ?? Array.Empty<IApplicationMenu>();
		values.Add(menus.Length);
		foreach (var menu in menus) {
			values.Add(menu?.Id);
			values.Add(menu?.Text);
			CaptureCommands(menu?.Items, values);
		}
		CaptureCommands(ToolBarItems, values);
		return values.ToArray();
	}

	// Records each command's presented state, including the children of action groups.
	private static void CaptureCommands(IApplicationMenuItem[] items, List<object> values) {
		values.Add(items?.Length ?? 0);
		foreach (var item in items ?? Array.Empty<IApplicationMenuItem>()) {
			if (item == null) {
				values.Add(null);
				continue;
			}
			values.Add(item.Id);
			values.Add(item.Title);
			values.Add(item.Icon);
			values.Add(item.IsVisible);
			values.Add(item.IsEnabled);
			values.Add(item is IApplicationCheckableMenuItem checkable && checkable.IsChecked);
			if (item is ApplicationMenuGroup group)
				CaptureCommands(group.Items, values);
		}
	}

	// Settings notifications carry the actual changed value; presentation work remains on the renderer.
	private void SettingsChanged() {
		if (!_disposed)
			_ = InvokeAsync(async () => {
				try {
					await OnSettingsChangedAsync(ScreenSettings);
					_settingsError = null;
				} catch (Exception error) {
					_settingsError = error.Message;
					SystemLog.Exception(error);
				}
				if (!_disposed)
					StateHasChanged();
			});
	}

	// Each screen listens directly to the settings owners that can affect its presentation.
	private IEnumerable<SettingsObject> GetObservedSettings() {
		var block = Block ?? Session?.Block;
		if (ScreenSettings != null)
			yield return ScreenSettings;
		if (block?.BlockSettings != null)
			yield return block.BlockSettings;
		if (block?.Source?.PluginSettings != null)
			yield return block.Source.PluginSettings;
	}
}
