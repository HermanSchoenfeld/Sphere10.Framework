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
using SharedUI = global::Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Web.AspNetCore.Blazor;

/// <summary>Configures one component registration independently of its navigation commands.</summary>
public class ApplicationScreenBuilder : ApplicationScreenBuilderBase<ApplicationScreenBuilder> {
	private IReadOnlyDictionary<string, object> _parameters = new Dictionary<string, object>();
	private Func<ApplicationScreenActivationContext, ApplicationScreenActivation> _activator;
	private Type _settingsEditor;

	/// <summary>Starts configuration for a component registration.</summary>
	public ApplicationScreenBuilder() {
	}

	/// <summary>Starts configuration for a component registration.</summary>
	internal ApplicationScreenBuilder(Type screenType, ApplicationScreenRegistration registration)
		: base(screenType, registration) {
	}

	/// <summary>Declares typed preferences with a dedicated renderer-owned editor component.</summary>
	public ApplicationScreenBuilder UseSettings<TSettings, TEditor>()
		where TSettings : SettingsObject, new()
		where TEditor : IComponent, IApplicationSettingsEditor {
		UseSettings<TSettings>();
		return WithSettingsEditor<TEditor>();
	}

	/// <summary>Chooses the component used to edit this registration's ordinary settings object.</summary>
	public ApplicationScreenBuilder WithSettingsEditor<TEditor>()
		where TEditor : IComponent, IApplicationSettingsEditor {
		_settingsEditor = typeof(TEditor);
		return this;
	}

	/// <summary>Captures default component parameters; values should be immutable identifiers or values.</summary>
	public ApplicationScreenBuilder WithParameters(IReadOnlyDictionary<string, object> parameters) {
		Guard.ArgumentNotNull(parameters, nameof(parameters));
		_parameters = new Dictionary<string, object>(parameters, StringComparer.OrdinalIgnoreCase);
		return this;
	}

	/// <summary>Prepares parameters only when creating a new session; the renderer still constructs the component.</summary>
	public ApplicationScreenBuilder WithActivator(Func<ApplicationScreenActivationContext, ApplicationScreenActivation> activator) {
		Guard.ArgumentNotNull(activator, nameof(activator));
		_activator = activator;
		return this;
	}

	/// <inheritdoc />
	protected override SharedUI.ApplicationScreenDefinition CreateDefinition() {
		return new ApplicationScreenDefinition(ScreenType, Title, ActivationMode, ScreenKind, Registration, StableKey, _parameters, _activator, Settings, _settingsEditor);
	}

	/// <inheritdoc />
	protected override void ValidateScreenType(Type screenType) {
		Tools.UI.ValidateScreenType(screenType, typeof(IApplicationScreen));
	}
}

/// <summary>Configures a component registration with a compile-time screen type.</summary>
public sealed class ApplicationScreenBuilder<TScreen> : ApplicationScreenBuilder
	where TScreen : IApplicationScreen {
	/// <summary>Starts configuration for a component registration.</summary>
	public ApplicationScreenBuilder()
		: this(new ApplicationScreenRegistration<TScreen>()) {
	}

	/// <summary>Starts configuration for a component registration.</summary>
	internal ApplicationScreenBuilder(ApplicationScreenRegistration<TScreen> registration)
		: base(typeof(TScreen), registration) {
	}
}
