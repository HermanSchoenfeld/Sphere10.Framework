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
using Microsoft.AspNetCore.Components;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Logic;

internal static class ApplicationBlockSnapshot {
	internal const string DefaultScreenItemId = "__default";

	internal static ApplicationBlock Create(IApplicationBlock block) {
		Guard.ArgumentNotNull(block, nameof(block));
		Guard.ArgumentNotNullOrEmpty(block.Title, nameof(block), "Block title is required.");
		var id = block.Id ?? block.Title;
		Guard.ArgumentNotNullOrEmpty(id, nameof(block), "Block ID is required.");
		Guard.ArgumentNotNull(block.Menus, nameof(block));
		if (block.DefaultScreen != null)
			ValidateScreenType(block.DefaultScreen);
		var menus = block.Menus.Select(CreateMenu).Cast<IApplicationMenu>().ToArray();
		Guard.Argument(menus.Select(menu => menu.Id).Distinct(StringComparer.Ordinal).Count() == menus.Length, nameof(block), "Menu IDs must be unique within a block.");
		var items = menus.SelectMany(menu => menu.Items).ToArray();
		Guard.Argument(items.Select(item => item.Id).Distinct(StringComparer.Ordinal).Count() == items.Length, nameof(block), "Menu item IDs must be unique within a block.");
		Guard.Argument(items.All(item => item.Id != DefaultScreenItemId), nameof(block), "The default screen item ID is reserved.");
		return new ApplicationBlock {
			Id = id, Title = block.Title, Position = block.Position, IconUrl = block.IconUrl, Tooltip = block.Tooltip,
			Menus = Array.AsReadOnly(menus), DefaultScreen = block.DefaultScreen, DefaultScreenTitle = block.DefaultScreenTitle
		};
	}

	internal static ApplicationMenu CreateMenu(IApplicationMenu menu) {
		Guard.ArgumentNotNull(menu, nameof(menu));
		Guard.ArgumentNotNullOrEmpty(menu.Text, nameof(menu), "Menu text is required.");
		Guard.ArgumentNotNull(menu.Items, nameof(menu));
		var id = menu.Id ?? menu.Text;
		Guard.ArgumentNotNullOrEmpty(id, nameof(menu), "Menu ID is required.");
		var items = menu.Items.Select(CreateItem).ToArray();
		return new ApplicationMenu { Id = id, Text = menu.Text, Icon = menu.Icon, Items = Array.AsReadOnly(items) };
	}

	internal static IApplicationMenuItem CreateItem(IApplicationMenuItem item) {
		Guard.ArgumentNotNull(item, nameof(item));
		Guard.ArgumentNotNullOrEmpty(item.Title, nameof(item), "Menu item title is required.");
		var id = item.Id ?? item.Title;
		Guard.ArgumentNotNullOrEmpty(id, nameof(item), "Menu item ID is required.");
		if (item is ShowScreenMenuItem screen) {
			ValidateScreenType(screen.ScreenType);
			Guard.Argument(screen.ActivationMode is ScreenActivationMode.SingleInstance or ScreenActivationMode.MultiInstance, nameof(item), "Unknown screen activation mode.");
			var snapshot = new ShowScreenMenuItem {
				Id = id, Title = screen.Title, Icon = screen.Icon, ScreenType = screen.ScreenType,
				ActivationMode = screen.ActivationMode, Parameters = screen.Parameters
			};
			snapshot.CopySubscriptionsFrom(screen);
			return snapshot;
		}
		Guard.Argument(item is ActionMenuItem, nameof(item), "A screen or action menu item is required.");
		var action = (ActionMenuItem)item;
		Guard.Argument(action.Action != null || action.AsyncAction != null, nameof(item), "An action callback is required.");
		var actionSnapshot = new ActionMenuItem { Id = id, Title = action.Title, Icon = action.Icon, Action = action.Action, AsyncAction = action.AsyncAction };
		actionSnapshot.CopySubscriptionsFrom(action);
		return actionSnapshot;
	}

	internal static void ValidateScreenType(Type screenType) {
		Guard.ArgumentNotNull(screenType, nameof(screenType));
		Guard.Argument(typeof(IComponent).IsAssignableFrom(screenType) && typeof(IApplicationScreen).IsAssignableFrom(screenType)
			&& !screenType.IsAbstract && !screenType.ContainsGenericParameters, nameof(screenType), "A concrete Blazor application screen is required.");
	}

	internal static ShowScreenMenuItem GetDefaultScreen(IApplicationBlock block) {
		var screens = block.Menus.SelectMany(menu => menu.Items).OfType<ShowScreenMenuItem>();
		if (block.DefaultScreen == null)
			return screens.FirstOrDefault();
		var matchingScreen = screens.FirstOrDefault(screen => screen.ScreenType == block.DefaultScreen);
		var defaultScreen = new ShowScreenMenuItem {
			Id = matchingScreen?.Id ?? DefaultScreenItemId, Title = block.DefaultScreenTitle ?? matchingScreen?.Title ?? block.DefaultScreen.Name,
			Icon = matchingScreen?.Icon, ScreenType = block.DefaultScreen, ActivationMode = matchingScreen?.ActivationMode ?? ScreenActivationMode.SingleInstance,
			Parameters = matchingScreen?.Parameters ?? new Dictionary<string, object>()
		};
		if (matchingScreen != null)
			defaultScreen.CopySubscriptionsFrom(matchingScreen);
		return defaultScreen;
	}
}

