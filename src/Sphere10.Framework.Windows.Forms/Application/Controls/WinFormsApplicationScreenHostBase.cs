// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using Sphere10.Framework.Application.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Sphere10.Framework.Windows.Forms;

public abstract class WinFormsApplicationScreenHostBase : UserControlEx, IWinFormsApplicationScreenHost {
	public event EventHandlerEx<WinFormsApplicationScreen?>? ActiveScreenChanging;
	public event EventHandlerEx<WinFormsApplicationScreen?>? ActiveScreenChanged;

	[DefaultValue(ScreenMode.SingleView)]
	public abstract ScreenMode ScreenMode { get; set; }

	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public abstract WinFormsApplicationScreen? ActiveScreen { get; }

	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public abstract WinFormsApplicationScreen[] Screens { get; }

	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public abstract WinFormsApplicationScreen[] OpenScreens { get; }

	public abstract void RegisterScreenTypes(IWinFormsApplicationBlock Block);

	public abstract bool InitializeScreens(IEnumerable<IWinFormsApplicationBlock> blocks);

	public abstract bool UnregisterScreenTypes(IWinFormsApplicationBlock block);

	public abstract WinFormsApplicationScreen? ActivateScreen(IWinFormsApplicationBlock Block, Type ScreenType, string? Title = null);

	public abstract bool ShowScreen(WinFormsApplicationScreen Screen);

	public abstract bool CloseScreen(WinFormsApplicationScreen Screen);

	public abstract bool CloseScreens(IEnumerable<WinFormsApplicationScreen> Screens);

	public abstract bool CanCloseScreens(IEnumerable<WinFormsApplicationScreen> Screens);

	public abstract bool UndockScreen(WinFormsApplicationScreen Screen);

	public abstract bool DockScreen(WinFormsApplicationScreen Screen);

	public abstract bool IsScreenUndocked(WinFormsApplicationScreen Screen);

	public abstract bool TrySetScreenMode(ScreenMode Mode);

	protected virtual void OnActiveScreenChanging(WinFormsApplicationScreen? Screen) => ActiveScreenChanging?.Invoke(Screen);

	protected virtual void OnActiveScreenChanged(WinFormsApplicationScreen? Screen) => ActiveScreenChanged?.Invoke(Screen);
}
