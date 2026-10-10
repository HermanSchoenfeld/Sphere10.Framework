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

/// <summary>Operations on retained and open screen instances in a host supporting multiple screens.</summary>
public interface IMultiApplicationScreenHost : IApplicationScreenHost {
	IApplicationScreen[] Screens { get; }

	IApplicationScreen[] OpenScreens { get; }

	Task MoveScreenAsync(IApplicationScreen screen, int index, CancellationToken cancellationToken = default);

	Task<bool> CloseScreensAsync(IEnumerable<IApplicationScreen> screens, CancellationToken cancellationToken = default);

	Task<bool> ShowScreenAsync(IApplicationScreen screen, CancellationToken cancellationToken = default);

	Task<bool> CloseScreenAsync(IApplicationScreen screen, CancellationToken cancellationToken = default);

	Task<bool> CanCloseScreenAsync(IApplicationScreen screen, CancellationToken cancellationToken = default);
}

public interface IMultiApplicationScreenHost<TBlock, TScreen> : IMultiApplicationScreenHost, IApplicationScreenHost<TBlock, TScreen>
	where TBlock : class, IApplicationBlock
	where TScreen : class, IApplicationScreen {
	new TScreen[] Screens { get; }

	new TScreen[] OpenScreens { get; }

	Task MoveScreenAsync(TScreen screen, int index, CancellationToken cancellationToken = default);

	Task<bool> CloseScreensAsync(IEnumerable<TScreen> screens, CancellationToken cancellationToken = default);

	Task<bool> ShowScreenAsync(TScreen screen, CancellationToken cancellationToken = default);

	Task<bool> CloseScreenAsync(TScreen screen, CancellationToken cancellationToken = default);

	Task<bool> CanCloseScreenAsync(TScreen screen, CancellationToken cancellationToken = default);

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
