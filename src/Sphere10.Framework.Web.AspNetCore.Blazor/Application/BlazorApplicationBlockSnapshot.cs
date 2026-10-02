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

namespace Sphere10.Framework.Web.AspNetCore.Blazor;

/// <summary>Blazor factories over the portable snapshot traversal and validation.</summary>
internal static class BlazorApplicationBlockSnapshot {
	internal const string DefaultScreenItemId = ApplicationBlockSnapshotBase<IBlazorApplicationBlock, IBlazorApplicationMenu, IBlazorApplicationMenuItem>.DefaultScreenItemId;

	private static readonly BlazorSnapshot Snapshot = new();

	internal static BlazorApplicationBlock Create(IBlazorApplicationBlock block) => (BlazorApplicationBlock)Snapshot.Create(block);

	internal static BlazorApplicationMenu CreateMenu(IBlazorApplicationMenu menu) => (BlazorApplicationMenu)Snapshot.CreateMenu(menu);

	internal static IBlazorApplicationMenuItem CreateItem(IBlazorApplicationMenuItem item) => Snapshot.CreateItem(item);

	internal static void ValidateScreenType(Type screenType) => Tools.UI.ValidateScreenType(screenType, typeof(IBlazorApplicationScreen));

	internal static BlazorScreenMenuItem GetDefaultScreen(IBlazorApplicationBlock block) => (BlazorScreenMenuItem)Snapshot.GetDefaultScreen(block);

	private sealed class BlazorSnapshot : ApplicationBlockSnapshotBase<IBlazorApplicationBlock, IBlazorApplicationMenu, IBlazorApplicationMenuItem> {
		protected override void ValidateScreenType(Type screenType) => BlazorApplicationBlockSnapshot.ValidateScreenType(screenType);

		protected override IBlazorApplicationBlock CreateBlockSnapshot(IBlazorApplicationBlock block, string id, IBlazorApplicationMenu[] menus) {
			Guard.ArgumentNotNull(block.ToolBarItems, nameof(block));
			var toolbar = block.ToolBarItems.Select(CreateItem).ToArray();
			foreach (var screen in toolbar.OfType<BlazorScreenMenuItem>())
				Guard.Argument(menus.SelectMany(menu => menu.Items).OfType<BlazorScreenMenuItem>()
					.Any(item => item.Id == screen.Id && item.ScreenType == screen.ScreenType && item.ActivationMode == screen.ActivationMode),
					nameof(block), "A toolbar screen command must match a screen registered in the block's menus.");
			return new BlazorApplicationBlock {
				Id = id, Title = block.Title, Position = block.Position, IconUrl = block.IconUrl, Tooltip = block.Tooltip,
				Menus = menus, ToolBarItems = toolbar, DefaultScreen = block.DefaultScreen, DefaultScreenTitle = block.DefaultScreenTitle,
				DefaultScreenActivationMode = block.DefaultScreenActivationMode, DefaultScreenKind = block.DefaultScreenKind
			};
		}

		protected override IBlazorApplicationMenu CreateMenuSnapshot(IBlazorApplicationMenu menu, string id, IBlazorApplicationMenuItem[] items) =>
			new BlazorApplicationMenu { Id = id, Text = menu.Text, Icon = menu.Icon, Items = items };

		protected override IBlazorApplicationMenuItem CreateItemSnapshot(IBlazorApplicationMenuItem item, string id) {
			if (item is BlazorMenuSeparator)
				return new BlazorMenuSeparator { Id = id };
			if (item is BlazorScreenMenuItem screen) {
				var snapshot = new BlazorScreenMenuItem {
					Id = id, Title = screen.Title, Icon = screen.Icon, ScreenType = screen.ScreenType,
					ActivationMode = screen.ActivationMode, ScreenKind = screen.ScreenKind, IsDefault = screen.IsDefault, Parameters = screen.Parameters
				};
				snapshot.CopySubscriptionsFrom(screen);
				return snapshot;
			}
			Guard.Argument(item is BlazorActionMenuItem, nameof(item), "A screen or action menu item is required.");
			var action = (BlazorActionMenuItem)item;
			Guard.Argument(action.Action != null || action.AsyncAction != null, nameof(item), "An action callback is required.");
			var actionSnapshot = new BlazorActionMenuItem { Id = id, Title = action.Title, Icon = action.Icon, Action = action.Action, AsyncAction = action.AsyncAction };
			actionSnapshot.CopySubscriptionsFrom(action);
			return actionSnapshot;
		}

		protected override IBlazorApplicationMenuItem CreateDefaultScreen(IBlazorApplicationBlock block, IBlazorApplicationMenuItem matchingScreen) {
			var screen = (BlazorScreenMenuItem)matchingScreen;
			var defaultScreen = new BlazorScreenMenuItem {
				Id = screen?.Id ?? DefaultScreenItemId, Title = block.DefaultScreenTitle ?? screen?.Title ?? block.DefaultScreen.Name,
				Icon = screen?.Icon, ScreenType = block.DefaultScreen,
				ActivationMode = block.DefaultScreenActivationMode ?? screen?.ActivationMode ?? ScreenActivationMode.MultiInstance,
				ScreenKind = block.DefaultScreenKind == ScreenKind.Normal ? screen?.ScreenKind ?? ScreenKind.Normal : block.DefaultScreenKind, IsDefault = true,
				Parameters = screen?.Parameters ?? new Dictionary<string, object>()
			};
			if (screen != null)
				defaultScreen.CopySubscriptionsFrom(screen);
			return defaultScreen;
		}
	}
}
