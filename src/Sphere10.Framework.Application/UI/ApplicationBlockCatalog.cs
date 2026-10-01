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

/// <summary>Shared snapshot ordering, stable-ID lookup and activation-policy validation.</summary>
public class ApplicationBlockCatalog<TBlock> : ApplicationBlockCatalogBase<TBlock> where TBlock : class, IApplicationBlock {
	private readonly Dictionary<string, TBlock> _blocks;
	private readonly TBlock[] _orderedBlocks;

	public ApplicationBlockCatalog(IEnumerable<TBlock> blocks, Func<TBlock, TBlock> snapshot,
		Func<TBlock, IScreenMenuItem> getDefaultScreen = null, Action<Type> validateScreenType = null
	) {
		Guard.ArgumentNotNull(blocks, nameof(blocks));
		Guard.ArgumentNotNull(snapshot, nameof(snapshot));
		var snapshots = blocks.Select(snapshot).OrderBy(block => block.Position).ToArray();
		Guard.Argument(snapshots.Select(block => block.Id).Distinct(StringComparer.Ordinal).Count() == snapshots.Length, nameof(blocks), "Block IDs must be unique.");

		var declarations = new List<KeyValuePair<Type, ScreenActivationMode>>();
		foreach (var block in snapshots) {
			if (block.DefaultScreen != null) {
				Tools.UI.ValidateScreenType(block.DefaultScreen);
				validateScreenType?.Invoke(block.DefaultScreen);
			}
			var screens = block.Menus.SelectMany(menu => menu.Items).OfType<IScreenMenuItem>().ToList();
			var defaultScreen = getDefaultScreen?.Invoke(block);
			if (defaultScreen != null)
				screens.Add(defaultScreen);
			foreach (var screen in screens) {
				Tools.UI.ValidateScreenType(screen.ScreenType);
				validateScreenType?.Invoke(screen.ScreenType);
				if (screen.ActivationMode.HasValue)
					declarations.Add(new KeyValuePair<Type, ScreenActivationMode>(screen.ScreenType, screen.ActivationMode.Value));
			}
		}
		var policies = new ScreenActivationPolicyRegistry();
		policies.RegisterDeclarations(declarations);
		_orderedBlocks = snapshots;
		_blocks = snapshots.ToDictionary(block => block.Id, StringComparer.Ordinal);
	}

	/// <summary>Returns a copy of the catalog membership in display order.</summary>
	public override TBlock[] Blocks => Tools.Array.Clone(_orderedBlocks);

	public override TBlock Get(string id) {
		Guard.ArgumentNotNullOrEmpty(id, nameof(id));
		Guard.Argument(_blocks.TryGetValue(id, out var block), nameof(id), $"Unknown block '{id}'.");
		return block;
	}
}
