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
using Sphere10.Framework;
using Sphere10.Framework.Application.UI;

namespace Tools;

public static class UI {
	/// <summary>Validates unique plugin names and block ownership, preserving definition identity in a new membership array.</summary>
	public static IApplicationPlugin[] ValidatePlugins(IEnumerable<IApplicationPlugin> plugins) {
		Guard.ArgumentNotNull(plugins, nameof(plugins));
		var definitions = plugins.ToArray();
		var names = new HashSet<string>(StringComparer.Ordinal);
		var owners = new Dictionary<IApplicationBlock, string>(ReferenceEqualityComparer.Instance);
		foreach (var plugin in definitions) {
			Guard.ArgumentNotNull(plugin, nameof(plugins));
			Guard.Argument(!string.IsNullOrWhiteSpace(plugin.Name), nameof(plugins), "A plugin name is required.");
			Guard.Argument(names.Add(plugin.Name), nameof(plugins), $"Plugin name '{plugin.Name}' is registered more than once.");
			foreach (var block in ValidatePluginBlocks(plugin.Blocks)) {
				Guard.Argument(!owners.TryGetValue(block, out var owner), nameof(plugins),
					$"Block '{block.Name}' cannot belong to both plugin '{owner}' and '{plugin.Name}'.");
				owners.Add(block, plugin.Name);
			}
		}
		return definitions;
	}

	/// <summary>Validates completed startup registrations using their captured block membership without rereading plugin definitions.</summary>
	public static ApplicationPluginRegistration<TPlugin>[] ValidatePluginRegistrations<TPlugin>(IEnumerable<ApplicationPluginRegistration<TPlugin>> registrations)
		where TPlugin : class, IApplicationPlugin {
		Guard.ArgumentNotNull(registrations, nameof(registrations));
		var definitions = registrations.ToArray();
		var names = new HashSet<string>(StringComparer.Ordinal);
		var owners = new Dictionary<IApplicationBlock, string>(ReferenceEqualityComparer.Instance);
		foreach (var registration in definitions) {
			Guard.ArgumentNotNull(registration, nameof(registrations));
			var plugin = registration.Plugin;
			Guard.Argument(!string.IsNullOrWhiteSpace(plugin.Name), nameof(registrations), "A plugin name is required.");
			Guard.Argument(names.Add(plugin.Name), nameof(registrations), $"Plugin name '{plugin.Name}' is registered more than once.");
			foreach (var block in ValidatePluginBlocks(registration.Blocks)) {
				Guard.Argument(!owners.TryGetValue(block, out var owner), nameof(registrations),
					$"Block '{block.Name}' cannot belong to both plugin '{owner}' and '{plugin.Name}'.");
				owners.Add(block, plugin.Name);
			}
		}
		return definitions;
	}

	/// <summary>Validates portable block membership and returns an owned array without cloning platform definitions.</summary>
	public static IApplicationBlock[] ValidatePluginBlocks(IEnumerable<IApplicationBlock> blocks) {
		Guard.ArgumentNotNull(blocks, nameof(blocks));
		var definitions = blocks.ToArray();
		var members = new HashSet<IApplicationBlock>(ReferenceEqualityComparer.Instance);
		foreach (var block in definitions) {
			Guard.ArgumentNotNull(block, nameof(blocks));
			Guard.Argument(!string.IsNullOrWhiteSpace(block.Name), nameof(blocks), "A plugin block name is required.");
			Guard.Argument(members.Add(block), nameof(blocks), $"Block '{block.Name}' is registered more than once in a plugin.");
		}
		return definitions;
	}

	/// <summary>
	/// Merges broader-to-narrower command layers. Matching semantic keys keep their original position and take the
	/// narrower command; separators are normalized. Results own their membership but retain command identity.
	/// </summary>
	public static TItem[] MergeMenuItems<TItem>(params TItem[][] layers)
		where TItem : class, IApplicationMenuItem => MergeMenuItems(item => {
			Guard.ArgumentNotNullOrEmpty(item.Title, nameof(layers), "A command caption is required for merging.");
			return item.Title;
		}, layers);

