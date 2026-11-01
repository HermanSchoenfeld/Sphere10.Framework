// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Web.AspNetCore.Blazor;

/// <summary>Configures one component registration independently of its navigation commands.</summary>
public class BlazorApplicationScreenBuilder : ApplicationScreenBuilderBase<BlazorApplicationScreenBuilder> {
	private IReadOnlyDictionary<string, object> _parameters = new Dictionary<string, object>();
	private Func<BlazorApplicationScreenActivationContext, BlazorApplicationScreenActivation> _activator;
	private Type _settingsEditor;

	public BlazorApplicationScreenBuilder() {
	}

	internal BlazorApplicationScreenBuilder(Type screenType, ApplicationScreenRegistration registration)
		: base(screenType, registration) {
	}

	/// <summary>Declares typed preferences with a dedicated renderer-owned editor component.</summary>
	public BlazorApplicationScreenBuilder UseSettings<TSettings, TEditor>()
		where TSettings : new()
		where TEditor : IComponent, IBlazorApplicationSettingsEditor {
		UseSettings<TSettings>();
		return WithSettingsEditor<TEditor>();
	}

	/// <summary>Uses the supplied object factory and a dedicated editor, including models without a default constructor.</summary>
	public BlazorApplicationScreenBuilder UseSettings<TSettings, TEditor>(Func<TSettings> defaults)
		where TEditor : IComponent, IBlazorApplicationSettingsEditor {
		UseSettings(defaults);
		return WithSettingsEditor<TEditor>();
	}

	/// <summary>Chooses the component used to edit this registration's ordinary settings object.</summary>
	public BlazorApplicationScreenBuilder WithSettingsEditor<TEditor>()
		where TEditor : IComponent, IBlazorApplicationSettingsEditor {
		_settingsEditor = typeof(TEditor);
		return this;
	}

	/// <summary>Captures default component parameters; values should be immutable identifiers or values.</summary>
	public BlazorApplicationScreenBuilder WithParameters(IReadOnlyDictionary<string, object> parameters) {
		Guard.ArgumentNotNull(parameters, nameof(parameters));
		_parameters = new Dictionary<string, object>(parameters, StringComparer.OrdinalIgnoreCase);
		return this;
	}

	/// <summary>Prepares parameters only when creating a new session; the renderer still constructs the component.</summary>
	public BlazorApplicationScreenBuilder WithActivator(Func<BlazorApplicationScreenActivationContext, BlazorApplicationScreenActivation> activator) {
		Guard.ArgumentNotNull(activator, nameof(activator));
		_activator = activator;
		return this;
	}

	protected override ApplicationScreenDefinition CreateDefinition() {
		return new BlazorApplicationScreenDefinition(ScreenType, Title, ActivationMode, ScreenKind, Registration, StableKey, _parameters, _activator, Settings, SettingsFactory, _settingsEditor);
	}

	protected override void ValidateScreenType(Type screenType) {
		Tools.UI.ValidateScreenType(screenType, typeof(IBlazorApplicationScreen));
	}
}

/// <summary>Configures a component registration with a compile-time screen type.</summary>
public sealed class BlazorApplicationScreenBuilder<TScreen> : BlazorApplicationScreenBuilder
	where TScreen : IBlazorApplicationScreen {
	public BlazorApplicationScreenBuilder()
		: this(new ApplicationScreenRegistration<TScreen>()) {
	}

	internal BlazorApplicationScreenBuilder(ApplicationScreenRegistration<TScreen> registration)
		: base(typeof(TScreen), registration) {
	}
}
