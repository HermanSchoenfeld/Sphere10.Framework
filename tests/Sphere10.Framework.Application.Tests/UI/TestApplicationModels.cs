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
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Application.Tests;

/// <summary>A test presentation adapter; command membership is independent of the shared block lifecycle contract.</summary>
internal class TestApplicationBlock : ApplicationBlockBase, IApplicationCommandProvider {
	private readonly List<IApplicationMenu> _menus = new();

	public TestApplicationBlock() {
	}

	public TestApplicationBlock(string name, ApplicationScreenDefinition defaultScreen) {
		Name = name;
		AddScreenDefinition(defaultScreen);
		DefaultScreenDefinition = defaultScreen;
	}

	public virtual IApplicationMenu[] Menus => _menus.ToArray();

	public virtual IApplicationMenuItem[] ToolBarItems => Array.Empty<IApplicationMenuItem>();

	public virtual void AddMenu(IApplicationMenu menu) {
		Guard.ArgumentNotNull(menu, nameof(menu));
		Guard.ArgumentNotNull(menu.Items, nameof(menu));
		Guard.Argument(!ContainsMenu(menu), nameof(menu), "The menu is already in this block.");
		Guard.Argument(menu.Parent == null || ReferenceEquals(menu.Parent, this), nameof(menu), "The menu already belongs to another block.");
		menu.Parent = this;
		_menus.Add(menu);
	}

	public virtual bool ContainsMenu(IApplicationMenu menu) {
		return _menus.Any(candidate => ReferenceEquals(candidate, menu));
	}

	public virtual void RemoveMenu(IApplicationMenu menu) {
		var index = _menus.FindIndex(candidate => ReferenceEquals(candidate, menu));
		if (index < 0)
			return;
		_menus.RemoveAt(index);
		if (ReferenceEquals(menu.Parent, this))
			menu.Parent = null;
	}

	public override void RemoveScreenDefinition(ApplicationScreenDefinition definition) {
		Guard.ArgumentNotNull(definition, nameof(definition));
		Guard.Argument(!definition.Activators.Any(item => ReferenceEquals(item.Parent?.Parent, this))
			&& !((IApplicationCommandProvider)this).ToolBarItems.OfType<IScreenMenuItem>().Any(item => ReferenceEquals(item.ScreenDefinition, definition)),
			nameof(definition), "A referenced screen definition cannot be removed.");
		base.RemoveScreenDefinition(definition);
	}
}

internal class TestApplicationMenu<TItem> : ApplicationMenuBase<TItem>
	where TItem : class, IApplicationMenuItem {
}

internal class TestApplicationMenuItem : ApplicationMenuItemBase {
}
