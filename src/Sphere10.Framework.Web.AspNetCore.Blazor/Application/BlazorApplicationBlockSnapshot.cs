// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Web.AspNetCore.Blazor;

/// <summary>Blazor factories over the portable snapshot traversal and validation.</summary>
internal static class BlazorApplicationBlockSnapshot {
	internal const string DefaultScreenItemId = "__default";
	internal const string DefinitionScreenItemPrefix = "__screen:";

	private static readonly BlazorSnapshot Snapshot = new();

	internal static BlazorApplicationBlock Create(IBlazorApplicationBlock block) => (BlazorApplicationBlock)Snapshot.Create(block);

	/// <summary>Validates startup definitions without constructing a service provider or a runtime UI.</summary>
	internal static IBlazorApplicationBlock[] CreateBlocks(IEnumerable<IBlazorApplicationBlock> blocks, bool orderByPosition = true) {
		Guard.ArgumentNotNull(blocks, nameof(blocks));
		var snapshots = blocks.Select(Create).Cast<IBlazorApplicationBlock>().ToArray();
		Guard.Argument(snapshots.Select(block => block.Id).Distinct(StringComparer.Ordinal).Count() == snapshots.Length, nameof(blocks), "Block IDs must be unique.");
		var definitions = Tools.UI.GetScreenDefinitions(snapshots);
		ApplicationScreenDefinition.ValidateDefinitions(definitions);
		return orderByPosition ? snapshots.OrderBy(block => block.Position).ToArray() : snapshots;
	}

	internal static BlazorApplicationMenu CreateApplicationMenu(IBlazorApplicationMenu menu) => Snapshot.CreateApplicationMenu(menu);

	internal static BlazorApplicationMenu CreateBuilderMenu(IBlazorApplicationMenu menu) => Snapshot.CreateBuilderMenu(menu);

	internal static IBlazorApplicationMenuItem CreateItem(IBlazorApplicationMenuItem item) => Snapshot.CreateItem(item);

	internal static IBlazorApplicationMenuItem CreateApplicationItem(IBlazorApplicationMenuItem item) => Snapshot.CreateApplicationItem(item);

	internal static IEnumerable<IBlazorApplicationMenuItem> EnumerateItems(IEnumerable<IBlazorApplicationMenuItem> items) {
		foreach (var item in items) {
			yield return item;
			if (item is BlazorApplicationMenuGroup group)
				foreach (var child in EnumerateItems(group.Items))
					yield return child;
		}
	}

	internal static void ValidateScreenType(Type screenType) => Tools.UI.ValidateScreenType(screenType, typeof(IBlazorApplicationScreen));

	/// <summary>Maps successful startup contribution order to the host's immutable browser snapshots using platform-local IDs.</summary>
	internal static IBlazorApplicationBlock[] OrderBlocks(IEnumerable<ApplicationPluginRegistration<IBlazorPlugin>> registrations, IEnumerable<IBlazorApplicationBlock> blocks) {
		var registered = blocks.ToArray();
		var ordered = new List<IBlazorApplicationBlock>();
		foreach (var source in registrations.SelectMany(registration => registration.Blocks).Cast<IBlazorApplicationBlock>()) {
			var block = registered.FirstOrDefault(candidate => candidate.Id == source.Id);
			if (block != null && !ordered.Contains(block))
				ordered.Add(block);
		}
		ordered.AddRange(registered.Where(block => !ordered.Contains(block)));
		return ordered.ToArray();
	}

