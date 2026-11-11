// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Serialization;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;
using SharedUI = global::Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Web.AspNetCore.Blazor;

/// <summary>Copies startup Blazor definitions into independent builder and circuit ownership graphs.</summary>
internal static class ApplicationBlockSnapshot {
	// Reserved browser identifier for the selected block default.
	internal const string DefaultScreenItemId = "__default";

	// Reserved prefix for screen definitions without navigation commands.
	internal const string DefinitionScreenItemPrefix = "__screen:";

	/// <summary>Copies each screen definition once and reuses that copy for menus, toolbar commands and the default.</summary>
	internal static ApplicationBlock Create(IApplicationBlock block) {
		Guard.ArgumentNotNull(block, nameof(block));
		Guard.ArgumentNotNullOrEmpty(block.Name, nameof(block), "Block name is required.");
		Guard.ArgumentNotNullOrEmpty(block.Id, nameof(block));
		Guard.ArgumentNotNull(block.ToolBarItems, nameof(block));
		ApplicationHelper.ResolveScreenReferences(block, block);
		var sourceDefinitions = ApplicationHelper.GetScreenDefinitions(new[] { block });
		var definitions = new Dictionary<SharedUI.ApplicationScreenDefinition, SharedUI.ApplicationScreenDefinition>(ReferenceEqualityComparer.Instance);
		foreach (var definition in sourceDefinitions) {
			ValidateScreenType(definition.ScreenType);
			SnapshotDefinition(definition, definitions);
		}

		// One map keeps every command and selected default within this copied registration graph.
		var menus = block.Menus.Select(menu => CreateMenu(menu, definitions)).ToArray();
		Guard.Argument(menus.Select(menu => menu.Id).Distinct(StringComparer.Ordinal).Count() == menus.Length, nameof(block), "Menu IDs must be unique within a block.");
		var registeredItems = EnumerateItems(menus.SelectMany(menu => menu.Items)).ToArray();
		Guard.Argument(registeredItems.Select(item => item.Id).Distinct(StringComparer.Ordinal).Count() == registeredItems.Length, nameof(block),
			"Menu item IDs, including grouped actions, must be unique within a block.");
		var toolbar = block.ToolBarItems.Select(item => CreateItem(item, definitions)).ToArray();
		var result = new BlockSnapshot(block) {
			Id = block.Id, Title = block.Title, Position = block.Position, IconUrl = block.IconUrl, Tooltip = block.Tooltip,
			Menus = menus, ToolBarItems = toolbar, BlockSettings = CopySettings(block.BlockSettings),
			ScreenDefinitions = sourceDefinitions.Select(definition => SnapshotDefinition(definition, definitions)).ToArray(),
			DefaultScreenDefinition = SnapshotDefinition(block.DefaultScreenDefinition, definitions),
			Source = block.Source
		};
		ApplicationHelper.GetScreenDefinitions(new[] { result });
		return result;
	}

	/// <summary>Validates startup definitions without constructing a service provider or a runtime UI.</summary>
	internal static IApplicationBlock[] CreateBlocks(IEnumerable<IApplicationBlock> blocks, bool orderByPosition = true) {
		Guard.ArgumentNotNull(blocks, nameof(blocks));
		var snapshots = blocks.Select(Create).Cast<IApplicationBlock>().ToArray();
		Guard.Argument(snapshots.Select(block => block.Id).Distinct(StringComparer.Ordinal).Count() == snapshots.Length, nameof(blocks), "Block IDs must be unique.");
		var definitions = ApplicationHelper.GetScreenDefinitions(snapshots);
		SharedUI.ApplicationScreenDefinition.ValidateDefinitions(definitions);
		return orderByPosition ? snapshots.OrderBy(block => block.Position).ToArray() : snapshots;
	}

	/// <summary>Creates circuit-owned settings values through their existing value serializer.</summary>
	internal static SettingsObject CopySettings(SettingsObject settings) {
		if (settings == null)
			return null;
		// Runtime owner copies retain conditional values while ordinary persistence keeps its existing predicates.
		using var writer = new StringWriter();
		var serializer = new SettingsObjectJsonSerializer();
		((DefaultContractResolver)serializer.ContractResolver).IgnoreShouldSerializeMembers = true;
		serializer.Serialize(writer, settings);
		using var reader = new Newtonsoft.Json.JsonTextReader(new StringReader(writer.ToString()));
		var copy = (SettingsObject)serializer.Deserialize(reader, settings.GetType());

		// An object-typed ID must keep its actual CLR type and identity rather than JSON's inferred representation.
		copy.ID = settings.ID;
		return copy;
	}

