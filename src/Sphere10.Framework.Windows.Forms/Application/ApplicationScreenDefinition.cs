// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;
using SharedUI = global::Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Native screen registration metadata with an optional UI-thread creation override.</summary>
public class ApplicationScreenDefinition : SharedUI.ApplicationScreenDefinition {
	/// <summary>Creates native registration metadata without invoking the optional activator.</summary>
	public ApplicationScreenDefinition(Type screenType, string title = null, ScreenActivationMode? activationMode = null,
		ScreenKind screenKind = ScreenKind.Normal, ApplicationScreenRegistration registration = null, string stableKey = null,
		Func<ApplicationScreenActivationContext, ApplicationScreen> activator = null, SettingsObject settings = null,
		Func<ApplicationSettingsEditorContext, ICrudEntityEditor<object>> settingsEditor = null)
		: base(screenType, title, activationMode, screenKind, registration, stableKey, settings) {
		ApplicationHelper.ValidateScreenType(screenType, typeof(ApplicationScreen));
		Activator = activator;
		SettingsEditor = settingsEditor;
	}

	/// <summary>The optional constructor override; null uses the host's existing compatible constructors.</summary>
	public Func<ApplicationScreenActivationContext, ApplicationScreen> Activator { get; }

	/// <summary>Creates an editor for the actual settings object; null uses the standard PropertyGrid editor.</summary>
	public Func<ApplicationSettingsEditorContext, ICrudEntityEditor<object>> SettingsEditor { get; }

	/// <summary>Preserves activation and identity while leaving command membership owned by the copied graph.</summary>
	public override SharedUI.ApplicationScreenDefinition Clone() {
		return new ApplicationScreenDefinition(ScreenType, Title, ActivationMode, ScreenKind, Registration, StableKey, Activator, Settings, SettingsEditor);
	}
}
