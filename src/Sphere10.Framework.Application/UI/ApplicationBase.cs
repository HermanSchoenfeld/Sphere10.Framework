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
using System.Threading.Tasks;

namespace Sphere10.Framework.Application.UI;

/// <summary>Shares application command storage and notifications while platform adapters project their existing screen hosts.</summary>
public abstract class ApplicationBase : Disposable, IApplication {
	public event EventHandlerEx Changed;

	private IApplicationMenu[] _menus = Array.Empty<IApplicationMenu>();
	private IApplicationMenuItem[] _toolBarItems = Array.Empty<IApplicationMenuItem>();
	private IApplicationScreenHost _observedHost;
	private IApplicationBlock[] _parentedBlocks = Array.Empty<IApplicationBlock>();

	public abstract IApplicationPlugin[] LoadedPlugins { get; }

	/// <summary>Projects the host's current registered block membership without retaining another collection.</summary>
	public virtual IApplicationBlock[] Blocks => ScreenHost?.Blocks ?? Array.Empty<IApplicationBlock>();

	public abstract IApplicationScreenHost ScreenHost { get; }

	public virtual bool HasUnsavedChanges => ScreenHost?.HasUnsavedChanges ?? false;

	public virtual IApplicationMenu[] Menus => Tools.Array.Clone(_menus);

	public virtual IApplicationMenuItem[] ToolBarItems => Tools.Array.Clone(_toolBarItems);

	protected bool IsDisposed { get; private set; }

	/// <summary>Replaces application-level menu membership without taking ownership of the definitions.</summary>
	public virtual void SetMenus(IEnumerable<IApplicationMenu> menus) {
		Guard.Ensure(!IsDisposed, "The application has been disposed.");
		Guard.ArgumentNotNull(menus, nameof(menus));
		var definitions = menus.ToArray();
		Guard.Argument(definitions.All(menu => menu != null), nameof(menus), "Menu definitions cannot be null.");
		_menus = definitions;
		OnChanged();
	}

	/// <summary>Replaces application-level toolbar membership without taking ownership of the definitions.</summary>
	public virtual void SetToolBarItems(IEnumerable<IApplicationMenuItem> items) {
		Guard.Ensure(!IsDisposed, "The application has been disposed.");
		Guard.ArgumentNotNull(items, nameof(items));
		var definitions = items.ToArray();
		Guard.Argument(definitions.All(item => item != null), nameof(items), "Toolbar definitions cannot be null.");
		_toolBarItems = definitions;
		OnChanged();
	}

	protected virtual void OnChanged() {
		if (IsDisposed)
			return;
		RefreshBlockParents();
		Changed?.Invoke();
	}

	/// <summary>Call after assigning the borrowed platform host to maintain its live blocks' application parent.</summary>
	protected void ObserveScreenHost() {
		Guard.Ensure(_observedHost == null, "The application already observes a screen host.");
		Guard.ArgumentNotNull(ScreenHost, nameof(ScreenHost));
		RefreshBlockParents();
		_observedHost = ScreenHost;
		_observedHost.Changed += OnScreenHostChanged;
	}

	protected virtual void OnScreenHostChanged() => RefreshBlockParents();

	protected override void FreeManagedResources() {
		IsDisposed = true;
		if (_observedHost != null)
			_observedHost.Changed -= OnScreenHostChanged;
		foreach (var block in _parentedBlocks)
			if (ReferenceEquals(block.Parent, this))
				block.Parent = null;
		_parentedBlocks = Array.Empty<IApplicationBlock>();
		_observedHost = null;
		Changed = null;
	}

	protected override ValueTask FreeManagedResourcesAsync() {
		FreeManagedResources();
		return ValueTask.CompletedTask;
	}

	private void RefreshBlockParents() {
		if (IsDisposed)
			return;
		var blocks = ScreenHost?.Blocks ?? Array.Empty<IApplicationBlock>();
		Guard.Ensure(blocks.All(block => block.Parent == null || ReferenceEquals(block.Parent, this)), "A registered block belongs to another application.");
		var members = blocks.ToHashSet(ReferenceEqualityComparer.Instance);
		foreach (var block in _parentedBlocks)
			if (!members.Contains(block) && ReferenceEquals(block.Parent, this))
				block.Parent = null;
		foreach (var block in blocks)
			block.Parent = this;
		_parentedBlocks = blocks;
	}
}
