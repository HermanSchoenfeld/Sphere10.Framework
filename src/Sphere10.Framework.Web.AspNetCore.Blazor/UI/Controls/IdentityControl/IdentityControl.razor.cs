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
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.UI.Controls;

/// <summary>Displays an application's current identity and routes its menu commands to the host.</summary>
/// <remarks>Accepts identity data from any authentication provider. It never signs in/out, authorizes commands or resolves an authentication service itself.</remarks>
public partial class IdentityControl : ComponentBase, IAsyncDisposable {
	private readonly ClaimsPrincipal _anonymousUser = new(new ClaimsIdentity());
	private readonly string _menuId = "sphere10-identity-" + Guid.NewGuid().ToString("N");
	private readonly Dictionary<string, ElementReference> _itemButtons = new();
	private IBlazorApplicationMenuItem[] _items = Array.Empty<IBlazorApplicationMenuItem>();
	private ClaimsPrincipal _lastUser;
	private ElementReference _trigger;
	private ElementReference _root;
	private IJSObjectReference _module;
	private Task _initializationTask;
	private Task _disposalTask;
	private string _focusedItem;
	private string _focusItem;
	private bool _focusTrigger;
	private bool _open;
	private bool _executing;
	private bool _disposed;

	[Inject] public IJSRuntime JSRuntime { get; set; }

	[Parameter] public ClaimsPrincipal User { get; set; }

	[Parameter] public string DisplayName { get; set; }

	[Parameter] public string SecondaryText { get; set; }

	[Parameter] public string AvatarUrl { get; set; }

	[Parameter] public IBlazorApplicationMenuItem[] Items { get; set; } = Array.Empty<IBlazorApplicationMenuItem>();

	[Parameter] public EventCallback<IBlazorApplicationMenuItem> OnSelect { get; set; }

	[Parameter] public RenderFragment<ClaimsPrincipal> AvatarTemplate { get; set; }

	[Parameter] public RenderFragment<ClaimsPrincipal> IdentityTemplate { get; set; }

	[Parameter] public RenderFragment<ClaimsPrincipal> MenuHeader { get; set; }

	[Parameter] public RenderFragment<ClaimsPrincipal> MenuFooter { get; set; }

	public ClaimsPrincipal EffectiveUser => User ?? _anonymousUser;

	public virtual string EffectiveDisplayName => !string.IsNullOrWhiteSpace(DisplayName) ? DisplayName :
		EffectiveUser.Identity?.IsAuthenticated == true ?
			!string.IsNullOrWhiteSpace(EffectiveUser.Identity.Name) ? EffectiveUser.Identity.Name : "Signed-in user" : "Guest";

	public virtual string EffectiveSecondaryText => SecondaryText ??
		(EffectiveUser.Identity?.IsAuthenticated == true ? EffectiveUser.FindFirst(ClaimTypes.Email)?.Value : null);

	protected string Initials {
		get {
			var words = EffectiveDisplayName.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
			return words.Length == 0 ? "?" : string.Concat(words[0].Take(1).Concat(words.Length > 1 ? words[^1].Take(1) : Array.Empty<char>())).ToUpperInvariant();
		}
	}

	protected override void OnParametersSet() {
		Guard.ArgumentNotNull(Items, nameof(Items));
		_items = Tools.UI.MergeMenuItems<IBlazorApplicationMenuItem>(Items);
		if (!ReferenceEquals(_lastUser, EffectiveUser)) {
			_lastUser = EffectiveUser;
			CloseMenu(false);
		}
		foreach (var id in _itemButtons.Keys.Where(id => _items.All(item => item.Id != id)).ToArray())
			_itemButtons.Remove(id);
		if (_focusItem != null && _items.All(item => item.Id != _focusItem))
			_focusItem = null;
	}

	protected override async Task OnAfterRenderAsync(bool firstRender) {
		if (_disposed)
			return;
		try {
			_initializationTask ??= InitializeJavascriptAsync();
			await _initializationTask;
			if (_disposed)
				return;
			if (_focusTrigger && !_executing) {
				_focusTrigger = false;
				await _trigger.FocusAsync();
			} else if (_open && _focusItem != null && _itemButtons.TryGetValue(_focusItem, out var item)) {
				_focusItem = null;
				await item.FocusAsync();
			}
		} catch (OperationCanceledException) when (_disposed) {
			// An import or focus operation can outlive removal of this control.
		} catch (JSDisconnectedException) {
			// Retry when this circuit can render again; the owned module is released on disposal.
			_initializationTask = null;
		}
	}

