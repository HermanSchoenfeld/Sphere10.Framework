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

/// <summary>Shared snapshot traversal with canonical screen definitions isolated to each copied graph.</summary>
public abstract class ApplicationBlockSnapshotBase<TBlock, TMenu, TItem> : IApplicationBlockSnapshot<TBlock, TMenu, TItem>
	where TBlock : class, IApplicationBlock
	where TMenu : class, IApplicationMenu
	where TItem : class, IApplicationMenuItem {
	public TBlock Create(TBlock block) {
		Guard.ArgumentNotNull(block, nameof(block));
		Guard.ArgumentNotNullOrEmpty(block.Name, nameof(block), "Block name is required.");
		var definitions = Tools.UI.GetScreenDefinitions(new[] { block });
		var defaultScreen = block.DefaultScreenDefinition;
		foreach (var definition in definitions)
			ValidateScreenTypeCore(definition.ScreenType);
		var snapshots = new Dictionary<ApplicationScreenDefinition, ApplicationScreenDefinition>(ReferenceEqualityComparer.Instance);
		foreach (var definition in definitions)
			SnapshotDefinition(definition, snapshots);
		var result = CreateBlockSnapshot(block, block.Menus.Cast<TMenu>().Select(menu => CreateMenu(menu, snapshots)).ToArray(), snapshots);
		result.Source = block.Source;
		Tools.UI.GetScreenDefinitions(new[] { result });
		Guard.Argument(result.ScreenDefinitions.Length == definitions.Length
			&& definitions.All(definition => result.ScreenDefinitions.Any(candidate => ReferenceEquals(candidate, snapshots[definition]))),
			nameof(block), "A snapshot must use its own canonical screen definitions.");
		Guard.Argument(defaultScreen == null || ReferenceEquals(result.DefaultScreenDefinition, snapshots[defaultScreen]),
			nameof(block), "A snapshot must preserve the selected default within its own definition graph.");
		return result;
	}

	public TMenu CreateMenu(TMenu menu) => CreateMenu(menu, new Dictionary<ApplicationScreenDefinition, ApplicationScreenDefinition>(ReferenceEqualityComparer.Instance));

	public TItem CreateItem(TItem item) => CreateItem(item, new Dictionary<ApplicationScreenDefinition, ApplicationScreenDefinition>(ReferenceEqualityComparer.Instance));

	protected TItem CreateItem(TItem item, Dictionary<ApplicationScreenDefinition, ApplicationScreenDefinition> definitions) {
		Guard.ArgumentNotNull(item, nameof(item));
		Guard.ArgumentNotNullOrEmpty(item.Title, nameof(item), "Menu item title is required.");
		if (item is IScreenMenuItem screen) {
			Guard.ArgumentNotNull(screen.ScreenDefinition, nameof(item));
			ValidateScreenTypeCore(screen.ScreenDefinition.ScreenType);
		}
		var result = CreateItemSnapshot(item, definitions);
		Guard.ArgumentNotNull(result, nameof(item));
		if (item is IScreenMenuItem registration)
			Guard.Argument(result is IScreenMenuItem snapshot && ReferenceEquals(snapshot.ScreenDefinition, SnapshotDefinition(registration.ScreenDefinition, definitions)),
				nameof(item), "A screen command snapshot must reference its graph's canonical definition.");
		return result;
	}

	protected ApplicationScreenDefinition SnapshotDefinition(ApplicationScreenDefinition definition, Dictionary<ApplicationScreenDefinition, ApplicationScreenDefinition> definitions) {
		if (definition == null)
			return null;
		if (!definitions.TryGetValue(definition, out var snapshot)) {
			snapshot = new ApplicationScreenDefinition(definition.ScreenType, definition.Title, definition.ActivationMode, definition.ScreenKind);
			definitions.Add(definition, snapshot);
		}
		return snapshot;
	}

	private TMenu CreateMenu(TMenu menu, Dictionary<ApplicationScreenDefinition, ApplicationScreenDefinition> definitions) {
		Guard.ArgumentNotNull(menu, nameof(menu));
		Guard.ArgumentNotNullOrEmpty(menu.Text, nameof(menu), "Menu text is required.");
		Guard.ArgumentNotNull(menu.Items, nameof(menu));
		return CreateMenuSnapshot(menu, menu.Items.Cast<TItem>().Select(item => CreateItem(item, definitions)).ToArray());
	}

	protected virtual void ValidateScreenType(Type screenType) {
	}

	protected abstract TBlock CreateBlockSnapshot(TBlock block, TMenu[] menus, Dictionary<ApplicationScreenDefinition, ApplicationScreenDefinition> definitions);

	protected abstract TMenu CreateMenuSnapshot(TMenu menu, TItem[] items);

	protected abstract TItem CreateItemSnapshot(TItem item, Dictionary<ApplicationScreenDefinition, ApplicationScreenDefinition> definitions);

	private void ValidateScreenTypeCore(Type screenType) {
		Tools.UI.ValidateScreenType(screenType);
		ValidateScreenType(screenType);
	}
}