	internal static BlazorScreenMenuItem GetDefaultScreen(IBlazorApplicationBlock block) {
		var definition = block.DefaultScreenDefinition;
		var screens = block.ScreenDefinitions.SelectMany(candidate => candidate.Activators).OfType<BlazorScreenMenuItem>()
			.Where(item => ReferenceEquals(item.Parent?.Parent, block)).ToArray();
		if (definition == null)
			return screens.FirstOrDefault(screen => screen.IsDefault) ?? screens.FirstOrDefault(screen => screen.ScreenKind == ScreenKind.Normal) ?? screens.FirstOrDefault();
		var matching = screens.FirstOrDefault(screen => screen.IsDefault && ReferenceEquals(screen.ScreenDefinition, definition))
			?? screens.FirstOrDefault(screen => ReferenceEquals(screen.ScreenDefinition, definition)) ?? screens.FirstOrDefault(screen => screen.ScreenType == definition.ScreenType);
		return Guard.ArgumentCast<BlazorApplicationBlock>(block, nameof(block))
			.GetGeneratedScreenCommand(definition, matching?.Id ?? DefaultScreenItemId, matching, isDefault: true);
	}

	internal static IBlazorApplicationMenuItem ResolveMenuItem(IBlazorApplicationBlock block, string menuItemId) {
		Guard.ArgumentNotNullOrEmpty(menuItemId, nameof(menuItemId));
		var item = EnumerateItems(block.Menus.SelectMany(menu => menu.Items).Concat(block.ToolBarItems)).FirstOrDefault(candidate => candidate.Id == menuItemId);
		var defaultScreen = block.DefaultScreen == null ? null : GetDefaultScreen(block);
		if (defaultScreen?.Id == menuItemId)
			item = defaultScreen;
		if (item == null && TryGetDefinitionIndex(block, menuItemId, out var index))
			item = GetDefinitionMenuItem(block, block.ScreenDefinitions[index]);
		Guard.Argument(item != null, nameof(menuItemId), $"Unknown menu item '{menuItemId}' in block '{block.Id}'.");
		return item;
	}

	internal static BlazorScreenMenuItem GetDefinitionMenuItem(IBlazorApplicationBlock block, ApplicationScreenDefinition definition) {
		var index = Array.FindIndex(block.ScreenDefinitions, candidate => ReferenceEquals(candidate, definition));
		Guard.Argument(index >= 0, nameof(definition), "The screen definition is not owned by this block.");
		return Guard.ArgumentCast<BlazorApplicationBlock>(block, nameof(block))
			.GetGeneratedScreenCommand(definition, DefinitionScreenItemPrefix + index.ToString(CultureInfo.InvariantCulture));
	}

	internal static bool IsDefinitionScreenId(IBlazorApplicationBlock block, string id) => TryGetDefinitionIndex(block, id, out _);

	private static bool TryGetDefinitionIndex(IBlazorApplicationBlock block, string id, out int index) {
		index = -1;
		return id != null && id.StartsWith(DefinitionScreenItemPrefix, StringComparison.Ordinal)
			&& int.TryParse(id[DefinitionScreenItemPrefix.Length..], NumberStyles.None, CultureInfo.InvariantCulture, out index)
			&& index >= 0 && index < block.ScreenDefinitions.Length;
	}

	private sealed class BlockSnapshot : BlazorApplicationBlock {
		private readonly Func<IApplication, CancellationToken, Task<bool>> _exitGuard;

		public BlockSnapshot(IBlazorApplicationBlock source) => _exitGuard = source.CanExitAsync;

		public override Task<bool> CanExitAsync(IApplication application, CancellationToken cancellationToken = default) => _exitGuard(application, cancellationToken);
	}

	private sealed class BlazorSnapshot : ApplicationBlockSnapshotBase<IBlazorApplicationBlock, IBlazorApplicationMenu, IBlazorApplicationMenuItem> {
		/// <summary>Copies application menu metadata while preserving commands already owned by a block.</summary>
		public BlazorApplicationMenu CreateApplicationMenu(IBlazorApplicationMenu menu) {
			Guard.ArgumentNotNull(menu, nameof(menu));
			Guard.ArgumentNotNullOrEmpty(menu.Text, nameof(menu), "Menu text is required.");
			var sourceItems = menu.Items;
			Guard.ArgumentNotNull(sourceItems, nameof(menu));
			var items = sourceItems.Select(CreateApplicationItem).ToArray();
			var projection = CreateMenuSnapshot(menu, items, ownsItems: false);
			projection.Parent = menu.Parent;
			for (var index = 0; index < items.Length; index++)
				if (!ReferenceEquals(items[index], sourceItems[index]))
					items[index].Parent = projection;
			return projection;
		}

