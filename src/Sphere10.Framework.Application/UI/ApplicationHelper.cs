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
using System.Reflection;
using System.Text;
using System.Xml.Serialization;
using Newtonsoft.Json;
using Sphere10.Framework;
using Sphere10.Framework.Application;

namespace Sphere10.Framework.Application.UI;

/// <summary>Supports internal application composition, registration validation and settings inspection.</summary>
internal static class ApplicationHelper {

	/// <summary>Validates unique plugin names and block ownership, preserving definition identity in a new membership array.</summary>
	internal static IApplicationPlugin[] ValidatePlugins(IEnumerable<IApplicationPlugin> plugins) {
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
	internal static ApplicationPluginRegistration<TPlugin>[] ValidatePluginRegistrations<TPlugin>(IEnumerable<ApplicationPluginRegistration<TPlugin>> registrations)
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
	internal static IApplicationBlock[] ValidatePluginBlocks(IEnumerable<IApplicationBlock> blocks) {
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
	internal static TItem[] MergeMenuItems<TItem>(params TItem[][] layers)
		where TItem : class, IApplicationMenuItem {
		return MergeMenuItems(item => {
			Guard.ArgumentNotNullOrEmpty(item.Title, nameof(layers), "A command caption is required for merging.");
			return item.Title;
		}, layers);
	}

	/// <summary>Lets adapters supply their own command identity without adding platform identifiers to shared metadata.</summary>
	internal static TItem[] MergeMenuItems<TItem>(Func<TItem, object> keySelector, params TItem[][] layers)
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
	internal static TMenu[] MergeMenus<TMenu, TItem>(Func<TMenu, TItem[], TMenu> createMenu, params TMenu[][] layers)
		where TMenu : class, IApplicationMenu
		where TItem : class, IApplicationMenuItem {
		return MergeMenus(createMenu, menu => {
			Guard.ArgumentNotNullOrEmpty(menu.Text, nameof(layers), "A menu caption is required for merging.");
			return menu.Text;
		}, item => {
			Guard.ArgumentNotNullOrEmpty(item.Title, nameof(layers), "A command caption is required for merging.");
			return item.Title;
		}, layers);
	}

	/// <summary>Merges menus using adapter-supplied semantic keys.</summary>
	internal static TMenu[] MergeMenus<TMenu, TItem>(Func<TMenu, TItem[], TMenu> createMenu,
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


	/// <summary>Composes supplied owners in application, source plugin, block and screen order.</summary>
	internal static ApplicationCommandLayer[] GetCommandLayers(IApplicationCommandProvider application, IApplicationCommandProvider block = null, IApplicationCommandProvider screen = null) {
		Guard.ArgumentNotNull(application, nameof(application));
		var result = new List<ApplicationCommandLayer> { new(ApplicationCommandLevel.Application, application) };
		if (block is IApplicationBlock { Source: { } plugin })
			result.Add(new ApplicationCommandLayer(ApplicationCommandLevel.Plugin, plugin));
		if (block != null)
			result.Add(new ApplicationCommandLayer(ApplicationCommandLevel.Block, block));
		if (screen != null)
			result.Add(new ApplicationCommandLayer(ApplicationCommandLevel.Screen, screen));
		return result.ToArray();
	}

	/// <summary>Composes menus while protecting application commands from contextual replacements.</summary>
	internal static TMenu[] MergeApplicationMenus<TMenu, TItem>(Func<TMenu, TItem[], TMenu> createMenu, params ApplicationCommandLayer[] layers)
		where TMenu : class, IApplicationMenu
		where TItem : class, IApplicationMenuItem {
		return MergeApplicationMenus(createMenu, menu => {
			Guard.ArgumentNotNullOrEmpty(menu.Text, nameof(layers), "A menu caption is required for merging.");
			return menu.Text;
		}, item => {
			Guard.ArgumentNotNullOrEmpty(item.Title, nameof(layers), "A command caption is required for merging.");
			return item.Title;
		}, layers);
	}

	/// <summary>Adapters supply their own semantic keys; application and contextual commands have separate identity scopes.</summary>
	internal static TMenu[] MergeApplicationMenus<TMenu, TItem>(Func<TMenu, TItem[], TMenu> createMenu,
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
	internal static TItem[] MergeApplicationToolBarItems<TItem>(params ApplicationCommandLayer[] layers)
		where TItem : class, IApplicationMenuItem {
		return MergeApplicationToolBarItems<TItem>(item => {
			Guard.ArgumentNotNullOrEmpty(item.Title, nameof(layers), "A command caption is required for merging.");
			return item.Title;
		}, layers);
	}

	/// <summary>Merges toolbar commands using adapter-supplied semantic keys within their owner scopes.</summary>
	internal static TItem[] MergeApplicationToolBarItems<TItem>(Func<TItem, object> itemKeySelector, params ApplicationCommandLayer[] layers)
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

	// Validate supplied command owners and preserve their broader-to-narrower precedence.
	private static ApplicationCommandLayer[] OrderCommandLayers(ApplicationCommandLayer[] layers) {
		Guard.ArgumentNotNull(layers, nameof(layers));
		Guard.Argument(layers.All(layer => layer != null), nameof(layers), "Command layers cannot be null.");
		foreach (var layer in layers) {
			Guard.ArgumentNotNull(layer.Provider.Menus, nameof(layers));
			Guard.ArgumentNotNull(layer.Provider.ToolBarItems, nameof(layers));
			Guard.Argument(layer.Provider.Menus.All(menu => menu != null && menu.Items != null), nameof(layers), "Menus and their item collections cannot be null.");
		}
		// Keep the existing enum values stable while placing the plugin before its block.
		return layers.OrderBy(layer => layer.Level switch {
			ApplicationCommandLevel.Application => 0,
			ApplicationCommandLevel.Plugin => 1,
			ApplicationCommandLevel.Block => 2,
			_ => 3
		}).ToArray();
	}

	/// <summary>Orders live definitions by plugin membership, then appends directly registered blocks in their supplied order.</summary>
	internal static IApplicationBlock[] OrderApplicationBlocks(IEnumerable<IApplicationPlugin> plugins, IEnumerable<IApplicationBlock> blocks) {
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
	internal static IApplicationBlock[] OrderApplicationBlocks<TPlugin>(IEnumerable<ApplicationPluginRegistration<TPlugin>> registrations, IEnumerable<IApplicationBlock> blocks)
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

	/// <summary>Preserves an explicit record identity or derives this screen registration's existing persistent ID.</summary>
	internal static object GetScreenSettingsId(string applicationKey, IApplicationBlock block, ApplicationScreenDefinition definition, string blockKey = null) {
		Guard.ArgumentNotNull(definition, nameof(definition));
		if (definition.Settings?.ID != null)
			return definition.Settings.ID;

		var key = GetScreenSettingsKey(applicationKey, block, definition, blockKey);
		return Hashers.Hash(CHF.SHA2_256, Encoding.UTF8.GetBytes(key)).ToHexString(true);
	}

	/// <summary>Returns the existing escaped application, plugin, block and screen registration key.</summary>
	internal static string GetScreenSettingsKey(string applicationKey, IApplicationBlock block, ApplicationScreenDefinition definition, string blockKey = null) {
		Guard.ArgumentNotNullOrEmpty(applicationKey, nameof(applicationKey));
		Guard.ArgumentNotNull(block, nameof(block));
		Guard.ArgumentNotNull(definition, nameof(definition));

		// Escape each owner segment so registrations retain their original persistence addresses.
		var segments = new[] { applicationKey, "screen", block.Source?.Name ?? "standalone", blockKey ?? block.Name, GetScreenRegistrationKey(block, definition) };
		Guard.Argument(segments.All(segment => !string.IsNullOrWhiteSpace(segment)), nameof(block), "Persistent settings identities cannot contain an empty segment.");
		return string.Join("/", segments.Select(Uri.EscapeDataString));
	}

	/// <summary>Reads a screen's declared preference type without constructing it or invoking a property getter.</summary>
	internal static Type GetScreenSettingsType(Type screenType) {
		Guard.ArgumentNotNull(screenType, nameof(screenType));
		Type propertySettingsType = null;
		for (var type = screenType; type != null; type = type.BaseType) {
			var property = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
				.FirstOrDefault(candidate => candidate.Name == nameof(IApplicationScreen.ScreenSettings) && candidate.GetIndexParameters().Length == 0);
			if (property != null) {
				propertySettingsType = property.PropertyType == typeof(SettingsObject) ? null : property.PropertyType;
				break;
			}
		}

		// Settings and the existing UseSettings spelling share one declaration contract.
		var attributeSettingsTypes = screenType.GetCustomAttributes<UseSettingsAttribute>(true).Select(attribute => attribute.SettingsTypeToUse).Distinct().ToArray();
		Guard.Argument(attributeSettingsTypes.All(type => type != null && typeof(SettingsObject).IsAssignableFrom(type) && !type.ContainsGenericParameters),
			nameof(screenType), "Settings attributes must identify a closed SettingsObject type.");
		Guard.Argument(attributeSettingsTypes.Length <= 1, nameof(screenType), "Screen settings attributes must identify the same settings type.");
		var attributeSettingsType = attributeSettingsTypes.FirstOrDefault();
		Guard.Argument(propertySettingsType == null || typeof(SettingsObject).IsAssignableFrom(propertySettingsType), nameof(screenType),
			"Screen settings must derive from SettingsObject.");
		Guard.Argument(propertySettingsType == null || attributeSettingsType == null || propertySettingsType == attributeSettingsType, nameof(screenType),
			"The settings attribute and declared ScreenSettings property must identify the same settings type.");
		return propertySettingsType ?? attributeSettingsType;
	}

	/// <summary>Identifies persistent public value members independently of editor visibility or read-only metadata.</summary>
	internal static bool IsSettingsMember(MemberInfo member) {
		Guard.ArgumentNotNull(member, nameof(member));
		// PropertyInfo's instance API ignores inheritance; the attribute API follows overridden properties.
		var attributes = Attribute.GetCustomAttributes(member, true);
		return member switch {
			PropertyInfo property => IsSettingsMember(property.PropertyType, attributes),
			FieldInfo field => IsSettingsMember(field.FieldType, attributes),
			_ => IsSettingsMember(attributes)
		};
	}

	/// <summary>Excludes runtime ownership and ignored values using component-model property metadata.</summary>
	internal static bool IsSettingsMember(Type memberType, IEnumerable<Attribute> attributes) {
		Guard.ArgumentNotNull(memberType, nameof(memberType));
		Guard.ArgumentNotNull(attributes, nameof(attributes));
		return !typeof(Delegate).IsAssignableFrom(memberType) && !typeof(IServiceProvider).IsAssignableFrom(memberType)
			&& !typeof(ISettingsProvider).IsAssignableFrom(memberType) && !typeof(IApplication).IsAssignableFrom(memberType)
			&& !typeof(IApplicationScreen).IsAssignableFrom(memberType) && !typeof(IApplicationScreenHost).IsAssignableFrom(memberType)
			&& IsSettingsMember(attributes);
	}

	/// <summary>Supports component-model descriptors using the same transient and serialization exclusions.</summary>
	internal static bool IsSettingsMember(IEnumerable<Attribute> attributes) {
		Guard.ArgumentNotNull(attributes, nameof(attributes));
		return !attributes.Any(attribute => attribute is TransientAttribute or JsonIgnoreAttribute or XmlIgnoreAttribute or NonSerializedAttribute);
	}

	/// <summary>Returns block-owned screen definitions in declaration order, preserving reference identity.</summary>
	internal static ApplicationScreenDefinition[] GetScreenDefinitions(IEnumerable<IApplicationBlock> blocks) {
		Guard.ArgumentNotNull(blocks, nameof(blocks));
		var definitions = new List<ApplicationScreenDefinition>();
		foreach (var block in blocks) {
			Guard.ArgumentNotNull(block, nameof(blocks));
			var owned = block.ScreenDefinitions;
			Guard.ArgumentNotNull(owned, nameof(blocks));
			Guard.Argument(owned.All(definition => definition != null), nameof(blocks), "Screen definitions cannot be null.");
			Guard.Argument(owned.Distinct(ReferenceEqualityComparer.Instance).Count() == owned.Length, nameof(blocks), "Screen definitions must be registered once by reference.");
			ValidateScreenRegistrations(owned);
			Guard.Argument(block.DefaultScreenDefinition == null || owned.Any(definition => ReferenceEquals(definition, block.DefaultScreenDefinition)),
				nameof(blocks), "The default screen definition must be owned by the block.");
			definitions.AddRange(owned);
		}
		ApplicationScreenDefinition.ValidateDefinitions(definitions);
		return definitions.ToArray();
	}

	/// <summary>Binds navigation commands to existing registrations without changing block membership or activating screens.</summary>
	internal static void ResolveScreenReferences(IApplicationBlock block, IApplicationCommandProvider commands) {
		Guard.ArgumentNotNull(commands, nameof(commands));
		ResolveScreenReferences(block, commands.Menus, commands.ToolBarItems);
	}

	/// <summary>Binds configured command collections without activating their owner's cached presentation properties.</summary>
	internal static void ResolveScreenReferences(IApplicationBlock block, IEnumerable<IApplicationMenu> menus, IEnumerable<IApplicationMenuItem> toolBarItems) {
		Guard.ArgumentNotNull(block, nameof(block));
		Guard.ArgumentNotNull(menus, nameof(menus));
		Guard.ArgumentNotNull(toolBarItems, nameof(toolBarItems));
		ValidateScreenRegistrations(block.ScreenDefinitions);
		Guard.Argument(toolBarItems.All(item => item != null), nameof(toolBarItems), "Toolbar items cannot be null.");
		foreach (var menu in menus) {
			Guard.ArgumentNotNull(menu, nameof(menus));
			Guard.ArgumentNotNull(menu.Items, nameof(menus));
			Guard.Argument(menu.Items.All(item => item != null), nameof(menus), "Menu items cannot be null.");
		}
		foreach (var item in menus.SelectMany(menu => menu.Items).Concat(toolBarItems).OfType<IScreenMenuItem>()) {
			var registration = item.Registration ?? item.ScreenDefinition?.Registration;
			Guard.Argument(registration != null, nameof(block), "A show-screen command must identify a registered screen.");
			// Navigation type shortcuts deliberately select the first registration in declaration order.
			var definition = registration.IsTypeReference ? block.ScreenDefinitions.FirstOrDefault(candidate => candidate.ScreenType == registration.ScreenType)
				: ResolveScreenRegistration(block.ScreenDefinitions, registration);
			Guard.Argument(definition != null, nameof(block), $"Screen '{registration.ScreenType.Name}' is not registered in the owning block. Add it with AddScreen first.");
			item.ResolveScreenDefinition(definition);
		}
	}

	/// <summary>Resolves an exact moniker or an unambiguous type within the owning block's registration scope.</summary>
	internal static ApplicationScreenDefinition ResolveScreenRegistration(IEnumerable<ApplicationScreenDefinition> definitions, ApplicationScreenRegistration registration) {
		Guard.ArgumentNotNull(definitions, nameof(definitions));
		Guard.ArgumentNotNull(registration, nameof(registration));
		var matching = definitions.Where(definition => registration.IsTypeReference ? definition.ScreenType == registration.ScreenType
			: definition.Registration.Token == registration.Token && definition.ScreenType == registration.ScreenType).ToArray();
		Guard.Argument(matching.Length > 0, nameof(registration), $"Screen '{registration.ScreenType.Name}' is not registered in the owning block. Add it with AddScreen first.");
		Guard.Argument(matching.Length == 1, nameof(registration), $"Screen '{registration.ScreenType.Name}' has several registrations. Supply the exact registration reference.");
		return matching[0];
	}

	/// <summary>Returns a caption-independent persistent key, requiring an explicit key for duplicate screen types.</summary>
	internal static string GetScreenRegistrationKey(IApplicationBlock block, ApplicationScreenDefinition definition) {
		Guard.ArgumentNotNull(block, nameof(block));
		Guard.Argument(definition != null && block.ScreenDefinitions.Any(candidate => ReferenceEquals(candidate, definition)), nameof(definition), "The screen registration must belong to this block.");
		if (definition.StableKey != null)
			return definition.StableKey;
		Guard.Argument(block.ScreenDefinitions.Count(candidate => candidate.ScreenType == definition.ScreenType) == 1, nameof(definition),
			$"Screen '{definition.ScreenType.Name}' has several registrations. Configure WithStableKey for a registration that needs persistent identity.");
		return definition.ScreenType.Assembly.GetName().Name + ":" + definition.ScreenType.FullName;
	}

	/// <summary>Validates one block's independent registration identities and explicitly supplied persistent keys.</summary>
	internal static void ValidateScreenRegistrations(IEnumerable<ApplicationScreenDefinition> definitions) {
		Guard.ArgumentNotNull(definitions, nameof(definitions));
		var owned = definitions.ToArray();
		ApplicationScreenDefinition.ValidateDefinitions(owned);
		Guard.Argument(owned.Select(definition => definition.Registration.Token).Distinct().Count() == owned.Length,
			nameof(definitions), "A screen registration can be added only once to a block.");
		var persistentKeys = owned.Select(definition => definition.StableKey ?? (owned.Count(candidate => candidate.ScreenType == definition.ScreenType) == 1
			? definition.ScreenType.Assembly.GetName().Name + ":" + definition.ScreenType.FullName : null)).Where(key => key != null).ToArray();
		Guard.Argument(persistentKeys.Distinct(StringComparer.Ordinal).Count() == persistentKeys.Length, nameof(definitions),
			"Screen registration stable keys must be unique within their owning block, including generated type keys.");
	}

	/// <summary>Returns the first block's marked startup screen. Permanent screens exclude empty placeholders.</summary>
	internal static ApplicationScreenDefinition GetDefaultScreen(IEnumerable<IApplicationBlock> blocks) {
		Guard.ArgumentNotNull(blocks, nameof(blocks));
		var members = blocks.ToArray();
		var definitions = GetScreenDefinitions(members);
		var hasPermanent = definitions.Any(screen => screen.ActivationMode == ScreenActivationMode.PermanentSingleton);
		return members.Select(block => block.DefaultScreenDefinition).FirstOrDefault(screen => screen != null && (!hasPermanent || screen.ScreenKind != ScreenKind.Empty));
	}

	/// <summary>Returns a block's explicitly selected placeholder before the first remaining placeholder.</summary>
	internal static ApplicationScreenDefinition GetEmptyScreen(IEnumerable<IApplicationBlock> blocks) {
		Guard.ArgumentNotNull(blocks, nameof(blocks));
		var members = blocks.ToArray();
		var definitions = GetScreenDefinitions(members);
		return members.Select(block => block.DefaultScreenDefinition).FirstOrDefault(screen => screen?.ScreenKind == ScreenKind.Empty)
			?? definitions.FirstOrDefault(screen => screen.ScreenKind == ScreenKind.Empty);
	}

	/// <summary>Identifies activation policies that retain a single screen per registration.</summary>
	internal static bool IsSingleton(ScreenActivationMode activationMode) {
		ValidateActivationMode(activationMode);
		return activationMode is ScreenActivationMode.SingleInstance or ScreenActivationMode.PermanentSingleton;
	}

	/// <summary>Rejects unknown screen hosting roles.</summary>
	internal static void ValidateScreenKind(ScreenKind screenKind) {
		Guard.Argument(screenKind is ScreenKind.Normal or ScreenKind.Empty, nameof(screenKind), "Unknown screen kind.");
	}

	/// <summary>Checks activation and hosting roles without changing the registration.</summary>
	internal static void ValidateScreenPolicy(ScreenActivationMode? activationMode, ScreenKind screenKind) {
		ValidateScreenKind(screenKind);
		if (activationMode.HasValue)
			ValidateActivationMode(activationMode.Value);
		Guard.Argument(screenKind != ScreenKind.Empty || activationMode != ScreenActivationMode.PermanentSingleton,
			nameof(activationMode), "An empty-workspace screen cannot also be a permanently open screen.");
	}

	/// <summary>Requires a concrete shared screen type and any supplied platform screen contract.</summary>
	internal static void ValidateScreenType(Type screenType, Type screenContract = null) {
		Guard.ArgumentNotNull(screenType, nameof(screenType));
		Guard.Argument(typeof(IApplicationScreen).IsAssignableFrom(screenType) && !screenType.IsAbstract && !screenType.ContainsGenericParameters,
			nameof(screenType), "A concrete application screen type is required.");
		if (screenContract != null)
			Guard.Argument(screenContract.IsAssignableFrom(screenType), nameof(screenType), $"The screen must implement or derive from {screenContract.Name}.");
	}

	/// <summary>Rejects unknown screen activation policies.</summary>
	internal static void ValidateActivationMode(ScreenActivationMode activationMode) {
		Guard.Argument(activationMode is ScreenActivationMode.SingleInstance or ScreenActivationMode.MultiInstance or ScreenActivationMode.PermanentSingleton, nameof(activationMode), "Unknown activation mode.");
	}
}
