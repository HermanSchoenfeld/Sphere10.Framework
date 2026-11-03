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
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Web.AspNetCore.Blazor;

/// <summary>Owns a component registration's parameters and optional renderer activation preparation.</summary>
public sealed class BlazorApplicationScreenDefinition : ApplicationScreenDefinition {
	/// <summary>Declares component parameters, activation metadata and the actual settings type.</summary>
	public BlazorApplicationScreenDefinition(Type screenType, string title = null, ScreenActivationMode? activationMode = null,
		ScreenKind screenKind = ScreenKind.Normal, ApplicationScreenRegistration registration = null, string stableKey = null,
		IReadOnlyDictionary<string, object> parameters = null,
		Func<BlazorApplicationScreenActivationContext, BlazorApplicationScreenActivation> activator = null,
		SettingsObject settings = null, Type settingsEditor = null)
		: base(screenType, title, activationMode, screenKind, registration, stableKey, settings) {
		Tools.UI.ValidateScreenType(screenType, typeof(IBlazorApplicationScreen));
		Guard.Argument(settingsEditor == null || !settingsEditor.IsAbstract && !settingsEditor.ContainsGenericParameters
			&& typeof(IComponent).IsAssignableFrom(settingsEditor)
			&& typeof(IBlazorApplicationSettingsEditor).IsAssignableFrom(settingsEditor),
			nameof(settingsEditor), "A settings editor must be a concrete component implementing IBlazorApplicationSettingsEditor.");
		Parameters = new ReadOnlyDictionary<string, object>(new Dictionary<string, object>(parameters ?? new Dictionary<string, object>(), StringComparer.OrdinalIgnoreCase));
		Activator = activator;
		SettingsEditor = settingsEditor;
	}

	/// <inheritdoc />
	public IReadOnlyDictionary<string, object> Parameters { get; }

	/// <inheritdoc />
	public Func<BlazorApplicationScreenActivationContext, BlazorApplicationScreenActivation> Activator { get; }

	/// <summary>The optional renderer-owned component that edits this registration's actual settings object.</summary>
	public Type SettingsEditor { get; }

	/// <summary>Copies configuration without sharing the snapshot's reverse command membership.</summary>
	public override ApplicationScreenDefinition Clone() {
		return new BlazorApplicationScreenDefinition(ScreenType, Title, ActivationMode, ScreenKind, Registration, StableKey, Parameters, Activator, Settings, SettingsEditor);
	}
}
