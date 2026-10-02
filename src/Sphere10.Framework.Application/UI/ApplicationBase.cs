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

	public abstract IApplicationPlugin[] Plugins { get; }

	public virtual IApplicationPlugin ActivePlugin => ActiveBlock == null ? null :
		Plugins.FirstOrDefault(plugin => plugin.Blocks.Any(block => string.Equals(block.Id, ActiveBlock.Id, StringComparison.Ordinal)));

	public virtual IApplicationBlock[] Blocks => Plugins.SelectMany(plugin => plugin.Blocks).ToArray();

	public abstract IApplicationBlock ActiveBlock { get; }

	public abstract IApplicationScreen ActiveScreen { get; }

	public abstract bool HasUnsavedChanges { get; }

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
		if (!IsDisposed)
			Changed?.Invoke();
	}

	protected override void FreeManagedResources() {
		IsDisposed = true;
		Changed = null;
	}

	protected override ValueTask FreeManagedResourcesAsync() {
		FreeManagedResources();
		return ValueTask.CompletedTask;
	}
}