	/// <summary>Lets adapters supply their own command identity without adding platform identifiers to shared metadata.</summary>
	public static TItem[] MergeMenuItems<TItem>(Func<TItem, object> keySelector, params TItem[][] layers)
		where TItem : class, IApplicationMenuItem {
		Guard.ArgumentNotNull(keySelector, nameof(keySelector));
		Guard.ArgumentNotNull(layers, nameof(layers));
		var result = new List<TItem>();
		var positions = new Dictionary<object, int>();
		foreach (var layer in layers) {
			Guard.ArgumentNotNull(layer, nameof(layers));
			foreach (var item in layer) {
				Guard.ArgumentNotNull(item, nameof(layers));
				if (item is IApplicationMenuSeparator) {
					result.Add(item);
					continue;
				}
				var key = keySelector(item);
				Guard.ArgumentNotNull(key, nameof(keySelector));
				if (positions.TryGetValue(key, out var index))
					result[index] = item;
				else {
					positions.Add(key, result.Count);
					result.Add(item);
				}
			}
		}
		var normalized = new List<TItem>();
		foreach (var item in result)
			if (item is not IApplicationMenuSeparator || normalized.Count > 0 && normalized[^1] is not IApplicationMenuSeparator)
				normalized.Add(item);
		if (normalized.Count > 0 && normalized[^1] is IApplicationMenuSeparator)
			normalized.RemoveAt(normalized.Count - 1);
		return normalized.ToArray();
	}

	/// <summary>
	/// Extends matching menus across application, block and screen layers. The narrower menu supplies its
	/// caption and platform metadata. The Help menu remains last, after screen contributions.
	/// </summary>
	public static TMenu[] MergeMenus<TMenu, TItem>(Func<TMenu, TItem[], TMenu> createMenu, params TMenu[][] layers)
		where TMenu : class, IApplicationMenu
		where TItem : class, IApplicationMenuItem => MergeMenus(createMenu, menu => {
			Guard.ArgumentNotNullOrEmpty(menu.Text, nameof(layers), "A menu caption is required for merging.");
			return menu.Text;
		}, item => {
			Guard.ArgumentNotNullOrEmpty(item.Title, nameof(layers), "A command caption is required for merging.");
			return item.Title;
		}, layers);

	/// <summary>Merges menus using adapter-supplied semantic keys.</summary>
	public static TMenu[] MergeMenus<TMenu, TItem>(Func<TMenu, TItem[], TMenu> createMenu,
		Func<TMenu, object> menuKeySelector, Func<TItem, object> itemKeySelector, params TMenu[][] layers)
		where TMenu : class, IApplicationMenu
		where TItem : class, IApplicationMenuItem {
		Guard.ArgumentNotNull(createMenu, nameof(createMenu));
		Guard.ArgumentNotNull(menuKeySelector, nameof(menuKeySelector));
		Guard.ArgumentNotNull(itemKeySelector, nameof(itemKeySelector));
		Guard.ArgumentNotNull(layers, nameof(layers));
		var result = new List<TMenu>();
		var positions = new Dictionary<object, int>();
		foreach (var layer in layers) {
			Guard.ArgumentNotNull(layer, nameof(layers));
			foreach (var menu in layer) {
				Guard.ArgumentNotNull(menu, nameof(layers));
				var key = menuKeySelector(menu);
				Guard.ArgumentNotNull(key, nameof(menuKeySelector));
				Guard.ArgumentNotNull(menu.Items, nameof(layers));
				var items = menu.Items.Cast<TItem>().ToArray();
				if (positions.TryGetValue(key, out var index))
					result[index] = createMenu(menu, MergeMenuItems(itemKeySelector, result[index].Items.Cast<TItem>().ToArray(), items));
				else {
					positions.Add(key, result.Count);
					result.Add(createMenu(menu, MergeMenuItems(itemKeySelector, items)));
				}
			}
		}
		return result.OrderBy(menu => string.Equals(menu.Text, "help", StringComparison.OrdinalIgnoreCase) ? 1 : 0).ToArray();
	}


