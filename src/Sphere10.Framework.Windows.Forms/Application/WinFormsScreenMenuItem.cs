// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing;
using Sphere10.Framework.Application.UI;
using SharedUI = global::Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Windows.Forms;

public class WinFormsScreenMenuItem : WinFormsLinkMenuItemBase, IWinFormsScreenMenuItem {
	private SharedUI.ApplicationScreenDefinition _screenDefinition;
	private ApplicationScreenRegistration _registration;

	public WinFormsScreenMenuItem()
		: this(string.Empty, null) {
	}

	public WinFormsScreenMenuItem(string text, Type viewType)
		: this(text, viewType, null) {
	}


	public WinFormsScreenMenuItem(string text, Type screenType, Image image16x16)
		: this(text, screenType, image16x16, true, true, false) {
	}

	public WinFormsScreenMenuItem(string text, Type screenType, Image image16x16, bool showOnExplorerBar, bool showOnToolBar, bool isStartScreen)
		: base(text, image16x16, showOnExplorerBar, showOnToolBar, isStartScreen) {
		_registration = screenType == null ? null : ApplicationScreenRegistration.ForType(screenType);
	}

	/// <summary>Creates a navigation command for a separately registered screen.</summary>
	public WinFormsScreenMenuItem(ApplicationScreenRegistration registration, string text = null, Image image16x16 = null,
		bool showOnExplorerBar = true, bool showOnToolBar = true, bool isStartScreen = false)
		: base(text, image16x16, showOnExplorerBar, showOnToolBar, isStartScreen) {
		Guard.ArgumentNotNull(registration, nameof(registration));
		Tools.UI.ValidateScreenType(registration.ScreenType, typeof(ApplicationScreen));
		_registration = registration;
	}

	/// <summary>Creates a command without adding the supplied definition to a block.</summary>
	public WinFormsScreenMenuItem(SharedUI.ApplicationScreenDefinition definition, string text = null, Image image16x16 = null,
		bool showOnExplorerBar = true, bool showOnToolBar = true, bool isStartScreen = false)
		: this(definition?.Registration, text, image16x16, showOnExplorerBar, showOnToolBar, isStartScreen) {
		ScreenDefinition = definition;
	}

	public ApplicationScreenRegistration Registration {
		get {
			return _registration;
		}
	}

	public virtual SharedUI.ApplicationScreenDefinition ScreenDefinition {
		get => _screenDefinition;
		set {
			if (ReferenceEquals(_screenDefinition, value))
				return;
			if (Parent != null)
				_screenDefinition?.RemoveActivator(this);
			_screenDefinition = value;
			_registration = value?.Registration ?? _registration;
			if (Parent != null)
				_screenDefinition?.AddActivator(this);
		}
	}

	/// <summary>Binds this command to its owner's canonical registration and supplies an omitted caption.</summary>
	public void ResolveScreenDefinition(SharedUI.ApplicationScreenDefinition definition) {
		Guard.ArgumentNotNull(definition, nameof(definition));
		var registration = _registration;
		ScreenDefinition = definition;
		_registration = registration;
		if (string.IsNullOrWhiteSpace(Text))
			Text = definition.Title;
	}

	public override void Dispose() {
		base.Dispose();
	}
}

