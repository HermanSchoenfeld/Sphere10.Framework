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

namespace Sphere10.Framework.Windows.Forms;

public abstract class WinFormsApplicationScreenHostDecorator<TConcrete> : IWinFormsApplicationScreenHost where TConcrete : IWinFormsApplicationScreenHost {
	public event EventHandlerEx<WinFormsApplicationScreen?> ActiveScreenChanging {
		add => InternalHost.ActiveScreenChanging += value;
		remove => InternalHost.ActiveScreenChanging -= value;
	}

	public event EventHandlerEx<WinFormsApplicationScreen?> ActiveScreenChanged {
		add => InternalHost.ActiveScreenChanged += value;
		remove => InternalHost.ActiveScreenChanged -= value;
	}

	protected readonly TConcrete InternalHost;

	protected WinFormsApplicationScreenHostDecorator(TConcrete Host) {
		Guard.ArgumentNotNull(Host, nameof(Host));
		InternalHost = Host;
	}

	public virtual ScreenMode ScreenMode {
		get => InternalHost.ScreenMode;
		set => InternalHost.ScreenMode = value;
	}

	public virtual WinFormsApplicationScreen? ActiveScreen => InternalHost.ActiveScreen;

	public virtual WinFormsApplicationScreen[] Screens => InternalHost.Screens;

	public virtual WinFormsApplicationScreen[] OpenScreens => InternalHost.OpenScreens;

	public virtual void RegisterScreenTypes(IWinFormsApplicationBlock Block) => InternalHost.RegisterScreenTypes(Block);

	public virtual bool InitializeScreens(IEnumerable<IWinFormsApplicationBlock> blocks) => InternalHost.InitializeScreens(blocks);

	public virtual bool UnregisterScreenTypes(IWinFormsApplicationBlock block) => InternalHost.UnregisterScreenTypes(block);

	public virtual WinFormsApplicationScreen? ActivateScreen(IWinFormsApplicationBlock Block, Type ScreenType, string? Title = null)
		=> InternalHost.ActivateScreen(Block, ScreenType, Title);

	public virtual bool ShowScreen(WinFormsApplicationScreen Screen) => InternalHost.ShowScreen(Screen);

	public virtual bool CloseScreen(WinFormsApplicationScreen Screen) => InternalHost.CloseScreen(Screen);

	public virtual bool CloseScreens(IEnumerable<WinFormsApplicationScreen> Screens) => InternalHost.CloseScreens(Screens);

	public virtual bool CanCloseScreens(IEnumerable<WinFormsApplicationScreen> Screens) => InternalHost.CanCloseScreens(Screens);

	public virtual bool UndockScreen(WinFormsApplicationScreen Screen) => InternalHost.UndockScreen(Screen);

	public virtual bool DockScreen(WinFormsApplicationScreen Screen) => InternalHost.DockScreen(Screen);

	public virtual bool IsScreenUndocked(WinFormsApplicationScreen Screen) => InternalHost.IsScreenUndocked(Screen);

	public virtual bool TrySetScreenMode(ScreenMode Mode) => InternalHost.TrySetScreenMode(Mode);
}

public abstract class WinFormsApplicationScreenHostDecorator : WinFormsApplicationScreenHostDecorator<IWinFormsApplicationScreenHost> {
	protected WinFormsApplicationScreenHostDecorator(IWinFormsApplicationScreenHost Host)
		: base(Host) {
	}
}