	/// <summary>Application commands persist; block and screen commands follow the hosted screen, falling back to the browsed block.</summary>
	public static ApplicationCommandLayer[] GetCommandLayers(IApplication application) {
		Guard.ArgumentNotNull(application, nameof(application));
		var result = new List<ApplicationCommandLayer> { new(ApplicationCommandLevel.Application, application) };
		var host = application.ScreenHost;
		var screen = host?.ActiveScreen;
		var block = screen?.Block ?? host?.ActiveBlock;
		if (block != null)
			result.Add(new ApplicationCommandLayer(ApplicationCommandLevel.Block, block));
		if (screen != null)
			result.Add(new ApplicationCommandLayer(ApplicationCommandLevel.Screen, screen));
		return result.ToArray();
	}

	/// <summary>Composes menus while protecting application commands from contextual replacements.</summary>
	public static TMenu[] MergeApplicationMenus<TMenu, TItem>(Func<TMenu, TItem[], TMenu> createMenu, params ApplicationCommandLayer[] layers)
		where TMenu : class, IApplicationMenu
		where TItem : class, IApplicationMenuItem => MergeApplicationMenus(createMenu, menu => {
			Guard.ArgumentNotNullOrEmpty(menu.Text, nameof(layers), "A menu caption is required for merging.");
			return menu.Text;
		}, item => {
			Guard.ArgumentNotNullOrEmpty(item.Title, nameof(layers), "A command caption is required for merging.");
			return item.Title;
		}, layers);

	/// <summary>Adapters supply their own semantic keys; application and contextual commands have separate identity scopes.</summary>
	public static TMenu[] MergeApplicationMenus<TMenu, TItem>(Func<TMenu, TItem[], TMenu> createMenu,
		Func<TMenu, object> menuKeySelector, Func<TItem, object> itemKeySelector, params ApplicationCommandLayer[] layers)
		where TMenu : class, IApplicationMenu
		where TItem : class, IApplicationMenuItem {
		Guard.ArgumentNotNull(itemKeySelector, nameof(itemKeySelector));
		var ordered = OrderCommandLayers(layers);
		var applicationMenus = MergeMenus(createMenu, menuKeySelector, itemKeySelector,
			ordered.Where(layer => layer.Level == ApplicationCommandLevel.Application).Select(layer => layer.Provider.Menus.Cast<TMenu>().ToArray()).ToArray());
		var contextualMenus = MergeMenus(createMenu, menuKeySelector, itemKeySelector,
			ordered.Where(layer => layer.Level != ApplicationCommandLevel.Application).Select(layer => layer.Provider.Menus.Cast<TMenu>().ToArray()).ToArray());
		var applicationItems = applicationMenus.SelectMany(menu => menu.Items).ToHashSet(ReferenceEqualityComparer.Instance);
		return MergeMenus(createMenu, menuKeySelector, item => {
			var key = itemKeySelector(item);
			Guard.ArgumentNotNull(key, nameof(itemKeySelector));
			return (applicationItems.Contains(item), key);
		}, applicationMenus, contextualMenus);
	}

	/// <summary>Composes toolbar items while preserving global commands and contextual block-to-screen overrides.</summary>
	public static TItem[] MergeApplicationToolBarItems<TItem>(params ApplicationCommandLayer[] layers)
		where TItem : class, IApplicationMenuItem => MergeApplicationToolBarItems<TItem>(item => {
			Guard.ArgumentNotNullOrEmpty(item.Title, nameof(layers), "A command caption is required for merging.");
			return item.Title;
		}, layers);

	public static TItem[] MergeApplicationToolBarItems<TItem>(Func<TItem, object> itemKeySelector, params ApplicationCommandLayer[] layers)
		where TItem : class, IApplicationMenuItem {
		Guard.ArgumentNotNull(itemKeySelector, nameof(itemKeySelector));
		var ordered = OrderCommandLayers(layers);
		var applicationItems = MergeMenuItems(itemKeySelector,
			ordered.Where(layer => layer.Level == ApplicationCommandLevel.Application).Select(layer => layer.Provider.ToolBarItems.Cast<TItem>().ToArray()).ToArray());
		var contextualItems = MergeMenuItems(itemKeySelector,
			ordered.Where(layer => layer.Level != ApplicationCommandLevel.Application).Select(layer => layer.Provider.ToolBarItems.Cast<TItem>().ToArray()).ToArray());
		var applicationReferences = applicationItems.ToHashSet(ReferenceEqualityComparer.Instance);
		return MergeMenuItems<TItem>(item => {
			var key = itemKeySelector(item);
			Guard.ArgumentNotNull(key, nameof(itemKeySelector));
			return (applicationReferences.Contains(item), key);
		}, applicationItems, contextualItems);
	}

