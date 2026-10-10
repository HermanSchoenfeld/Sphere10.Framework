// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.Framework.Application.UI;

public abstract class MultiApplicationScreenHostDecorator<TConcrete> : ApplicationScreenHostDecorator<TConcrete>, IMultiApplicationScreenHost
	where TConcrete : IMultiApplicationScreenHost {
	protected MultiApplicationScreenHostDecorator(TConcrete internalHost)
		: base(internalHost) {
	}

	public virtual IApplicationScreen[] Screens => InternalHost.Screens;

	public virtual IApplicationScreen[] OpenScreens => InternalHost.OpenScreens;

	public virtual Task MoveScreenAsync(IApplicationScreen screen, int index, CancellationToken cancellationToken = default) => InternalHost.MoveScreenAsync(screen, index, cancellationToken);

	public virtual Task<bool> CloseScreensAsync(IEnumerable<IApplicationScreen> screens, CancellationToken cancellationToken = default) => InternalHost.CloseScreensAsync(screens, cancellationToken);

	public virtual Task<bool> ShowScreenAsync(IApplicationScreen screen, CancellationToken cancellationToken = default) => InternalHost.ShowScreenAsync(screen, cancellationToken);

	public virtual Task<bool> CloseScreenAsync(IApplicationScreen screen, CancellationToken cancellationToken = default) => InternalHost.CloseScreenAsync(screen, cancellationToken);

	public virtual Task<bool> CanCloseScreenAsync(IApplicationScreen screen, CancellationToken cancellationToken = default) => InternalHost.CanCloseScreenAsync(screen, cancellationToken);
}

public abstract class MultiApplicationScreenHostDecorator<TBlock, TScreen, TConcrete> : ApplicationScreenHostDecorator<TBlock, TScreen, TConcrete>, IMultiApplicationScreenHost<TBlock, TScreen>
	where TBlock : class, IApplicationBlock
	where TScreen : class, IApplicationScreen
	where TConcrete : IMultiApplicationScreenHost<TBlock, TScreen> {
	protected MultiApplicationScreenHostDecorator(TConcrete internalHost)
		: base(internalHost) {
	}

	public virtual TScreen[] Screens => InternalHost.Screens;

	public virtual TScreen[] OpenScreens => InternalHost.OpenScreens;

	public virtual Task MoveScreenAsync(TScreen screen, int index, CancellationToken cancellationToken = default) => InternalHost.MoveScreenAsync(screen, index, cancellationToken);

	public virtual Task<bool> CloseScreensAsync(IEnumerable<TScreen> screens, CancellationToken cancellationToken = default) => InternalHost.CloseScreensAsync(screens, cancellationToken);

	public virtual Task<bool> ShowScreenAsync(TScreen screen, CancellationToken cancellationToken = default) => InternalHost.ShowScreenAsync(screen, cancellationToken);

	public virtual Task<bool> CloseScreenAsync(TScreen screen, CancellationToken cancellationToken = default) => InternalHost.CloseScreenAsync(screen, cancellationToken);

	public virtual Task<bool> CanCloseScreenAsync(TScreen screen, CancellationToken cancellationToken = default) => InternalHost.CanCloseScreenAsync(screen, cancellationToken);

	// Preserve neutral calls to virtual class members when a subclass explicitly remaps the typed interface.
	IApplicationScreen[] IMultiApplicationScreenHost.Screens => Screens;

	IApplicationScreen[] IMultiApplicationScreenHost.OpenScreens => OpenScreens;

	Task IMultiApplicationScreenHost.MoveScreenAsync(IApplicationScreen screen, int index, CancellationToken cancellationToken) => MoveScreenAsync(Guard.ArgumentCast<TScreen>(screen, nameof(screen)), index, cancellationToken);

	Task<bool> IMultiApplicationScreenHost.CloseScreensAsync(IEnumerable<IApplicationScreen> screens, CancellationToken cancellationToken) {
		Guard.ArgumentNotNull(screens, nameof(screens));
		return CloseScreensAsync(screens.Select(screen => Guard.ArgumentCast<TScreen>(screen, nameof(screens))), cancellationToken);
	}

	Task<bool> IMultiApplicationScreenHost.ShowScreenAsync(IApplicationScreen screen, CancellationToken cancellationToken) => ShowScreenAsync(Guard.ArgumentCast<TScreen>(screen, nameof(screen)), cancellationToken);

	Task<bool> IMultiApplicationScreenHost.CloseScreenAsync(IApplicationScreen screen, CancellationToken cancellationToken) => CloseScreenAsync(Guard.ArgumentCast<TScreen>(screen, nameof(screen)), cancellationToken);

	Task<bool> IMultiApplicationScreenHost.CanCloseScreenAsync(IApplicationScreen screen, CancellationToken cancellationToken) => CanCloseScreenAsync(Guard.ArgumentCast<TScreen>(screen, nameof(screen)), cancellationToken);
}

public abstract class MultiApplicationScreenHostDecorator : MultiApplicationScreenHostDecorator<IMultiApplicationScreenHost> {
	protected MultiApplicationScreenHostDecorator(IMultiApplicationScreenHost internalHost)
		: base(internalHost) {
	}
}

public abstract class MultiApplicationScreenHostDecorator<TBlock, TScreen> : MultiApplicationScreenHostDecorator<TBlock, TScreen, IMultiApplicationScreenHost<TBlock, TScreen>>
	where TBlock : class, IApplicationBlock
	where TScreen : class, IApplicationScreen {
	protected MultiApplicationScreenHostDecorator(IMultiApplicationScreenHost<TBlock, TScreen> internalHost)
		: base(internalHost) {
	}
}
