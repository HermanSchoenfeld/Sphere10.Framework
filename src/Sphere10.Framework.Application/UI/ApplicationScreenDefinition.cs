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

	public ApplicationScreenDefinition(Type screenType, string title = null, ScreenActivationMode? activationMode = null, ScreenKind screenKind = ScreenKind.Normal,
		ApplicationScreenRegistration registration = null, string stableKey = null, object settings = null, Func<object> settingsFactory = null) {
		Tools.UI.ValidateScreenType(screenType);
		Tools.UI.ValidateScreenPolicy(activationMode, screenKind);
		Guard.Argument(registration == null || registration.ScreenType == screenType && !registration.IsTypeReference, nameof(registration), "A definition requires an exact registration of its screen type.");
		Guard.Argument(stableKey == null || !string.IsNullOrWhiteSpace(stableKey), nameof(stableKey), "A stable registration key cannot be blank.");
		ScreenType = screenType;
		Title = title ?? screenType.Name;
		ActivationMode = activationMode;
		ScreenKind = screenKind;
		Registration = registration ?? new ApplicationScreenRegistration(screenType);
		StableKey = stableKey;
		Settings = settings == null ? null : ApplicationSettingsValueCloner.Copy(settings);
		var defaults = settings == null || settingsFactory != null ? null : ApplicationSettingsValueCloner.Copy(settings);
		SettingsFactory = settingsFactory ?? (defaults == null ? null : () => ApplicationSettingsValueCloner.Copy(defaults));
	}

	public Type ScreenType { get; }

	public string Title { get; private set; }

	public ScreenActivationMode? ActivationMode { get; private set; }

	public ScreenKind ScreenKind { get; }

	/// <summary>Identifies this registration across graph snapshots without sharing mutable ownership.</summary>
	public ApplicationScreenRegistration Registration { get; }

	/// <summary>An explicit persistent identity, required when the owning block has several registrations of this type.</summary>
	public string StableKey { get; }

	/// <summary>Optional declared preferences, discoverable without constructing this registration's screen.</summary>
	public object Settings { get; }

	/// <summary>Creates fresh default values for Reset; an omitted factory copies the supplied object's initial values.</summary>
	public Func<object> SettingsFactory { get; }

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
		foreach (var definition in definitions) {
			Guard.ArgumentNotNull(definition, nameof(definitions));
			Tools.UI.ValidateScreenType(definition.ScreenType);
			Tools.UI.ValidateScreenPolicy(definition.ActivationMode, definition.ScreenKind);
			validateScreenType?.Invoke(definition.ScreenType);
		}
	}

	/// <summary>Copies registration metadata into an independent graph with no command membership.</summary>
	public virtual ApplicationScreenDefinition Clone() {
		return new ApplicationScreenDefinition(ScreenType, Title, ActivationMode, ScreenKind, Registration, StableKey, Settings, SettingsFactory);
	}

	public virtual ApplicationScreenDefinition WithActivationMode(ScreenActivationMode activationMode) {
		Tools.UI.ValidateScreenPolicy(activationMode, ScreenKind);
		var definition = Clone();
		definition.ActivationMode = activationMode;
		return definition;
	}

	public virtual ApplicationScreenDefinition WithTitle(string title) {
		Guard.ArgumentNotNullOrEmpty(title, nameof(title));
		var definition = Clone();
		definition.Title = title;
		return definition;
	}
}
