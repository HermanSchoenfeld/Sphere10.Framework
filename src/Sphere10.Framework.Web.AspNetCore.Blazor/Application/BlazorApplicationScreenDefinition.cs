// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.AspNetCore.Components;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Web.AspNetCore.Blazor;

/// <summary>Owns a component registration's parameters and optional renderer activation preparation.</summary>
public sealed class BlazorApplicationScreenDefinition : ApplicationScreenDefinition {
	public BlazorApplicationScreenDefinition(Type screenType, string title = null, ScreenActivationMode? activationMode = null,
		ScreenKind screenKind = ScreenKind.Normal, ApplicationScreenRegistration registration = null, string stableKey = null,
		IReadOnlyDictionary<string, object> parameters = null,
		Func<BlazorApplicationScreenActivationContext, BlazorApplicationScreenActivation> activator = null,
		object settings = null, Func<object> settingsFactory = null, Type settingsEditor = null)
		: base(screenType, title, activationMode, screenKind, registration, stableKey, settings, settingsFactory) {
		Tools.UI.ValidateScreenType(screenType, typeof(IBlazorApplicationScreen));
		Guard.Argument(settingsEditor == null || !settingsEditor.IsAbstract && !settingsEditor.ContainsGenericParameters
			&& typeof(IComponent).IsAssignableFrom(settingsEditor)
			&& typeof(IBlazorApplicationSettingsEditor).IsAssignableFrom(settingsEditor),
			nameof(settingsEditor), "A settings editor must be a concrete component implementing IBlazorApplicationSettingsEditor.");
		Parameters = new ReadOnlyDictionary<string, object>(new Dictionary<string, object>(parameters ?? new Dictionary<string, object>(), StringComparer.OrdinalIgnoreCase));
		Activator = activator;
		SettingsEditor = settingsEditor;
	}

	public IReadOnlyDictionary<string, object> Parameters { get; }

	public Func<BlazorApplicationScreenActivationContext, BlazorApplicationScreenActivation> Activator { get; }

	/// <summary>The optional renderer-owned component that edits this registration's detached settings value.</summary>
	public Type SettingsEditor { get; }

	/// <summary>Copies configuration without sharing the snapshot's reverse command membership.</summary>
	public override ApplicationScreenDefinition Clone() {
		return new BlazorApplicationScreenDefinition(ScreenType, Title, ActivationMode, ScreenKind, Registration, StableKey, Parameters, Activator, Settings, SettingsFactory, SettingsEditor);
	}
}
