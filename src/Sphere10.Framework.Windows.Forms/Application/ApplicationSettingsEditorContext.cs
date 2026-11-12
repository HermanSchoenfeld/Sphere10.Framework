// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Windows.Forms;
using Sphere10.Framework.Application;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>The actual settings object and borrowed native services available to a custom editor.</summary>
public sealed class ApplicationSettingsEditorContext {
	private readonly Action<SettingsObject, string[]> _includeSettings;
	// Editors borrow this context; the Options dialog owns the native editor control.
	internal ApplicationSettingsEditorContext(SettingsObject settings, IApplication application,
		IServiceProvider services, IWin32Window owner, Action<SettingsObject, string[]> includeSettings = null) {
		Settings = settings;
		Application = application;
		Services = services;
		Owner = owner;
		_includeSettings = includeSettings;
	}

	/// <summary>The actual settings object owned by the selected plugin, block or screen.</summary>
	public SettingsObject Settings { get; }

	/// <summary>The owning block application, or null for a plain main-form application.</summary>
	public IApplication Application { get; }

	/// <summary>The actual application provider, when the host was configured through DI.</summary>
	public IServiceProvider Services { get; }

	/// <summary>The Options dialog owning any additional native editor windows.</summary>
	public IWin32Window Owner { get; }

	/// <summary>Includes selected members of another existing application settings root on this editor's page, using the same dialog's Apply, Cancel and member-scoped Reset lifecycle.</summary>
	/// <remarks>This does not create a record or transfer ownership. Call before binding the additional root to an editor; the dialog validates graph membership and backs up its original values.</remarks>
	public void IncludeSettings(SettingsObject settings, params string[] memberNames) {
		Guard.ArgumentNotNull(settings, nameof(settings));
		Guard.ArgumentNotNull(memberNames, nameof(memberNames));
		Guard.Ensure(_includeSettings != null, "This settings editor context does not support additional existing settings roots.");
		_includeSettings(settings, memberNames);
	}
}
