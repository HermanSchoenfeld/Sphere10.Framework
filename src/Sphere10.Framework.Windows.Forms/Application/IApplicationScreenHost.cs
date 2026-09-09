// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Owns screen instances, selection and their docked or detached presentation.</summary>
public interface IApplicationScreenHost {
	event EventHandlerEx<ApplicationScreen?> ActiveScreenChanging;
	event EventHandlerEx<ApplicationScreen?> ActiveScreenChanged;
	ScreenMode ScreenMode { get; set; }
	ApplicationScreen? ActiveScreen { get; }
	IReadOnlyCollection<ApplicationScreen> Screens { get; }
	IReadOnlyCollection<ApplicationScreen> OpenScreens { get; }
	/// <summary>Registers a block's explicit screen type policies before activating any screen. Conflicting declarations are rejected atomically.</summary>
	void RegisterScreenTypes(IApplicationBlock Block);
	/// <summary>Creates a screen or selects its existing single instance, including when registered through another block.</summary>
	ApplicationScreen? ActivateScreen(IApplicationBlock Block, Type ScreenType, string? Title = null);
	/// <summary>Shows a supplied instance using its registered type policy. Rejects duplicate single-instance screens and conflicting constructor defaults.</summary>
	bool ShowScreen(ApplicationScreen Screen);
	bool CloseScreen(ApplicationScreen Screen);
	bool CloseScreens(IEnumerable<ApplicationScreen> Screens);
	bool CanCloseScreens(IEnumerable<ApplicationScreen> Screens);
	bool UndockScreen(ApplicationScreen Screen);
	bool DockScreen(ApplicationScreen Screen);
	bool IsScreenUndocked(ApplicationScreen Screen);
	bool TrySetScreenMode(ScreenMode Mode);
}
