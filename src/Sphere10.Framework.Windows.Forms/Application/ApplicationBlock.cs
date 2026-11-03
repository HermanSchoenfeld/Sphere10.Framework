// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Sphere10.Framework.Application.UI;
using SharedUI = global::Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Windows.Forms;

public class ApplicationBlock : ApplicationBlockBase, IApplicationBlock {
	private readonly List<IApplicationMenu> _menus = new();
	private IApplicationMenu[] _cachedMenus;
	private IApplicationMenuItem[] _cachedToolBarItems;

	public ApplicationBlock()
		: this(string.Empty, null, null, null) {
	}

	public ApplicationBlock(string title, Image image32x32, Image image8x8, string helpFile)
		: this(title, image32x32, image8x8, helpFile, null) {
	}

	public ApplicationBlock(string title, Image image32x32, Image image8x8, string helpFile, ApplicationMenu[] menus)
		: this(title, false, false, image32x32, image8x8, helpFile, menus) {
	}

	public ApplicationBlock(string title, bool showInToolStrip, bool showInMenuStrip, Image image32x32, Image image8x8, string helpFile, ApplicationMenu[] menus) {
		Name = title;
		ShowInMenuStrip = showInMenuStrip;
		ShowInToolStrip = showInToolStrip;
		HelpFileCHM = helpFile;
		Image32x32 = image32x32;
		Image8x8 = image8x8;
		if (menus != null)
			foreach (var menu in menus)
				AddMenu(menu);
	}

	public new virtual IApplication Parent {
		get => (IApplication)base.Parent;
		set => base.Parent = value;
	}

	public new virtual IApplicationPlugin Source {
		get => (IApplicationPlugin)base.Source;
		set => base.Source = value;
	}

	public bool ShowInToolStrip { get; set; }

	public virtual bool ShowInMenuStrip { get; set; }

	public virtual Image Image32x32 { get; set; }

	public Image Image8x8 { get; set; }

	/// <summary>Activates this block's fixed menu definitions once and preserves their identity for its lifetime.</summary>
	public virtual IApplicationMenu[] Menus => Tools.Array.Clone(_cachedMenus ??= GetMenus());

	/// <summary>Activates this block's own toolbar definitions independently of its current presentation.</summary>
	public virtual IApplicationMenuItem[] ToolBarItems => Tools.Array.Clone(_cachedToolBarItems ??= GetToolBarItems());

	public string HelpFileCHM { get; set; }

	protected internal IApplicationMenu[] MenuCollection => _menus.ToArray();

	/// <summary>Facades override this to bind menus to the public native block.</summary>
	protected virtual IApplicationBlock MenuOwner => this;

	/// <summary>Navigation references must be removed before their canonical screen definition.</summary>
	public override void RemoveScreenDefinition(SharedUI.ApplicationScreenDefinition definition) {
		Guard.ArgumentNotNull(definition, nameof(definition));
		Guard.Argument(!definition.Activators.Any(item => ReferenceEquals(item.Parent?.Parent, MenuOwner))
			&& !((IApplicationCommandProvider)this).ToolBarItems.OfType<IScreenMenuItem>().Any(item => ReferenceEquals(item.ScreenDefinition, definition)),
			nameof(definition), "A referenced screen definition cannot be removed.");
		base.RemoveScreenDefinition(definition);
	}

	public virtual void AddMenu(SharedUI.IApplicationMenu menu) {
		Guard.ArgumentNotNull(menu, nameof(menu));
		AddMenu(Guard.ArgumentCast<IApplicationMenu>(menu, nameof(menu)));
	}

	public virtual void AddMenu(IApplicationMenu menu) {
		Guard.Ensure(_cachedMenus == null && _cachedToolBarItems == null, "Command collections cannot change after activation.");
		Guard.ArgumentNotNull(menu, nameof(menu));
		Guard.ArgumentNotNull(menu.Items, nameof(menu));
		Guard.Argument(!_menus.Exists(candidate => ReferenceEquals(candidate, menu)), nameof(menu), "The menu is already in this block.");
		Guard.Argument(menu.Parent == null || ReferenceEquals(menu.Parent, MenuOwner), nameof(menu), "The menu already belongs to another block.");
		menu.Parent = MenuOwner;
		_menus.Add(menu);
	}

	public virtual bool ContainsMenu(SharedUI.IApplicationMenu menu) {
		return menu is IApplicationMenu nativeMenu && ContainsMenu(nativeMenu);
	}

	public virtual bool ContainsMenu(IApplicationMenu menu) {
		return _menus.Exists(candidate => ReferenceEquals(candidate, menu));
	}

	public virtual void RemoveMenu(SharedUI.IApplicationMenu menu) {
		if (menu is IApplicationMenu nativeMenu)
			RemoveMenu(nativeMenu);
	}

	/// <summary>Removing navigation leaves block-owned definitions available for direct activation.</summary>
	public virtual void RemoveMenu(IApplicationMenu menu) {
		Guard.Ensure(_cachedMenus == null && _cachedToolBarItems == null, "Command collections cannot change after activation.");
		var index = _menus.FindIndex(candidate => ReferenceEquals(candidate, menu));
		if (index < 0)
			return;
		_menus.RemoveAt(index);
		if (ReferenceEquals(menu.Parent, MenuOwner))
			menu.Parent = null;
	}

	/// <summary>Supplies the block's own menu definitions when its command cache is first accessed.</summary>
	protected virtual IApplicationMenu[] GetMenus() {
		return MenuCollection;
	}

	/// <summary>Supplies toolbar definitions declared directly by this block.</summary>
	protected virtual IApplicationMenuItem[] GetToolBarItems() {
		return Array.Empty<IApplicationMenuItem>();
	}

	public virtual void Dispose() {
		// Activated factory definitions belong to this owner too; unused factories remain unactivated.
		var menus = MenuCollection.Concat(_cachedMenus ?? Array.Empty<IApplicationMenu>())
			.Distinct<IApplicationMenu>(ReferenceEqualityComparer.Instance).ToArray();
		var menuItems = new HashSet<IApplicationMenuItem>(menus.SelectMany(menu => menu.Items), ReferenceEqualityComparer.Instance);
		foreach (var menu in menus)
			menu.Dispose();
		foreach (var item in _cachedToolBarItems ?? Array.Empty<IApplicationMenuItem>())
			if (menuItems.Add(item))
				item.Dispose();
		Image8x8?.Dispose();
		Image32x32?.Dispose();
	}
}