	private static ApplicationCommandLayer[] OrderCommandLayers(ApplicationCommandLayer[] layers) {
		Guard.ArgumentNotNull(layers, nameof(layers));
		Guard.Argument(layers.All(layer => layer != null), nameof(layers), "Command layers cannot be null.");
		foreach (var layer in layers) {
			Guard.ArgumentNotNull(layer.Provider.Menus, nameof(layers));
			Guard.ArgumentNotNull(layer.Provider.ToolBarItems, nameof(layers));
			Guard.Argument(layer.Provider.Menus.All(menu => menu != null && menu.Items != null), nameof(layers), "Menus and their item collections cannot be null.");
		}
		return layers.OrderBy(layer => layer.Level).ToArray();
	}

	/// <summary>Orders live definitions by plugin membership, then appends directly registered blocks in their supplied order.</summary>
	public static IApplicationBlock[] OrderApplicationBlocks(IEnumerable<IApplicationPlugin> plugins, IEnumerable<IApplicationBlock> blocks) {
		var definitions = ValidatePlugins(plugins);
		var available = ValidatePluginBlocks(blocks).ToDictionary(block => block, ReferenceEqualityComparer.Instance);
		var ordered = new List<IApplicationBlock>();
		foreach (var plugin in definitions)
			foreach (var block in plugin.Blocks)
				if (available.Remove(block, out var registered))
					ordered.Add(registered);
		ordered.AddRange(available.Values);
		return ordered.ToArray();
	}

	/// <summary>Orders registered blocks by captured startup membership, then appends directly registered blocks in their supplied order.</summary>
	public static IApplicationBlock[] OrderApplicationBlocks<TPlugin>(IEnumerable<ApplicationPluginRegistration<TPlugin>> registrations, IEnumerable<IApplicationBlock> blocks)
		where TPlugin : class, IApplicationPlugin {
		var definitions = ValidatePluginRegistrations(registrations);
		var available = ValidatePluginBlocks(blocks).ToDictionary(block => block, ReferenceEqualityComparer.Instance);
		var ordered = new List<IApplicationBlock>();
		foreach (var registration in definitions)
			foreach (var block in registration.Blocks)
				if (available.Remove(block, out var registered))
					ordered.Add(registered);
		ordered.AddRange(available.Values);
		return ordered.ToArray();
	}

	/// <summary>Returns block-owned screen definitions in declaration order, preserving reference identity.</summary>
	public static ApplicationScreenDefinition[] GetScreenDefinitions(IEnumerable<IApplicationBlock> blocks) {
		Guard.ArgumentNotNull(blocks, nameof(blocks));
		var definitions = new List<ApplicationScreenDefinition>();
		foreach (var block in blocks) {
			Guard.ArgumentNotNull(block, nameof(blocks));
			var owned = block.ScreenDefinitions;
			Guard.ArgumentNotNull(owned, nameof(blocks));
			Guard.Argument(owned.All(definition => definition != null), nameof(blocks), "Screen definitions cannot be null.");
			Guard.Argument(owned.Distinct(ReferenceEqualityComparer.Instance).Count() == owned.Length, nameof(blocks), "Screen definitions must be registered once by reference.");
			Guard.Argument(block.DefaultScreenDefinition == null || owned.Any(definition => ReferenceEquals(definition, block.DefaultScreenDefinition)),
				nameof(blocks), "The default screen definition must be owned by the block.");
			Guard.ArgumentNotNull(block.Menus, nameof(blocks));
			Guard.ArgumentNotNull(block.ToolBarItems, nameof(blocks));
			Guard.Argument(block.ToolBarItems.All(item => item != null), nameof(blocks), "Toolbar items cannot be null.");
			foreach (var menu in block.Menus) {
				Guard.ArgumentNotNull(menu, nameof(blocks));
				Guard.ArgumentNotNull(menu.Items, nameof(blocks));
				Guard.Argument(menu.Items.All(item => item != null), nameof(blocks), "Menu items cannot be null.");
			}
			foreach (var item in block.Menus.SelectMany(menu => menu.Items).Concat(block.ToolBarItems).OfType<IScreenMenuItem>())
				Guard.Argument(item.ScreenDefinition != null && owned.Any(definition => ReferenceEquals(definition, item.ScreenDefinition)),
					nameof(blocks), "Screen commands must reference a definition owned by their block.");
			definitions.AddRange(owned);
		}
		ApplicationScreenDefinition.ValidateDefinitions(definitions);
		return definitions.ToArray();
	}

