// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using Sphere10.Framework.Application.UI;
using SharedUI = global::Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Configures one native screen independently of its navigation commands.</summary>
public class ApplicationScreenBuilder : ApplicationScreenBuilderBase<ApplicationScreenBuilder> {
	private Func<ApplicationScreenActivationContext, ApplicationScreen> _activator;
	private Func<ApplicationSettingsEditorContext, ICrudEntityEditor<object>> _settingsEditor;

	/// <summary>Starts a registration whose type is supplied through WithScreen.</summary>
	public ApplicationScreenBuilder() {
	}

	// Preserves the typed registration identity supplied by a specialized builder.
	protected ApplicationScreenBuilder(Type screenType, ApplicationScreenRegistration registration)
		: base(screenType, registration) {
	}

	/// <summary>Overrides creation only; the host still owns policy, guards, retention and disposal.</summary>
	public ApplicationScreenBuilder WithActivator(Func<ApplicationScreenActivationContext, ApplicationScreen> activator) {
		Guard.ArgumentNotNull(activator, nameof(activator));
		_activator = activator;
		return this;
	}

	/// <summary>Supplies an editor for the actual settings object while Options owns validation and persistence.</summary>
	public ApplicationScreenBuilder WithSettingsEditor(Func<ApplicationSettingsEditorContext, ICrudEntityEditor<object>> editorFactory) {
		Guard.ArgumentNotNull(editorFactory, nameof(editorFactory));
		_settingsEditor = editorFactory;
		return this;
	}

	// Keeps incompatible screen types out of the native registration graph.
	protected override void ValidateScreenType(Type screenType) => ApplicationHelper.ValidateScreenType(screenType, typeof(ApplicationScreen));

	// Builds metadata only; activation remains deferred until the host opens this registration.
	protected override SharedUI.ApplicationScreenDefinition CreateDefinition()
		=> new ApplicationScreenDefinition(ScreenType, Title, ActivationMode, ScreenKind, Registration, StableKey, _activator, Settings, _settingsEditor);
}

/// <summary>Supplies the compile-time screen type and typed registration identity for a native screen.</summary>
public class ApplicationScreenBuilder<TScreen> : ApplicationScreenBuilder
	where TScreen : ApplicationScreen {
	/// <summary>Starts a registration for the compile-time screen type.</summary>
	public ApplicationScreenBuilder()
		: base(typeof(TScreen), new ApplicationScreenRegistration<TScreen>()) {
	}
}
