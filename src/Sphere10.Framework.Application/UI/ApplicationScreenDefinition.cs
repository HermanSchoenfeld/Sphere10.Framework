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

/// <summary>Screen registration metadata, actual settings and the navigation commands that activate it.</summary>
public class ApplicationScreenDefinition {
	private readonly List<IApplicationMenuItem> _activators = new();

	public ApplicationScreenDefinition(Type screenType, string title = null, ScreenActivationMode? activationMode = null, ScreenKind screenKind = ScreenKind.Normal,
		ApplicationScreenRegistration registration = null, string stableKey = null, SettingsObject settings = null) {
		ApplicationHelper.ValidateScreenType(screenType);
		ApplicationHelper.ValidateScreenPolicy(activationMode, screenKind);
		Guard.Argument(registration == null || registration.ScreenType == screenType && !registration.IsTypeReference, nameof(registration), "A definition requires an exact registration of its screen type.");
		Guard.Argument(stableKey == null || !string.IsNullOrWhiteSpace(stableKey), nameof(stableKey), "A stable registration key cannot be blank.");
		ScreenType = screenType;
		Title = title ?? screenType.Name;
		ActivationMode = activationMode;
		ScreenKind = screenKind;
		Registration = registration ?? new ApplicationScreenRegistration(screenType);
		StableKey = stableKey;
		var declaredSettingsType = ApplicationHelper.GetScreenSettingsType(screenType);
		if (settings == null && declaredSettingsType != null) {
			Guard.Argument(!declaredSettingsType.IsAbstract && declaredSettingsType.GetConstructor(Type.EmptyTypes) != null, nameof(settings),
				"The declared screen settings need a public parameterless constructor or explicit registration defaults.");
			settings = (SettingsObject)Tools.Object.Create(declaredSettingsType);
		}
		Guard.Argument(settings == null || declaredSettingsType == null || declaredSettingsType.IsInstanceOfType(settings), nameof(settings),
			"Registration settings must match the screen's declared settings type.");
		Settings = settings;
	}

	public Type ScreenType { get; }

	public string Title { get; private set; }

	public ScreenActivationMode? ActivationMode { get; private set; }

	public ScreenKind ScreenKind { get; }

	/// <summary>Identifies this registration across graph snapshots without sharing mutable ownership.</summary>
	public ApplicationScreenRegistration Registration { get; }

	/// <summary>An explicit persistent identity, required when the owning block has several registrations of this type.</summary>
	public string StableKey { get; }

	/// <summary>The actual settings instance shared by this registration and its screen instances.</summary>
	public SettingsObject Settings { get; set; }

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
			ApplicationHelper.ValidateScreenType(definition.ScreenType);
			ApplicationHelper.ValidateScreenPolicy(definition.ActivationMode, definition.ScreenKind);
			validateScreenType?.Invoke(definition.ScreenType);
		}
	}

	/// <summary>Copies registration metadata into an independent graph with no command membership.</summary>
	public virtual ApplicationScreenDefinition Clone()
		=> new ApplicationScreenDefinition(ScreenType, Title, ActivationMode, ScreenKind, Registration, StableKey, Settings);

	public virtual ApplicationScreenDefinition WithActivationMode(ScreenActivationMode activationMode) {
		ApplicationHelper.ValidateScreenPolicy(activationMode, ScreenKind);
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
