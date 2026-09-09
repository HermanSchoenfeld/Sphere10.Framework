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

public abstract class ApplicationScreenHostDecorator<TConcrete> : IApplicationScreenHost where TConcrete : IApplicationScreenHost {
	public event EventHandlerEx<ApplicationScreen?> ActiveScreenChanging {
		add => InternalHost.ActiveScreenChanging += value;
		remove => InternalHost.ActiveScreenChanging -= value;
	}

	public event EventHandlerEx<ApplicationScreen?> ActiveScreenChanged {
		add => InternalHost.ActiveScreenChanged += value;
		remove => InternalHost.ActiveScreenChanged -= value;
	}

	protected readonly TConcrete InternalHost;

	protected ApplicationScreenHostDecorator(TConcrete Host) {
		Guard.ArgumentNotNull(Host, nameof(Host));
		InternalHost = Host;
	}

	public virtual ScreenMode ScreenMode {
		get => InternalHost.ScreenMode;
		set => InternalHost.ScreenMode = value;
	}

	public virtual ApplicationScreen? ActiveScreen => InternalHost.ActiveScreen;

	public virtual IReadOnlyCollection<ApplicationScreen> Screens => InternalHost.Screens;

	public virtual IReadOnlyCollection<ApplicationScreen> OpenScreens => InternalHost.OpenScreens;

	public virtual void RegisterScreenTypes(IApplicationBlock Block) => InternalHost.RegisterScreenTypes(Block);

	public virtual ApplicationScreen? ActivateScreen(IApplicationBlock Block, Type ScreenType, string? Title = null)
		=> InternalHost.ActivateScreen(Block, ScreenType, Title);

	public virtual bool ShowScreen(ApplicationScreen Screen) => InternalHost.ShowScreen(Screen);

	public virtual bool CloseScreen(ApplicationScreen Screen) => InternalHost.CloseScreen(Screen);

	public virtual bool CloseScreens(IEnumerable<ApplicationScreen> Screens) => InternalHost.CloseScreens(Screens);

	public virtual bool CanCloseScreens(IEnumerable<ApplicationScreen> Screens) => InternalHost.CanCloseScreens(Screens);

	public virtual bool UndockScreen(ApplicationScreen Screen) => InternalHost.UndockScreen(Screen);

	public virtual bool DockScreen(ApplicationScreen Screen) => InternalHost.DockScreen(Screen);

	public virtual bool IsScreenUndocked(ApplicationScreen Screen) => InternalHost.IsScreenUndocked(Screen);

	public virtual bool TrySetScreenMode(ScreenMode Mode) => InternalHost.TrySetScreenMode(Mode);
}

public abstract class ApplicationScreenHostDecorator : ApplicationScreenHostDecorator<IApplicationScreenHost> {
	protected ApplicationScreenHostDecorator(IApplicationScreenHost Host)
		: base(Host) {
	}
}
