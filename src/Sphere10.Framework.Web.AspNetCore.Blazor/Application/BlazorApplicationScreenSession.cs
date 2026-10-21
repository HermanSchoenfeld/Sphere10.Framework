// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Web.AspNetCore.Blazor;

/// <summary>The logical Blazor screen retained by the host, with renderer-local identity and immutable component parameters.</summary>
/// <remarks>The renderer creates and disposes Screen. This proxy delegates its portable screen lifecycle to that component.</remarks>
public sealed class BlazorApplicationScreenSession : IBlazorApplicationScreen {
	private readonly string _registeredTitle;
	private readonly ApplicationScreenDefinition _definition;
	private readonly List<IBlazorApplicationMenu> _ownedMenus = new();
	private readonly List<IBlazorApplicationMenuItem> _componentToolBarItems = new();
	private BlazorApplicationMenu _toolbarOwner;
	private readonly bool _hasActivationState;

	internal BlazorApplicationScreenSession(IBlazorApplicationBlock block, BlazorScreenMenuItem menuItem, ApplicationScreenDefinition definition, BlazorApplicationScreenActivation activation = null) {
		Guard.ArgumentNotNull(block, nameof(block));
		Guard.ArgumentNotNull(menuItem, nameof(menuItem));
		Guard.ArgumentNotNull(definition, nameof(definition));
		Id = Guid.NewGuid();
		Block = block;
		MenuItem = menuItem;
		_definition = definition;
		_registeredTitle = definition.Title;
		var parameters = new Dictionary<string, object>(menuItem.Parameters, StringComparer.OrdinalIgnoreCase);
		if (activation != null)
			foreach (var parameter in activation.Parameters)
				parameters[parameter.Key] = parameter.Value;
		Parameters = new ReadOnlyDictionary<string, object>(parameters);
		InstanceKey = ActivationMode == ScreenActivationMode.MultiInstance ? activation?.InstanceKey : null;
		_hasActivationState = activation != null && (activation.Parameters.Count > 0 || !string.IsNullOrEmpty(activation.InstanceKey));
	}

	public Guid Id { get; }

	public IBlazorApplicationScreenHost Host { get; set; }

	public IBlazorApplicationBlock Block { get; set; }

	public BlazorScreenMenuItem MenuItem { get; }

	public string MenuItemId => MenuItem.Id;

	public Type ScreenType => _definition.ScreenType;

	public ScreenActivationMode ActivationMode => _definition.ActivationMode ?? ScreenActivationMode.MultiInstance;

	public bool IsPermanent => ActivationMode == ScreenActivationMode.PermanentSingleton;

	public ScreenKind ScreenKind => _definition.ScreenKind;

	public string InstanceKey { get; }

	public IDictionary<string, object> Parameters { get; }

	public IBlazorApplicationScreen Screen { get; private set; }

	public string Title => Screen?.Title ?? _registeredTitle;

	public bool HasUnsavedChanges => Screen?.HasUnsavedChanges ?? false;

	public bool CanRestoreAtStartup => !_hasActivationState && ScreenKind == ScreenKind.Normal && (Screen?.CanRestoreAtStartup ?? false);

	public string Icon => MenuItem.Icon;

	public IBlazorApplicationMenu[] Menus {
		get {
			var menus = Screen?.Menus ?? Array.Empty<IBlazorApplicationMenu>();
			foreach (var previous in _ownedMenus.ToArray())
				if (!menus.Any(menu => ReferenceEquals(menu, previous))) {
					if (ReferenceEquals(previous.Parent, Block))
						previous.Parent = null;
					_ownedMenus.Remove(previous);
				}
			foreach (var menu in menus)
				if (menu.Parent == null) {
					if (!_ownedMenus.Any(candidate => ReferenceEquals(candidate, menu)))
						_ownedMenus.Add(menu);
					menu.Parent = Block;
				}
			return menus;
		}
	}

