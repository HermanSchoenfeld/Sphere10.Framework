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

/// <summary>Immutable screen metadata with reverse membership of the navigation commands that activate it.</summary>
public class ApplicationScreenDefinition {
	private readonly List<IApplicationMenuItem> _activators = new();

	public ApplicationScreenDefinition(Type screenType, string title = null, ScreenActivationMode? activationMode = null, ScreenKind screenKind = ScreenKind.Normal) {
		Tools.UI.ValidateScreenType(screenType);
		Tools.UI.ValidateScreenPolicy(activationMode, screenKind);
		ScreenType = screenType;
		Title = title ?? screenType.Name;
		ActivationMode = activationMode;
		ScreenKind = screenKind;
	}

	public Type ScreenType { get; }

	public string Title { get; }

	public ScreenActivationMode? ActivationMode { get; }

	public ScreenKind ScreenKind { get; }

	/// <summary>Returns a membership snapshot of commands attached to menus that reference this definition.</summary>
	public IApplicationMenuItem[] Activators => _activators.ToArray();

	public void AddActivator(IApplicationMenuItem item) {
		Guard.ArgumentNotNull(item, nameof(item));
		Guard.Argument(item is IScreenMenuItem screen && ReferenceEquals(screen.ScreenDefinition, this), nameof(item), "The command must activate this definition.");
		if (!_activators.Any(candidate => ReferenceEquals(candidate, item)))
			_activators.Add(item);
	}

	public void RemoveActivator(IApplicationMenuItem item) => _activators.RemoveAll(candidate => ReferenceEquals(candidate, item));

	/// <summary>Validates a complete set of definitions without changing their metadata or graph membership.</summary>
	public static void ValidateDefinitions(IEnumerable<ApplicationScreenDefinition> definitions, Action<Type> validateScreenType = null) {
		Guard.ArgumentNotNull(definitions, nameof(definitions));
		var modes = new Dictionary<Type, ScreenActivationMode>();
		var kinds = new Dictionary<Type, ScreenKind>();
		foreach (var definition in definitions) {
			Guard.ArgumentNotNull(definition, nameof(definitions));
			Tools.UI.ValidateScreenType(definition.ScreenType);
			Tools.UI.ValidateScreenPolicy(definition.ActivationMode, definition.ScreenKind);
			validateScreenType?.Invoke(definition.ScreenType);
			Guard.Argument(!kinds.TryGetValue(definition.ScreenType, out var kind) || kind == definition.ScreenKind,
				nameof(definitions), $"Screen {definition.ScreenType.Name} declares conflicting screen kinds.");
			kinds[definition.ScreenType] = definition.ScreenKind;
			if (!definition.ActivationMode.HasValue)
				continue;
			var mode = definition.ActivationMode.Value;
			Guard.Argument(!modes.TryGetValue(definition.ScreenType, out var existing) || existing == mode,
				nameof(definitions), $"Screen {definition.ScreenType.Name} declares conflicting activation modes.");
			modes[definition.ScreenType] = mode;
		}
	}

	public virtual ApplicationScreenDefinition WithActivationMode(ScreenActivationMode activationMode) =>
		new(ScreenType, Title, activationMode, ScreenKind);

	public virtual ApplicationScreenDefinition WithTitle(string title) =>
		new(ScreenType, title, ActivationMode, ScreenKind);
}
