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

public abstract class ApplicationScreenHostDecorator<TConcrete> : MultiApplicationScreenHostDecorator<IApplicationBlock, ApplicationScreen, TConcrete>, IApplicationScreenHost
	where TConcrete : IApplicationScreenHost {
	public virtual event EventHandlerEx<ApplicationScreen?> ActiveScreenChanging {
		add => InternalHost.ActiveScreenChanging += value;
		remove => InternalHost.ActiveScreenChanging -= value;
	}

	public virtual event EventHandlerEx<ApplicationScreen?> ActiveScreenChanged {
		add => InternalHost.ActiveScreenChanged += value;
		remove => InternalHost.ActiveScreenChanged -= value;
	}

	protected ApplicationScreenHostDecorator(TConcrete host)
		: base(host) {
	}

	public override ScreenMode ScreenMode {
		get => InternalHost.ScreenMode;
		set => InternalHost.ScreenMode = value;
	}

	public override IUserInterfaceServices UserInterfaceServices => InternalHost.UserInterfaceServices;

	public virtual Task<bool> CloseActiveScreenAsync(CancellationToken cancellationToken = default) => InternalHost.CloseActiveScreenAsync(cancellationToken);

	public virtual Task<bool> CanCloseActiveScreenAsync(CancellationToken cancellationToken = default) => InternalHost.CanCloseActiveScreenAsync(cancellationToken);

	public virtual void SelectBlock(IApplicationBlock block) => InternalHost.SelectBlock(block);

	public virtual void RegisterScreenTypes(IApplicationBlock Block) => InternalHost.RegisterScreenTypes(Block);

	public virtual bool InitializeScreens(IEnumerable<IApplicationBlock> blocks) => InternalHost.InitializeScreens(blocks);

	public virtual bool UnregisterScreenTypes(IApplicationBlock block) => InternalHost.UnregisterScreenTypes(block);

	public virtual ApplicationScreen? ActivateScreen(IApplicationBlock Block, Type ScreenType, string? Title = null)
		=> InternalHost.ActivateScreen(Block, ScreenType, Title);

	public virtual ApplicationScreen? ActivateScreen(IApplicationBlock block, SharedUI.ApplicationScreenDefinition definition)
		=> InternalHost.ActivateScreen(block, definition);

	public virtual bool ShowScreen(ApplicationScreen Screen) => InternalHost.ShowScreen(Screen);

	public virtual Task<bool> ShowScreenAsync(ApplicationScreen screen, ApplicationScreenRegistration registration, CancellationToken cancellationToken = default)
		=> InternalHost.ShowScreenAsync(screen, registration, cancellationToken);

	public virtual bool ShowScreenUndocked(ApplicationScreen screen) => InternalHost.ShowScreenUndocked(screen);

	public override Task<bool> ShowScreenUndockedAsync(ApplicationScreen screen, CancellationToken cancellationToken = default)
		=> InternalHost.ShowScreenUndockedAsync(screen, cancellationToken);

	public virtual bool CloseScreen(ApplicationScreen Screen) => InternalHost.CloseScreen(Screen);

	public virtual bool CloseScreens(IEnumerable<ApplicationScreen> Screens) => InternalHost.CloseScreens(Screens);

	public virtual Task<bool> CloseAllScreensAsync(CancellationToken cancellationToken = default) => InternalHost.CloseAllScreensAsync(cancellationToken);

	public virtual Task<bool> CloseAllOtherScreensAsync(ApplicationScreen keepScreen, CancellationToken cancellationToken = default)
		=> InternalHost.CloseAllOtherScreensAsync(keepScreen, cancellationToken);

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
