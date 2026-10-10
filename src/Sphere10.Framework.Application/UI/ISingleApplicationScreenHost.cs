// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.Framework.Application.UI;

/// <summary>Operations on the current screen without exposing a collection of hosted instances.</summary>
public interface ISingleApplicationScreenHost : IApplicationScreenHost {
	Task<bool> CloseActiveScreenAsync(CancellationToken cancellationToken = default);

	Task<bool> CanCloseActiveScreenAsync(CancellationToken cancellationToken = default);
}

public interface ISingleApplicationScreenHost<TBlock, TScreen> : ISingleApplicationScreenHost, IApplicationScreenHost<TBlock, TScreen>
	where TBlock : class, IApplicationBlock
	where TScreen : class, IApplicationScreen {
}
