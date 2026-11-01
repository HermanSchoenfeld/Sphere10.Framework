// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Configures one native screen independently of its navigation commands.</summary>
public class WinFormsApplicationScreenBuilder : ApplicationScreenBuilderBase<WinFormsApplicationScreenBuilder> {
	private Func<WinFormsApplicationScreenActivationContext, WinFormsApplicationScreen> _activator;
	private Func<WinFormsApplicationSettingsEditorContext, ICrudEntityEditor<object>> _settingsEditor;

	/// <summary>Starts a registration whose type is supplied through WithScreen.</summary>
	public WinFormsApplicationScreenBuilder() {
	}

	// Preserves the typed registration identity supplied by a specialized builder.
	protected WinFormsApplicationScreenBuilder(Type screenType, ApplicationScreenRegistration registration)
		: base(screenType, registration) {
	}

	/// <summary>Overrides creation only; the host still owns policy, guards, retention and disposal.</summary>
	public WinFormsApplicationScreenBuilder WithActivator(Func<WinFormsApplicationScreenActivationContext, WinFormsApplicationScreen> activator) {
		Guard.ArgumentNotNull(activator, nameof(activator));
		_activator = activator;
		return this;
	}

	/// <summary>Supplies a dedicated detached editor while leaving validation and accepted persistence with Options.</summary>
	public WinFormsApplicationScreenBuilder WithSettingsEditor(Func<WinFormsApplicationSettingsEditorContext, ICrudEntityEditor<object>> editorFactory) {
		Guard.ArgumentNotNull(editorFactory, nameof(editorFactory));
		_settingsEditor = editorFactory;
		return this;
	}

	// Keeps incompatible screen types out of the native registration graph.
	protected override void ValidateScreenType(Type screenType) {
		Tools.UI.ValidateScreenType(screenType, typeof(WinFormsApplicationScreen));
	}

	// Builds metadata only; activation remains deferred until the host opens this registration.
	protected override ApplicationScreenDefinition CreateDefinition() {
		return new WinFormsApplicationScreenDefinition(ScreenType, Title, ActivationMode, ScreenKind, Registration, StableKey, _activator, Settings, _settingsEditor, SettingsFactory);
	}
}

/// <summary>Supplies the compile-time screen type and typed registration identity for a native screen.</summary>
public class WinFormsApplicationScreenBuilder<TScreen> : WinFormsApplicationScreenBuilder
	where TScreen : WinFormsApplicationScreen {
	/// <summary>Starts a registration for the compile-time screen type.</summary>
	public WinFormsApplicationScreenBuilder()
		: base(typeof(TScreen), new ApplicationScreenRegistration<TScreen>()) {
	}
}
