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

/// <summary>Adds an optional native editor to application-root preference metadata without creating a screen.</summary>
public class WinFormsApplicationSettingsDefinition : ApplicationSettingsDefinition {
	/// <summary>Declares the existing root schema and an optional detached entity-editor factory.</summary>
	public WinFormsApplicationSettingsDefinition(string stableKey, string title, object settings,
		Func<WinFormsApplicationSettingsEditorContext, ICrudEntityEditor<object>> editorFactory = null, Func<object> settingsFactory = null)
		: base(stableKey, title, settings, settingsFactory) {
		EditorFactory = editorFactory;
	}

	/// <summary>Creates only the selected editor; null uses the standard PropertyGrid-based editor.</summary>
	public Func<WinFormsApplicationSettingsEditorContext, ICrudEntityEditor<object>> EditorFactory { get; }
}