	public IBlazorApplicationMenuItem[] ToolBarItems {
		get {
			var items = Screen?.ToolBarItems ?? Array.Empty<IBlazorApplicationMenuItem>();
			foreach (var previous in _componentToolBarItems.ToArray())
				if (!items.Any(item => ReferenceEquals(item, previous))) {
					_toolbarOwner.RemoveItem(previous);
					_componentToolBarItems.Remove(previous);
				}
			foreach (var item in items)
				if (item.Parent == null) {
					_toolbarOwner ??= new BlazorApplicationMenu { Text = "Screen toolbar" };
					_toolbarOwner.Parent = Block;
					if (!_componentToolBarItems.Any(candidate => ReferenceEquals(candidate, item)))
						_componentToolBarItems.Add(item);
					_toolbarOwner.AddItem(item);
				}
			return items;
		}
	}

	public HelpType Type => Screen?.Type ?? HelpType.None;

	public string FileName => Screen?.FileName;

	public string Url => Screen?.Url;

	public int? PageNumber => Screen?.PageNumber;

	public int? HelpTopicID => Screen?.HelpTopicID;

	public int? HelpTopicAlias => Screen?.HelpTopicAlias;

	public Task<bool> CanDeactivateAsync(CancellationToken cancellationToken = default) {
		cancellationToken.ThrowIfCancellationRequested();
		return Screen?.CanDeactivateAsync(cancellationToken) ?? Task.FromResult(true);
	}

	public Task OnActivatedAsync(CancellationToken cancellationToken = default) {
		cancellationToken.ThrowIfCancellationRequested();
		return Screen?.OnActivatedAsync(cancellationToken) ?? Task.CompletedTask;
	}

	public Task OnDeactivatedAsync(CancellationToken cancellationToken = default) {
		cancellationToken.ThrowIfCancellationRequested();
		return Screen?.OnDeactivatedAsync(cancellationToken) ?? Task.CompletedTask;
	}

	internal void SetScreen(IBlazorApplicationScreen screen) {
		Guard.Argument(screen == null || ScreenType.IsInstanceOfType(screen), nameof(screen), "The component must match the retained screen type.");
		Guard.Argument(!ReferenceEquals(screen, this), nameof(screen), "A screen proxy cannot be attached to itself.");
		if (ReferenceEquals(Screen, screen))
			return;
		Guard.Argument(screen == null || (screen.Host == null || ReferenceEquals(screen.Host, Host))
			&& (screen.Block == null || ReferenceEquals(screen.Block, Block)), nameof(screen), "The component already belongs to another host or block.");
		ClearAttachedScreen();
		Screen = screen;
		if (screen == null)
			return;
		var attached = false;
		using var attachment = Tools.Scope.ExecuteOnDispose(() => {
			if (!attached)
				ClearAttachedScreen();
		});
		screen.Host = Host;
		screen.Block = Block;
		_ = Menus;
		_ = ToolBarItems;
		attached = true;
	}

	private void ClearAttachedScreen() {
		// Rollback must not read component getters that may have caused the attachment failure.
		var screen = Screen;
		Screen = null;
		try {
			foreach (var menu in _ownedMenus)
				if (ReferenceEquals(menu.Parent, Block))
					menu.Parent = null;
			if (_toolbarOwner != null) {
				foreach (var item in _componentToolBarItems)
					if (ReferenceEquals(item.Parent, _toolbarOwner)) {
						_toolbarOwner.RemoveItem(item);
						if (ReferenceEquals(item.Parent, _toolbarOwner))
							item.Parent = null;
					}
				_toolbarOwner.Parent = null;
			}
		} finally {
			_ownedMenus.Clear();
			_componentToolBarItems.Clear();
			if (screen != null)
				try {
					screen.Block = null;
				} finally {
					screen.Host = null;
				}
		}
	}

	void IComponent.Attach(RenderHandle renderHandle) => throw new NotSupportedException("Render the registered ScreenType; retained screen sessions are logical proxies.");

	Task IComponent.SetParametersAsync(ParameterView parameters) => throw new NotSupportedException("Render the registered ScreenType; retained screen sessions are logical proxies.");
}
