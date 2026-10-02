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
		var owners = new Dictionary<string, string>(StringComparer.Ordinal);
		foreach (var plugin in definitions) {
			Guard.ArgumentNotNull(plugin, nameof(plugins));
			Guard.Argument(!string.IsNullOrWhiteSpace(plugin.Name), nameof(plugins), "A plugin name is required.");
			Guard.Argument(names.Add(plugin.Name), nameof(plugins), $"Plugin name '{plugin.Name}' is registered more than once.");
			foreach (var block in ValidatePluginBlocks(plugin.Blocks)) {
				Guard.Argument(!owners.TryGetValue(block.Id, out var owner), nameof(plugins),
					$"Block '{block.Id}' cannot belong to both plugin '{owner}' and '{plugin.Name}'.");
				owners.Add(block.Id, plugin.Name);
			}
		}
		return definitions;
	}

	/// <summary>Validates portable block membership and returns an owned array without cloning platform definitions.</summary>
	public static IApplicationBlock[] ValidatePluginBlocks(IEnumerable<IApplicationBlock> blocks) {
		Guard.ArgumentNotNull(blocks, nameof(blocks));
		var definitions = blocks.ToArray();
		var ids = new HashSet<string>(StringComparer.Ordinal);
		foreach (var block in definitions) {
			Guard.ArgumentNotNull(block, nameof(blocks));
			Guard.Argument(!string.IsNullOrWhiteSpace(block.Id), nameof(blocks), "A plugin block ID is required.");
			Guard.Argument(ids.Add(block.Id), nameof(blocks), $"Block ID '{block.Id}' is registered more than once in a plugin.");
		}
		return definitions;
	}

	/// <summary>Finds a collision-free name for the plugin that adapts standalone application blocks.</summary>
	public static string GetImplicitPluginName(IEnumerable<IApplicationPlugin> plugins) {
		var names = ValidatePlugins(plugins).Select(plugin => plugin.Name).ToHashSet(StringComparer.Ordinal);
		var name = "Application";
		for (var suffix = 2; names.Contains(name); suffix++)
			name = $"Application {suffix}";
		return name;
	}

	/// <summary>
	/// Merges broader-to-narrower command layers. Matching IDs keep their original position and take the
	/// narrower command; separators are normalized. Results own their membership but retain command identity.
	/// </summary>
	public static TItem[] MergeMenuItems<TItem>(params TItem[][] layers) where TItem : class, IApplicationMenuItem {
		Guard.ArgumentNotNull(layers, nameof(layers));
		var result = new List<TItem>();
		var positions = new Dictionary<string, int>(StringComparer.Ordinal);
		foreach (var layer in layers) {
			Guard.ArgumentNotNull(layer, nameof(layers));
			foreach (var item in layer) {
				Guard.ArgumentNotNull(item, nameof(layers));
				if (item is IApplicationMenuSeparator) {
					result.Add(item);
					continue;
				}
				Guard.ArgumentNotNullOrEmpty(item.Id, nameof(layers), "Command IDs are required for merging.");
				if (positions.TryGetValue(item.Id, out var index))
					result[index] = item;
				else {
					positions.Add(item.Id, result.Count);
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
	/// caption and platform metadata. A menu whose ID is "help" remains last, after screen contributions.
	/// </summary>
	public static TMenu[] MergeMenus<TMenu, TItem>(Func<TMenu, TItem[], TMenu> createMenu, params TMenu[][] layers)
		where TMenu : class, IApplicationMenu
		where TItem : class, IApplicationMenuItem {
		Guard.ArgumentNotNull(createMenu, nameof(createMenu));
		Guard.ArgumentNotNull(layers, nameof(layers));
		var result = new List<TMenu>();
		var positions = new Dictionary<string, int>(StringComparer.Ordinal);
		foreach (var layer in layers) {
			Guard.ArgumentNotNull(layer, nameof(layers));
			foreach (var menu in layer) {
				Guard.ArgumentNotNull(menu, nameof(layers));
				Guard.ArgumentNotNullOrEmpty(menu.Id, nameof(layers), "Menu IDs are required for merging.");
				Guard.ArgumentNotNull(menu.Items, nameof(layers));
				var items = menu.Items.Cast<TItem>().ToArray();
				if (positions.TryGetValue(menu.Id, out var index))
					result[index] = createMenu(menu, MergeMenuItems(result[index].Items.Cast<TItem>().ToArray(), items));
				else {
					positions.Add(menu.Id, result.Count);
					result.Add(createMenu(menu, MergeMenuItems(items)));
				}
			}
		}
		return result.OrderBy(menu => string.Equals(menu.Id, "help", StringComparison.OrdinalIgnoreCase) ? 1 : 0).ToArray();
	}


	/// <summary>Orders live definitions by plugin membership, then appends directly registered blocks in their supplied order.</summary>
	public static IApplicationBlock[] OrderApplicationBlocks(IEnumerable<IApplicationPlugin> plugins, IEnumerable<IApplicationBlock> blocks) {
		var definitions = ValidatePlugins(plugins);
		var available = ValidatePluginBlocks(blocks).ToDictionary(block => block.Id, StringComparer.Ordinal);
		var ordered = new List<IApplicationBlock>();
		foreach (var plugin in definitions)
			foreach (var block in plugin.Blocks)
				if (available.Remove(block.Id, out var registered))
					ordered.Add(registered);
		ordered.AddRange(available.Values);
		return ordered.ToArray();
	}

	/// <summary>Resolves defaults and screen policies once, preserving block and menu declaration order.</summary>
	public static ApplicationScreenDefinition[] GetScreenDefinitions(IEnumerable<IApplicationBlock> blocks) {
		Guard.ArgumentNotNull(blocks, nameof(blocks));
		var definitions = new List<ApplicationScreenDefinition>();
		foreach (var block in blocks) {
			Guard.ArgumentNotNull(block, nameof(blocks));
			var menus = block.Menus;
			Guard.ArgumentNotNull(menus, nameof(blocks));
			foreach (var menu in menus) {
				Guard.ArgumentNotNull(menu, nameof(blocks));
				Guard.ArgumentNotNull(menu.Items, nameof(blocks));
				Guard.Argument(menu.Items.All(item => item != null), nameof(blocks), "Menu items cannot be null.");
			}
			var screens = menus.SelectMany(menu => menu.Items).OfType<IScreenMenuItem>().ToArray();
			var matching = screens.FirstOrDefault(screen => screen.ScreenType == block.DefaultScreen);
			if (block.DefaultScreen != null && matching == null)
				definitions.Add(new ApplicationScreenDefinition(block, block.DefaultScreen, ApplicationScreenDefinition.DefaultMenuItemId,
					block.DefaultScreenTitle, block.DefaultScreenActivationMode, block.DefaultScreenKind, true));
			foreach (var screen in screens) {
				var isBlockDefault = ReferenceEquals(screen, matching);
				if (isBlockDefault) {
					Guard.Argument(!block.DefaultScreenActivationMode.HasValue || !screen.ActivationMode.HasValue
						|| block.DefaultScreenActivationMode == screen.ActivationMode, nameof(blocks), "The block default and its menu entry declare conflicting activation modes.");
					Guard.Argument(block.DefaultScreenKind == ScreenKind.Normal || block.DefaultScreenKind == screen.ScreenKind,
						nameof(blocks), "The block default and its menu entry declare conflicting screen kinds.");
				}
				definitions.Add(new ApplicationScreenDefinition(block, screen.ScreenType, screen.Id,
					isBlockDefault ? block.DefaultScreenTitle ?? screen.ScreenTitle ?? screen.Title : screen.ScreenTitle ?? screen.Title,
					isBlockDefault ? block.DefaultScreenActivationMode ?? screen.ActivationMode : screen.ActivationMode,
					screen.ScreenKind, isBlockDefault || screen.IsDefault));
			}
		}

		// A component type has one lifetime and role throughout an application, even when referenced by multiple menus.
		var kinds = new Dictionary<Type, ScreenKind>();
		var policies = new ScreenActivationPolicyRegistry();
		foreach (var definition in definitions) {
			Guard.Argument(!kinds.TryGetValue(definition.ScreenType, out var kind) || kind == definition.ScreenKind,
				nameof(blocks), $"Screen {definition.ScreenType.Name} declares conflicting screen kinds.");
			kinds[definition.ScreenType] = definition.ScreenKind;
			if (definition.ActivationMode.HasValue)
				policies.RegisterInstance(definition.ScreenType, definition.ActivationMode.Value);
		}
		return definitions.ToArray();
	}

	/// <summary>Returns the first marked startup screen. Placeholders cannot be selected when a permanent normal screen is present.</summary>
	public static ApplicationScreenDefinition GetDefaultScreen(IEnumerable<ApplicationScreenDefinition> screens) {
		Guard.ArgumentNotNull(screens, nameof(screens));
		var definitions = screens.ToArray();
		Guard.Argument(definitions.All(screen => screen != null), nameof(screens), "Screen definitions cannot be null.");
		var hasPermanent = definitions.Any(screen => screen.ActivationMode == ScreenActivationMode.PermanentSingleton);
		return definitions.FirstOrDefault(screen => screen.IsDefault && (!hasPermanent || screen.ScreenKind != ScreenKind.Empty));
	}

	/// <summary>Returns the first empty-workspace candidate, preferring one explicitly marked as default.</summary>
	public static ApplicationScreenDefinition GetEmptyScreen(IEnumerable<ApplicationScreenDefinition> screens) {
		Guard.ArgumentNotNull(screens, nameof(screens));
		var definitions = screens.ToArray();
		Guard.Argument(definitions.All(screen => screen != null), nameof(screens), "Screen definitions cannot be null.");
		var candidates = definitions.Where(screen => screen.ScreenKind == ScreenKind.Empty).ToArray();
		return candidates.FirstOrDefault(screen => screen.IsDefault) ?? candidates.FirstOrDefault();
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