	public ValueTask DisposeAsync() => new(_disposalTask ??= DisposeCoreAsync());

	protected void ToggleMenu() {
		if (_executing || _disposed)
			return;
		if (_open)
			CloseMenu();
		else
			OpenMenu();
	}

	protected void OpenMenu(bool last = false) {
		if (_executing || _disposed)
			return;
		_open = true;
		_focusTrigger = false;
		var commands = _items.Where(item => item is not IApplicationMenuSeparator).ToArray();
		_focusItem = (last ? commands.LastOrDefault() : commands.FirstOrDefault())?.Id;
	}

	protected void CloseMenu(bool restoreFocus = true) {
		_focusTrigger = _open && restoreFocus;
		_open = false;
		_focusItem = null;
		_focusedItem = null;
	}

	/// <summary>Serializes command selection. Overrides belong in OnItemSelectedAsync so double-click protection remains active.</summary>
	protected async Task SelectItemAsync(IBlazorApplicationMenuItem item) {
		Guard.ArgumentNotNull(item, nameof(item));
		if (_executing || _disposed || !_items.Contains(item))
			return;
		_executing = true;
		CloseMenu(false);
		StateHasChanged();
		using var completion = Tools.Scope.ExecuteOnDispose(() => {
			_executing = false;
			if (!_disposed)
				_focusTrigger = true;
		});
		await OnItemSelectedAsync(item);
	}

	/// <summary>Override to customize command handling, or pass OnSelect to route commands through the application's existing screen host.</summary>
	protected virtual Task OnItemSelectedAsync(IBlazorApplicationMenuItem item) => OnSelect.InvokeAsync(item);

	protected virtual void OnItemHover(IBlazorApplicationMenuItem item) {
		if (item is BlazorApplicationMenuItem command)
			command.NotifyHover();
	}

	protected void OnTriggerKeyDown(KeyboardEventArgs args) {
		if (args.AltKey || args.CtrlKey || args.MetaKey)
			return;
		if (args.Key is "ArrowDown" or "ArrowUp")
			OpenMenu(args.Key == "ArrowUp");
		else if (args.Key == "Escape")
			CloseMenu();
	}

	protected void OnMenuKeyDown(KeyboardEventArgs args) {
		if (args.AltKey || args.CtrlKey || args.MetaKey)
			return;
		if (args.Key == "Escape") {
			CloseMenu();
			return;
		}
		if (args.Key is not ("ArrowDown" or "ArrowUp" or "Home" or "End"))
			return;
		var commands = _items.Where(item => item is not IApplicationMenuSeparator).ToArray();
		if (commands.Length == 0)
			return;
		var index = Array.FindIndex(commands, item => item.Id == _focusedItem);
		index = args.Key switch {
			"Home" => 0,
			"End" => commands.Length - 1,
			"ArrowUp" => (index + commands.Length - 1) % commands.Length,
			_ => (index + 1) % commands.Length
		};
		_focusItem = commands[index].Id;
	}

	private async Task InitializeJavascriptAsync() {
		_module ??= await JSRuntime.InvokeAsync<IJSObjectReference>("import",
			"./_content/Sphere10.Framework.Web.AspNetCore.Blazor/UI/Controls/IdentityControl/IdentityControl.razor.js");
		if (!_disposed && _module != null)
			await _module.InvokeVoidAsync("Initialize", _root);
	}

	private async Task DisposeCoreAsync() {
		_disposed = true;
		_open = false;
		_focusTrigger = false;
		_focusItem = null;
		try {
			if (_initializationTask != null)
				await _initializationTask;
		} catch (OperationCanceledException) {
			// Browser shutdown can cancel an import still owned by this control.
		} catch (JSDisconnectedException) {
			// No browser listeners can be reached after circuit disconnection.
		}
		if (_module == null)
			return;
		try {
			await _module.InvokeVoidAsync("Dispose", _root);
		} catch (OperationCanceledException) {
			// Listener teardown may race browser shutdown.
		} catch (JSDisconnectedException) {
			// The removed DOM node releases its listeners when disconnected.
		}
		try {
			await _module.DisposeAsync();
		} catch (OperationCanceledException) {
			// Releasing owned interop handles may also race browser shutdown.
		} catch (JSDisconnectedException) {
			// The disconnected circuit has already released the browser-side handle.
		}
	}

	private void FocusedItem(IBlazorApplicationMenuItem item) {
		_focusedItem = item.Id;
		OnItemHover(item);
	}
}
