// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Sphere10.Framework.Windows.Forms;

public abstract class ApplicationScreenHostBase : UserControlEx, IApplicationScreenHost {
	public event EventHandlerEx<ApplicationScreen?>? ActiveScreenChanging;
	public event EventHandlerEx<ApplicationScreen?>? ActiveScreenChanged;

	[DefaultValue(ScreenMode.SingleView)]
	public abstract ScreenMode ScreenMode { get; set; }

	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public abstract ApplicationScreen? ActiveScreen { get; }

	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public abstract IReadOnlyCollection<ApplicationScreen> Screens { get; }

	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public abstract IReadOnlyCollection<ApplicationScreen> OpenScreens { get; }

	public abstract void RegisterScreenTypes(IApplicationBlock Block);

	public abstract ApplicationScreen? ActivateScreen(IApplicationBlock Block, Type ScreenType, string? Title = null);

	public abstract bool ShowScreen(ApplicationScreen Screen);

	public abstract bool CloseScreen(ApplicationScreen Screen);

	public abstract bool CloseScreens(IEnumerable<ApplicationScreen> Screens);

	public abstract bool CanCloseScreens(IEnumerable<ApplicationScreen> Screens);

	public abstract bool UndockScreen(ApplicationScreen Screen);

	public abstract bool DockScreen(ApplicationScreen Screen);

	public abstract bool IsScreenUndocked(ApplicationScreen Screen);

	public abstract bool TrySetScreenMode(ScreenMode Mode);

	protected virtual void OnActiveScreenChanging(ApplicationScreen? Screen) => ActiveScreenChanging?.Invoke(Screen);

	protected virtual void OnActiveScreenChanged(ApplicationScreen? Screen) => ActiveScreenChanged?.Invoke(Screen);
}
