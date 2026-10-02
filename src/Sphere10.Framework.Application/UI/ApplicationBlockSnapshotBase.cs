// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Linq;

namespace Sphere10.Framework.Application.UI;

/// <summary>Shared snapshot validation and traversal, with factories retaining platform-specific metadata.</summary>
public abstract class ApplicationBlockSnapshotBase<TBlock, TMenu, TItem> : IApplicationBlockSnapshot<TBlock, TMenu, TItem>
	where TBlock : class, IApplicationBlock
	where TMenu : class, IApplicationMenu
	where TItem : class, IApplicationMenuItem {
	public const string DefaultScreenItemId = ApplicationScreenDefinition.DefaultMenuItemId;

	public TBlock Create(TBlock block) {
		Guard.ArgumentNotNull(block, nameof(block));
		Guard.ArgumentNotNullOrEmpty(block.Name, nameof(block), "Block name is required.");
		var id = block.Id ?? block.Name;
		Guard.ArgumentNotNullOrEmpty(id, nameof(block), "Block ID is required.");
		var sourceMenus = block.Menus;
		Guard.ArgumentNotNull(sourceMenus, nameof(block));
		if (block.DefaultScreen != null) {
			ValidateScreenTypeCore(block.DefaultScreen);
			Tools.UI.ValidateScreenPolicy(block.DefaultScreenActivationMode, block.DefaultScreenKind);
		}
		var menus = sourceMenus.Cast<TMenu>().Select(CreateMenu).ToArray();
		Guard.Argument(menus.Select(menu => menu.Id).Distinct(StringComparer.Ordinal).Count() == menus.Length, nameof(block), "Menu IDs must be unique within a block.");
		var items = menus.SelectMany(menu => menu.Items).ToArray();
		Guard.Argument(items.Select(item => item.Id).Distinct(StringComparer.Ordinal).Count() == items.Length, nameof(block), "Menu item IDs must be unique within a block.");
		Guard.Argument(items.All(item => item.Id != DefaultScreenItemId), nameof(block), "The default screen item ID is reserved.");
		return CreateBlockSnapshot(block, id, menus);
	}

	public TMenu CreateMenu(TMenu menu) {
		Guard.ArgumentNotNull(menu, nameof(menu));
		Guard.ArgumentNotNullOrEmpty(menu.Text, nameof(menu), "Menu text is required.");
		var sourceItems = menu.Items;
		Guard.ArgumentNotNull(sourceItems, nameof(menu));
		var id = menu.Id ?? menu.Text;
		Guard.ArgumentNotNullOrEmpty(id, nameof(menu), "Menu ID is required.");
		return CreateMenuSnapshot(menu, id, sourceItems.Cast<TItem>().Select(CreateItem).ToArray());
	}

	public TItem CreateItem(TItem item) {
		Guard.ArgumentNotNull(item, nameof(item));
		Guard.ArgumentNotNullOrEmpty(item.Title, nameof(item), "Menu item title is required.");
		var id = item.Id ?? item.Title;
		Guard.ArgumentNotNullOrEmpty(id, nameof(item), "Menu item ID is required.");
		if (item is IScreenMenuItem screen) {
			ValidateScreenTypeCore(screen.ScreenType);
			Tools.UI.ValidateScreenPolicy(screen.ActivationMode, screen.ScreenKind);
		}
		return CreateItemSnapshot(item, id);
	}

	public TItem GetDefaultScreen(TBlock block) {
		Guard.ArgumentNotNull(block, nameof(block));
		var sourceMenus = block.Menus;
		Guard.ArgumentNotNull(sourceMenus, nameof(block));
		if (block.DefaultScreen != null) {
			ValidateScreenTypeCore(block.DefaultScreen);
			Tools.UI.ValidateScreenPolicy(block.DefaultScreenActivationMode, block.DefaultScreenKind);
		}
		var screens = sourceMenus.SelectMany(menu => menu.Items).OfType<IScreenMenuItem>();
		var result = block.DefaultScreen == null
			? (TItem)(screens.FirstOrDefault(screen => screen.IsDefault) ?? screens.FirstOrDefault(screen => screen.ScreenKind == ScreenKind.Normal) ?? screens.FirstOrDefault())
			: CreateDefaultScreen(block, (TItem)screens.FirstOrDefault(screen => screen.ScreenType == block.DefaultScreen));
		if (result != null) {
			Guard.Argument(result is IScreenMenuItem, nameof(block), "The default screen factory must return a screen menu item.");
			var screen = (IScreenMenuItem)result;
			ValidateScreenTypeCore(screen.ScreenType);
			Tools.UI.ValidateScreenPolicy(screen.ActivationMode, screen.ScreenKind);
		}
		return result;
	}

	/// <summary>Adds platform validation after the neutral screen contract has been checked.</summary>
	protected virtual void ValidateScreenType(Type screenType) {
	}

	protected abstract TBlock CreateBlockSnapshot(TBlock block, string id, TMenu[] menus);

	protected abstract TMenu CreateMenuSnapshot(TMenu menu, string id, TItem[] items);

	protected abstract TItem CreateItemSnapshot(TItem item, string id);

	protected abstract TItem CreateDefaultScreen(TBlock block, TItem matchingScreen);

	private void ValidateScreenTypeCore(Type screenType) {
		Tools.UI.ValidateScreenType(screenType);
		ValidateScreenType(screenType);
	}
}
