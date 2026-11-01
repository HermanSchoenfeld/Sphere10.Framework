// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>The ordinary detached settings value and borrowed native services available to a custom editor.</summary>
public sealed class WinFormsApplicationSettingsEditorContext {
	// Editors borrow this context; the Options dialog retains session, control and persistence ownership.
	internal WinFormsApplicationSettingsEditorContext(object settings, IWinFormsApplication application,
		IServiceProvider services, IWin32Window owner) {
		Settings = settings;
		Application = application;
		Services = services;
		Owner = owner;
	}

	/// <summary>The ordinary detached object edited by this page.</summary>
	public object Settings { get; }

	/// <summary>The owning block application, or null for a plain main-form application.</summary>
	public IWinFormsApplication Application { get; }

	/// <summary>The actual application provider, when the host was configured through DI.</summary>
	public IServiceProvider Services { get; }

	/// <summary>The Options dialog owning any additional native editor windows.</summary>
	public IWin32Window Owner { get; }
}
