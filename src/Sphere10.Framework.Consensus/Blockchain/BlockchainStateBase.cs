// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using Sphere10.Framework;

namespace Sphere10.Framework.Consensus;

/// <summary>
/// Base implementation of <see cref="IBlockchainState"/> providing a no-op update scope.
/// Concrete states override <see cref="EnterUpdateScope"/> to supply transactional semantics.
/// </summary>
public abstract class BlockchainStateBase : IBlockchainState {

	public virtual ActionScope EnterUpdateScope()
		=> Tools.Scope.ExecuteOnDispose(() => { });
}
