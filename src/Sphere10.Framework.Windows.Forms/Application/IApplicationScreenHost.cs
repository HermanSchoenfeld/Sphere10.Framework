// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;
using SharedUI = global::Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Owns screen instances, selection and their docked or detached presentation.</summary>
public interface IApplicationScreenHost : ISingleApplicationScreenHost<IApplicationBlock, ApplicationScreen>,
	IMultiApplicationScreenHost<IApplicationBlock, ApplicationScreen> {
	event EventHandlerEx<ApplicationScreen?> ActiveScreenChanging;
	event EventHandlerEx<ApplicationScreen?> ActiveScreenChanged;
	/// <summary>Browses a registered block without changing the active screen or running leave guards.</summary>
	void SelectBlock(IApplicationBlock block);
	/// <summary>Registers a block's explicit screen type policies before activating any screen. Conflicting declarations are rejected atomically.</summary>
	void RegisterScreenTypes(IApplicationBlock Block);
	/// <summary>Opens permanent screens and chooses the ordered default when no screen is active.</summary>
	bool InitializeScreens(IEnumerable<IApplicationBlock> blocks);
	/// <summary>Removes a block's definitions and owned instances, including permanent screens, if every screen permits closing.</summary>
	bool UnregisterScreenTypes(IApplicationBlock block);
	/// <summary>Creates a screen or selects its existing single instance, including when registered through another block.</summary>
	ApplicationScreen? ActivateScreen(IApplicationBlock Block, Type ScreenType, string? Title = null);
	ApplicationScreen? ActivateScreen(IApplicationBlock block, SharedUI.ApplicationScreenDefinition definition);
	/// <summary>Shows a supplied instance using its registered type policy. Rejects duplicate single-instance screens and conflicting constructor defaults.</summary>
	bool ShowScreen(ApplicationScreen Screen);

	/// <summary>Shows a caller-created contextual screen through its exact block registration.</summary>
	Task<bool> ShowScreenAsync(ApplicationScreen screen, ApplicationScreenRegistration registration, CancellationToken cancellationToken = default);
	/// <summary>Shows a supplied screen directly in a modeless detached window without selecting a main-frame tab. Requires MultiView and a normal nonpermanent screen.</summary>
	bool ShowScreenUndocked(ApplicationScreen screen);
	bool CloseScreen(ApplicationScreen Screen);
	bool CloseScreens(IEnumerable<ApplicationScreen> Screens);
	Task<bool> CloseAllScreensAsync(CancellationToken cancellationToken = default);
	Task<bool> CloseAllOtherScreensAsync(ApplicationScreen keepScreen, CancellationToken cancellationToken = default);
	bool CanCloseScreens(IEnumerable<ApplicationScreen> Screens);
	bool UndockScreen(ApplicationScreen Screen);
	bool DockScreen(ApplicationScreen Screen);
	bool IsScreenUndocked(ApplicationScreen Screen);
	bool TrySetScreenMode(ScreenMode Mode);
}