		/// <summary>Retains owned commands and freezes ordinary application action definitions.</summary>
		public IBlazorApplicationMenuItem CreateApplicationItem(IBlazorApplicationMenuItem item) {
			Guard.ArgumentNotNull(item, nameof(item));
			if (item.Parent?.Parent != null)
				return item;
			Guard.Argument(item is not IScreenMenuItem, nameof(item), "The screen command must belong to a block's menu.");
			if (item is BlazorApplicationMenuGroup group) {
				Guard.ArgumentNotNullOrEmpty(item.Title, nameof(item), "Menu item title is required.");
				ValidateItemId(item);
				return CreateGroupSnapshot(group, group.Items.Select(CreateApplicationItem).ToArray());
			}
			return CreateItem(item);
		}

		/// <summary>Staged menu builders clone commands while retaining prototype definitions until their block graph is copied.</summary>
		public BlazorApplicationMenu CreateBuilderMenu(IBlazorApplicationMenu menu) {
			var definitions = new Dictionary<ApplicationScreenDefinition, ApplicationScreenDefinition>(ReferenceEqualityComparer.Instance);
			foreach (var definition in menu.Items.OfType<IScreenMenuItem>().Select(item => item.ScreenDefinition))
				definitions.TryAdd(definition, definition);
			return (BlazorApplicationMenu)CreateMenuSnapshot(menu, menu.Items.Select(item => CreateItem(item, definitions)).ToArray());
		}

		protected override void ValidateScreenType(Type screenType) => BlazorApplicationBlockSnapshot.ValidateScreenType(screenType);

		protected override IBlazorApplicationBlock CreateBlockSnapshot(IBlazorApplicationBlock block, IBlazorApplicationMenu[] menus,
			Dictionary<ApplicationScreenDefinition, ApplicationScreenDefinition> definitions) {
			Guard.ArgumentNotNullOrEmpty(block.Id, nameof(block));
			Guard.ArgumentNotNull(block.ToolBarItems, nameof(block));
			Guard.Argument(menus.Select(menu => menu.Id).Distinct(StringComparer.Ordinal).Count() == menus.Length, nameof(block), "Menu IDs must be unique within a block.");
			var registeredItems = EnumerateItems(menus.SelectMany(menu => menu.Items)).ToArray();
			Guard.Argument(registeredItems.Select(item => item.Id).Distinct(StringComparer.Ordinal).Count() == registeredItems.Length, nameof(block),
				"Menu item IDs, including grouped actions, must be unique within a block.");
			var toolbar = block.ToolBarItems.Select(item => CreateItem(item, definitions)).ToArray();
			return new BlockSnapshot(block) {
				Id = block.Id, Title = block.Title, Position = block.Position, IconUrl = block.IconUrl, Tooltip = block.Tooltip,
				Menus = menus, ToolBarItems = toolbar, DefaultScreen = block.DefaultScreen, DefaultScreenTitle = block.DefaultScreenTitle,
				DefaultScreenActivationMode = block.DefaultScreenActivationMode, DefaultScreenKind = block.DefaultScreenKind,
				ScreenDefinitions = block.ScreenDefinitions.Select(definition => SnapshotDefinition(definition, definitions)).ToArray(),
				DefaultScreenDefinition = SnapshotDefinition(block.DefaultScreenDefinition, definitions)
					?? menus.SelectMany(menu => menu.Items).OfType<BlazorScreenMenuItem>().FirstOrDefault(item => item.IsDefault)?.ScreenDefinition
			};
		}

