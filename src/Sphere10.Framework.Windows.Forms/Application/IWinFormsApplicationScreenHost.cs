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
public interface IWinFormsApplicationScreenHost {
	event EventHandlerEx<WinFormsApplicationScreen?> ActiveScreenChanging;
	event EventHandlerEx<WinFormsApplicationScreen?> ActiveScreenChanged;
	ScreenMode ScreenMode { get; set; }
	WinFormsApplicationScreen? ActiveScreen { get; }
	WinFormsApplicationScreen[] Screens { get; }
	WinFormsApplicationScreen[] OpenScreens { get; }
	/// <summary>Registers a block's explicit screen type policies before activating any screen. Conflicting declarations are rejected atomically.</summary>
	void RegisterScreenTypes(IWinFormsApplicationBlock Block);
	/// <summary>Creates a screen or selects its existing single instance, including when registered through another block.</summary>
	WinFormsApplicationScreen? ActivateScreen(IWinFormsApplicationBlock Block, Type ScreenType, string? Title = null);
	/// <summary>Shows a supplied instance using its registered type policy. Rejects duplicate single-instance screens and conflicting constructor defaults.</summary>
	bool ShowScreen(WinFormsApplicationScreen Screen);
	bool CloseScreen(WinFormsApplicationScreen Screen);
	bool CloseScreens(IEnumerable<WinFormsApplicationScreen> Screens);
	bool CanCloseScreens(IEnumerable<WinFormsApplicationScreen> Screens);
	bool UndockScreen(WinFormsApplicationScreen Screen);
	bool DockScreen(WinFormsApplicationScreen Screen);
	bool IsScreenUndocked(WinFormsApplicationScreen Screen);
	bool TrySetScreenMode(ScreenMode Mode);
}