	/// <summary>Returns the first block's marked startup screen. Permanent screens exclude empty placeholders.</summary>
	public static ApplicationScreenDefinition GetDefaultScreen(IEnumerable<IApplicationBlock> blocks) {
		Guard.ArgumentNotNull(blocks, nameof(blocks));
		var members = blocks.ToArray();
		var definitions = GetScreenDefinitions(members);
		var hasPermanent = definitions.Any(screen => screen.ActivationMode == ScreenActivationMode.PermanentSingleton);
		return members.Select(block => block.DefaultScreenDefinition).FirstOrDefault(screen => screen != null && (!hasPermanent || screen.ScreenKind != ScreenKind.Empty));
	}

	/// <summary>Returns a block's explicitly selected placeholder before the first remaining placeholder.</summary>
	public static ApplicationScreenDefinition GetEmptyScreen(IEnumerable<IApplicationBlock> blocks) {
		Guard.ArgumentNotNull(blocks, nameof(blocks));
		var members = blocks.ToArray();
		var definitions = GetScreenDefinitions(members);
		return members.Select(block => block.DefaultScreenDefinition).FirstOrDefault(screen => screen?.ScreenKind == ScreenKind.Empty)
			?? definitions.FirstOrDefault(screen => screen.ScreenKind == ScreenKind.Empty);
	}

	public static bool IsSingleton(ScreenActivationMode activationMode) {
		ValidateActivationMode(activationMode);
		return activationMode is ScreenActivationMode.SingleInstance or ScreenActivationMode.PermanentSingleton;
	}

	public static void ValidateScreenKind(ScreenKind screenKind) =>
		Guard.Argument(screenKind is ScreenKind.Normal or ScreenKind.Empty, nameof(screenKind), "Unknown screen kind.");

	public static void ValidateScreenPolicy(ScreenActivationMode? activationMode, ScreenKind screenKind) {
		ValidateScreenKind(screenKind);
		if (activationMode.HasValue)
			ValidateActivationMode(activationMode.Value);
		Guard.Argument(screenKind != ScreenKind.Empty || activationMode != ScreenActivationMode.PermanentSingleton,
			nameof(activationMode), "An empty-workspace screen cannot also be a permanently open screen.");
	}

	public static void ValidateScreenType(Type screenType, Type screenContract = null) {
		Guard.ArgumentNotNull(screenType, nameof(screenType));
		Guard.Argument(typeof(IApplicationScreen).IsAssignableFrom(screenType) && !screenType.IsAbstract && !screenType.ContainsGenericParameters,
			nameof(screenType), "A concrete application screen type is required.");
		if (screenContract != null)
			Guard.Argument(screenContract.IsAssignableFrom(screenType), nameof(screenType), $"The screen must implement or derive from {screenContract.Name}.");
	}

	public static void ValidateActivationMode(ScreenActivationMode activationMode) =>
		Guard.Argument(activationMode is ScreenActivationMode.SingleInstance or ScreenActivationMode.MultiInstance or ScreenActivationMode.PermanentSingleton, nameof(activationMode), "Unknown activation mode.");
}
