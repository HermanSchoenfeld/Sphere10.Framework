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

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Logic;

/// <summary>Immutable structural snapshots of the registered application blocks.</summary>
public class ApplicationBlockCatalog : ApplicationBlockCatalogBase {
	private readonly Dictionary<string, IApplicationBlock> _blocks;

	public ApplicationBlockCatalog(IEnumerable<IApplicationBlock> blocks) {
		Guard.ArgumentNotNull(blocks, nameof(blocks));
		var snapshots = blocks.Select(ApplicationBlockSnapshot.Create).OrderBy(block => block.Position).Cast<IApplicationBlock>().ToArray();
		Guard.Argument(snapshots.Select(block => block.Id).Distinct(StringComparer.Ordinal).Count() == snapshots.Length, nameof(blocks), "Block IDs must be unique.");
		var policies = new Dictionary<Type, ScreenActivationMode>();
		foreach (var block in snapshots) {
			var screens = block.Menus.SelectMany(menu => menu.Items).OfType<ShowScreenMenuItem>().ToList();
			var defaultScreen = ApplicationBlockSnapshot.GetDefaultScreen(block);
			if (defaultScreen != null)
				screens.Add(defaultScreen);
			foreach (var screen in screens) {
				Guard.Argument(!policies.TryGetValue(screen.ScreenType, out var registeredMode) || registeredMode == screen.ActivationMode,
					nameof(blocks), $"Conflicting activation modes for {screen.ScreenType.Name}.");
				policies[screen.ScreenType] = screen.ActivationMode;
			}
		}
		Blocks = Array.AsReadOnly(snapshots);
		_blocks = snapshots.ToDictionary(block => block.Id, StringComparer.Ordinal);
	}

	public override IReadOnlyList<IApplicationBlock> Blocks { get; }

	public override IApplicationBlock Get(string id) {
		Guard.ArgumentNotNullOrEmpty(id, nameof(id));
		Guard.Argument(_blocks.TryGetValue(id, out var block), nameof(id), $"Unknown block '{id}'.");
		return block;
	}
}

