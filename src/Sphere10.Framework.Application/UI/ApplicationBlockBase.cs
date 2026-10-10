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

namespace Sphere10.Framework.Application.UI;

/// <summary>Reusable block metadata, canonical screen definitions and menu membership.</summary>
public abstract class ApplicationBlockBase : IApplicationBlock {
	private readonly List<IApplicationMenu> _menus = new();
	private readonly List<ApplicationScreenDefinition> _screenDefinitions = new();
	private ApplicationScreenDefinition _defaultScreenDefinition;

	public virtual IApplication Parent { get; set; }

	public virtual IApplicationPlugin Source { get; set; }

	public virtual string Name { get; set; } = string.Empty;

	public virtual int Position { get; set; }

	public virtual ApplicationScreenDefinition[] ScreenDefinitions => _screenDefinitions.ToArray();

	public virtual ApplicationScreenDefinition DefaultScreenDefinition {
		get => _defaultScreenDefinition;
		set {
			if (value != null)
				AddScreenDefinition(value);
			_defaultScreenDefinition = value;
		}
	}

	public virtual IApplicationMenu[] Menus => _menus.ToArray();

	protected IApplicationMenu[] MenuCollection => _menus.ToArray();

	/// <summary>Facades override this to bind menus to the public block.</summary>
	protected virtual IApplicationBlock MenuOwner => this;

	public virtual void AddScreenDefinition(ApplicationScreenDefinition definition) {
		Guard.ArgumentNotNull(definition, nameof(definition));
		if (!ContainsScreenDefinition(definition))
			_screenDefinitions.Add(definition);
	}

	public virtual bool ContainsScreenDefinition(ApplicationScreenDefinition definition) =>
		_screenDefinitions.Any(candidate => ReferenceEquals(candidate, definition));

	/// <summary>Removes an unreferenced definition using reference identity; active navigation registrations must be changed first.</summary>
	public virtual void RemoveScreenDefinition(ApplicationScreenDefinition definition) {
		Guard.ArgumentNotNull(definition, nameof(definition));
		Guard.Argument(!ReferenceEquals(DefaultScreenDefinition, definition), nameof(definition), "The default screen definition cannot be removed.");
		Guard.Argument(!definition.Activators.Any(item => ReferenceEquals(item.Parent?.Parent, MenuOwner))
			&& !((IApplicationCommandProvider)this).ToolBarItems.OfType<IScreenMenuItem>().Any(item => ReferenceEquals(item.ScreenDefinition, definition)),
			nameof(definition), "A referenced screen definition cannot be removed.");
		_screenDefinitions.RemoveAll(candidate => ReferenceEquals(candidate, definition));
	}

	public virtual void AddMenu(IApplicationMenu menu) {
		Guard.ArgumentNotNull(menu, nameof(menu));
		Guard.ArgumentNotNull(menu.Items, nameof(menu));
		Guard.Argument(!_menus.Exists(candidate => ReferenceEquals(candidate, menu)), nameof(menu), "The menu is already in this block.");
		Guard.Argument(menu.Parent == null || ReferenceEquals(menu.Parent, MenuOwner), nameof(menu), "The menu already belongs to another block.");
		foreach (var item in menu.Items.OfType<IScreenMenuItem>())
			AddScreenDefinition(item.ScreenDefinition);
		menu.Parent = MenuOwner;
		_menus.Add(menu);
	}

	public virtual bool ContainsMenu(IApplicationMenu menu) => _menus.Exists(candidate => ReferenceEquals(candidate, menu));

	/// <summary>Removing navigation leaves block-owned definitions available for direct activation.</summary>
	public virtual void RemoveMenu(IApplicationMenu menu) {
		var index = _menus.FindIndex(candidate => ReferenceEquals(candidate, menu));
		if (index < 0)
			return;
		_menus.RemoveAt(index);
		if (ReferenceEquals(menu.Parent, MenuOwner))
			menu.Parent = null;
	}
}