	/// <summary>Copies application menu metadata while preserving commands already owned by a block.</summary>
	internal static ApplicationMenu CreateApplicationMenu(IApplicationMenu menu) {
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

	/// <summary>Staged menu builders clone commands while retaining prototype definitions until their block graph is copied.</summary>
	internal static ApplicationMenu CreateBuilderMenu(IApplicationMenu menu) {
		var definitions = new Dictionary<SharedUI.ApplicationScreenDefinition, SharedUI.ApplicationScreenDefinition>(ReferenceEqualityComparer.Instance);
		// Several menu items may open the same screen; each definition is retained once.
		foreach (var definition in menu.Items.OfType<IScreenMenuItem>().Select(item => item.ScreenDefinition).Where(definition => definition != null).Distinct<SharedUI.ApplicationScreenDefinition>(ReferenceEqualityComparer.Instance))
			definitions.Add(definition, definition);
		return CreateMenu(menu, definitions);
	}

	/// <summary>Copies a standalone command with its own definition graph.</summary>
	internal static IApplicationMenuItem CreateItem(IApplicationMenuItem item)
		=> CreateItem(item, new Dictionary<SharedUI.ApplicationScreenDefinition, SharedUI.ApplicationScreenDefinition>(ReferenceEqualityComparer.Instance));

	/// <summary>Retains owned commands and freezes ordinary application action definitions.</summary>
	internal static IApplicationMenuItem CreateApplicationItem(IApplicationMenuItem item) {
		Guard.ArgumentNotNull(item, nameof(item));
		if (item.Parent?.Parent != null)
			return item;
		Guard.Argument(item is not IScreenMenuItem, nameof(item), "The screen command must belong to a block's menu.");
		if (item is ApplicationMenuGroup group) {
			Guard.ArgumentNotNullOrEmpty(item.Title, nameof(item), "Menu item title is required.");
			ValidateItemId(item);
			return CreateGroupSnapshot(group, group.Items.Select(CreateApplicationItem).ToArray());
		}
		return CreateItem(item);
	}

	/// <summary>Enumerates nested action groups in their displayed order.</summary>
	internal static IEnumerable<IApplicationMenuItem> EnumerateItems(IEnumerable<IApplicationMenuItem> items) {
		foreach (var item in items) {
			yield return item;
			if (item is ApplicationMenuGroup group)
				foreach (var child in EnumerateItems(group.Items))
					yield return child;
		}
	}

	/// <summary>Rejects screen types that cannot be hosted as Blazor application screens.</summary>
	internal static void ValidateScreenType(Type screenType) => ApplicationHelper.ValidateScreenType(screenType, typeof(IApplicationScreen));

	/// <summary>Maps successful startup contribution order to the host's immutable browser snapshots using platform-local IDs.</summary>
	internal static IApplicationBlock[] OrderBlocks(
		IEnumerable<ApplicationPluginRegistration<IBlazorPlugin>> registrations, IEnumerable<IApplicationBlock> blocks
	) {
		var registered = blocks.ToArray();
		var ordered = new List<IApplicationBlock>();
		foreach (var source in registrations.SelectMany(registration => registration.Blocks).Cast<IApplicationBlock>()) {
			var block = registered.FirstOrDefault(candidate => candidate.Id == source.Id);
			if (block != null && !ordered.Contains(block))
				ordered.Add(block);
		}
		ordered.AddRange(registered.Where(block => !ordered.Contains(block)));
		return ordered.ToArray();
	}

	/// <summary>Finds a block command by its stable browser ID.</summary>
	internal static IApplicationMenuItem ResolveMenuItem(IApplicationBlock block, string menuItemId) {
		Guard.ArgumentNotNullOrEmpty(menuItemId, nameof(menuItemId));
		var item = EnumerateItems(block.Menus.SelectMany(menu => menu.Items).Concat(block.ToolBarItems)).FirstOrDefault(candidate => candidate.Id == menuItemId);
		Guard.Argument(item != null, nameof(menuItemId), $"Unknown menu item '{menuItemId}' in block '{block.Id}'.");
		return item;
	}

	/// <summary>Resolves a stable registration route or a legacy command bookmark without fabricating a command.</summary>
	internal static SharedUI.ApplicationScreenDefinition ResolveScreenDefinition(IApplicationBlock block, string id) {
		if (id == DefaultScreenItemId)
			return block.DefaultScreenDefinition;
		if (id != null && id.StartsWith(DefinitionScreenItemPrefix, StringComparison.Ordinal)) {
			var key = Uri.UnescapeDataString(id[DefinitionScreenItemPrefix.Length..]);
			return block.ScreenDefinitions.SingleOrDefault(definition => definition.StableKey == key
				|| definition.StableKey == null && block.ScreenDefinitions.Count(candidate => candidate.ScreenType == definition.ScreenType) == 1
				&& definition.ScreenType.Assembly.GetName().Name + ":" + definition.ScreenType.FullName == key);
		}
		return EnumerateItems(block.Menus.SelectMany(menu => menu.Items).Concat(block.ToolBarItems))
			.OfType<BlazorScreenMenuItem>().SingleOrDefault(item => item.Id == id)?.ScreenDefinition;
	}

	/// <summary>Creates a caption-independent URL identity for menu-free registrations.</summary>
	internal static string GetDefinitionScreenId(IApplicationBlock block, SharedUI.ApplicationScreenDefinition definition)
		=> DefinitionScreenItemPrefix + Uri.EscapeDataString(ApplicationHelper.GetScreenRegistrationKey(block, definition));

	/// <summary>Checks whether a browser ID identifies an owned menu-free registration.</summary>
	internal static bool IsDefinitionScreenId(IApplicationBlock block, string id) {
		return id != null && (id == DefaultScreenItemId || id.StartsWith(DefinitionScreenItemPrefix, StringComparison.Ordinal))
			&& ResolveScreenDefinition(block, id) != null;
	}

	// Menu ownership is assigned by the concrete Blazor menu after all children are copied.
	private static ApplicationMenu CreateMenu(IApplicationMenu menu, Dictionary<SharedUI.ApplicationScreenDefinition, SharedUI.ApplicationScreenDefinition> definitions) {
		Guard.ArgumentNotNull(menu, nameof(menu));
		Guard.ArgumentNotNullOrEmpty(menu.Text, nameof(menu), "Menu text is required.");
		Guard.ArgumentNotNull(menu.Items, nameof(menu));
		return CreateMenuSnapshot(menu, menu.Items.Select(item => CreateItem(item, definitions)).ToArray());
	}

	// Copy supported command values while keeping screen references in the current graph's definition map.
	private static IApplicationMenuItem CreateItem(
		IApplicationMenuItem item, Dictionary<SharedUI.ApplicationScreenDefinition, SharedUI.ApplicationScreenDefinition> definitions
	) {
		Guard.ArgumentNotNull(item, nameof(item));
		Guard.Argument(item is IScreenMenuItem || !string.IsNullOrWhiteSpace(item.Title), nameof(item), "Menu item title is required.");
		if (item is IScreenMenuItem screenReference) {
			Guard.ArgumentNotNull(screenReference.Registration, nameof(item));
			ValidateScreenType(screenReference.Registration.ScreenType);
		}
		ValidateItemId(item);
		if (item is BlazorMenuSeparator separator)
			return new BlazorMenuSeparator { Id = item.Id, IsVisible = separator.IsVisible, CanExecute = separator.CanExecute };
		if (item is ApplicationMenuGroup group)
			return CreateGroupSnapshot(group, group.Items.Select(child => CreateItem(child, definitions)).ToArray());
		if (item is BlazorScreenMenuItem screen) {
			var snapshot = new BlazorScreenMenuItem {
				Id = screen.HasExplicitId ? item.Id : null,
				Title = screen.Title,
				Icon = screen.Icon,
				IsVisible = screen.IsVisible,
				CanExecute = screen.CanExecute,
				Registration = screen.Registration
			};
			if (screen.ScreenDefinition != null)
				snapshot.ResolveScreenDefinition(SnapshotDefinition(screen.ScreenDefinition, definitions));
			snapshot.CopySubscriptionsFrom(screen);
			return snapshot;
		}
		Guard.Argument(item is BlazorActionMenuItem, nameof(item), "A screen or action menu item is required.");
		var action = (BlazorActionMenuItem)item;
		Guard.Argument(action.Action != null || action.AsyncAction != null, nameof(item), "An action callback is required.");
		var actionSnapshot = new BlazorActionMenuItem {
			Id = item.Id,
			Title = action.Title,
			Icon = action.Icon,
			IsVisible = action.IsVisible,
			CanExecute = action.CanExecute,
			Action = action.Action,
			AsyncAction = action.AsyncAction
		};
		actionSnapshot.CopySubscriptionsFrom(action);
		return actionSnapshot;
	}

	// Clone each definition once, including menu-free registrations and aliased commands.
	private static SharedUI.ApplicationScreenDefinition SnapshotDefinition(
		SharedUI.ApplicationScreenDefinition definition, Dictionary<SharedUI.ApplicationScreenDefinition, SharedUI.ApplicationScreenDefinition> definitions
	) {
		if (definition == null)
			return null;
		if (!definitions.TryGetValue(definition, out var snapshot)) {
			snapshot = definition.Clone();
			snapshot.Settings = CopySettings(definition.Settings);
			definitions.Add(definition, snapshot);
		}
		return snapshot;
	}

	// Application projections borrow their owned commands; block/menu products own their copied children.
	private static ApplicationMenu CreateMenuSnapshot(IApplicationMenu menu, IApplicationMenuItem[] items, bool ownsItems = true) {
		Guard.ArgumentNotNullOrEmpty(menu.Id, nameof(menu));
		Guard.Argument(
			items.Select(item => item.Id).Distinct(StringComparer.Ordinal).Count() == items.Length, nameof(menu), "Menu item IDs must be unique within a menu."
		);
		return new ApplicationMenu(ownsItems) { Id = menu.Id, Text = menu.Text, Icon = menu.Icon, Items = items };
	}

	// Action groups retain their containing menu's ownership and prohibit nested screen registrations.
	private static ApplicationMenuGroup CreateGroupSnapshot(ApplicationMenuGroup item, IApplicationMenuItem[] items) {
		Guard.Argument(items.Length > 0 && items.All(child => child is not BlazorScreenMenuItem), nameof(item),
			"Action groups require children; screen registrations must remain top-level block menu items.");
		Guard.Argument(items.Select(child => child.Id).Distinct(StringComparer.Ordinal).Count() == items.Length, nameof(item), "Action group child IDs must be unique.");
		var snapshot = new ApplicationMenuGroup {
			Id = item.Id, Title = item.Title, Icon = item.Icon, IsVisible = item.IsVisible, CanExecute = item.CanExecute, Items = items
		};
		snapshot.CopySubscriptionsFrom(item);
		return snapshot;
	}

	// Reserved route names cannot be reused as ordinary command IDs.
	private static void ValidateItemId(IApplicationMenuItem item) {
		Guard.ArgumentNotNullOrEmpty(item.Id, nameof(item));
		Guard.Argument(item.Id != DefaultScreenItemId, nameof(item), "The default screen ID is reserved for browser navigation.");
		Guard.Argument(
			!item.Id.StartsWith(DefinitionScreenItemPrefix, StringComparison.Ordinal), nameof(item), "The definition screen ID prefix is reserved for browser navigation."
		);
	}

	#region Inner Types

	// Preserve a contributed block's exit policy without retaining its mutable ownership links.
	private sealed class BlockSnapshot : ApplicationBlock {
		private readonly Func<SharedUI.IApplication, CancellationToken, Task<bool>> _exitGuard;

		/// <summary>Retains the source block's exit policy while the copy owns its graph.</summary>
		public BlockSnapshot(IApplicationBlock source) => _exitGuard = source.CanExitAsync;

		/// <inheritdoc />
		public override Task<bool> CanExitAsync(SharedUI.IApplication application, CancellationToken cancellationToken = default)
			=> _exitGuard(application, cancellationToken);
	}

	#endregion
}