		protected override IBlazorApplicationMenu CreateMenuSnapshot(IBlazorApplicationMenu menu, IBlazorApplicationMenuItem[] items)
			=> CreateMenuSnapshot(menu, items, ownsItems: true);

		protected override IBlazorApplicationMenuItem CreateItemSnapshot(IBlazorApplicationMenuItem item,
			Dictionary<ApplicationScreenDefinition, ApplicationScreenDefinition> definitions) {
			ValidateItemId(item);
			if (item is BlazorMenuSeparator separator)
				return new BlazorMenuSeparator { Id = item.Id, IsVisible = separator.IsVisible, CanExecute = separator.CanExecute };
			if (item is BlazorApplicationMenuGroup group)
				return CreateGroupSnapshot(group, group.Items.Select(child => CreateItem(child, definitions)).ToArray());
			if (item is BlazorScreenMenuItem screen) {
				var snapshot = new BlazorScreenMenuItem {
					Id = item.Id, Title = screen.Title, Icon = screen.Icon, IsVisible = screen.IsVisible, CanExecute = screen.CanExecute,
					ScreenDefinition = SnapshotDefinition(screen.ScreenDefinition, definitions),
					ActivationMode = screen.ActivationMode, ScreenKind = screen.ScreenKind, IsDefault = screen.IsDefault, Parameters = screen.Parameters
				};
				snapshot.CopySubscriptionsFrom(screen);
				return snapshot;
			}
			Guard.Argument(item is BlazorActionMenuItem, nameof(item), "A screen or action menu item is required.");
			var action = (BlazorActionMenuItem)item;
			Guard.Argument(action.Action != null || action.AsyncAction != null, nameof(item), "An action callback is required.");
			var actionSnapshot = new BlazorActionMenuItem { Id = item.Id, Title = action.Title, Icon = action.Icon, IsVisible = action.IsVisible, CanExecute = action.CanExecute, Action = action.Action, AsyncAction = action.AsyncAction };
			actionSnapshot.CopySubscriptionsFrom(action);
			return actionSnapshot;
		}

		private static BlazorApplicationMenu CreateMenuSnapshot(IBlazorApplicationMenu menu, IBlazorApplicationMenuItem[] items, bool ownsItems) {
			Guard.ArgumentNotNullOrEmpty(menu.Id, nameof(menu));
			Guard.Argument(items.Select(item => item.Id).Distinct(StringComparer.Ordinal).Count() == items.Length, nameof(menu), "Menu item IDs must be unique within a menu.");
			return new BlazorApplicationMenu(ownsItems) { Id = menu.Id, Text = menu.Text, Icon = menu.Icon, Items = items };
		}

		private static BlazorApplicationMenuGroup CreateGroupSnapshot(BlazorApplicationMenuGroup item, IBlazorApplicationMenuItem[] items) {
			Guard.Argument(items.Length > 0 && items.All(child => child is not BlazorScreenMenuItem), nameof(item),
				"Action groups require children; screen registrations must remain top-level block menu items.");
			Guard.Argument(items.Select(child => child.Id).Distinct(StringComparer.Ordinal).Count() == items.Length, nameof(item), "Action group child IDs must be unique.");
			var snapshot = new BlazorApplicationMenuGroup {
				Id = item.Id, Title = item.Title, Icon = item.Icon, IsVisible = item.IsVisible, CanExecute = item.CanExecute, Items = items
			};
			snapshot.CopySubscriptionsFrom(item);
			return snapshot;
		}

		private static void ValidateItemId(IBlazorApplicationMenuItem item) {
			Guard.ArgumentNotNullOrEmpty(item.Id, nameof(item));
			Guard.Argument(item.Id != DefaultScreenItemId, nameof(item), "The default screen ID is reserved for browser navigation.");
			Guard.Argument(!item.Id.StartsWith(DefinitionScreenItemPrefix, StringComparison.Ordinal), nameof(item), "The definition screen ID prefix is reserved for browser navigation.");
		}
	}
}
