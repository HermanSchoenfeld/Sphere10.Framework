// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.Framework.Application.UI;

public abstract class SingleApplicationScreenHostDecorator<TConcrete> : ApplicationScreenHostDecorator<TConcrete>, ISingleApplicationScreenHost
	where TConcrete : ISingleApplicationScreenHost {
	protected SingleApplicationScreenHostDecorator(TConcrete internalHost)
		: base(internalHost) {
	}

	public virtual Task<bool> CloseActiveScreenAsync(CancellationToken cancellationToken = default) => InternalHost.CloseActiveScreenAsync(cancellationToken);

	public virtual Task<bool> CanCloseActiveScreenAsync(CancellationToken cancellationToken = default) => InternalHost.CanCloseActiveScreenAsync(cancellationToken);
}

public abstract class SingleApplicationScreenHostDecorator<TBlock, TScreen, TConcrete> : ApplicationScreenHostDecorator<TBlock, TScreen, TConcrete>, ISingleApplicationScreenHost<TBlock, TScreen>
	where TBlock : class, IApplicationBlock
	where TScreen : class, IApplicationScreen
	where TConcrete : ISingleApplicationScreenHost<TBlock, TScreen> {
	protected SingleApplicationScreenHostDecorator(TConcrete internalHost)
		: base(internalHost) {
	}

	public virtual Task<bool> CloseActiveScreenAsync(CancellationToken cancellationToken = default) => InternalHost.CloseActiveScreenAsync(cancellationToken);

	public virtual Task<bool> CanCloseActiveScreenAsync(CancellationToken cancellationToken = default) => InternalHost.CanCloseActiveScreenAsync(cancellationToken);
}

public abstract class SingleApplicationScreenHostDecorator : SingleApplicationScreenHostDecorator<ISingleApplicationScreenHost> {
	protected SingleApplicationScreenHostDecorator(ISingleApplicationScreenHost internalHost)
		: base(internalHost) {
	}
}

public abstract class SingleApplicationScreenHostDecorator<TBlock, TScreen> : SingleApplicationScreenHostDecorator<TBlock, TScreen, ISingleApplicationScreenHost<TBlock, TScreen>>
	where TBlock : class, IApplicationBlock
	where TScreen : class, IApplicationScreen {
	protected SingleApplicationScreenHostDecorator(ISingleApplicationScreenHost<TBlock, TScreen> internalHost)
		: base(internalHost) {
	}
}
