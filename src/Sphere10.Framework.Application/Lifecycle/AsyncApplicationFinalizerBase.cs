// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Threading.Tasks;

namespace Sphere10.Framework.Application;

public abstract class AsyncApplicationFinalizerBase : ApplicationFinalizerBase {
#pragma warning disable CS0465 // Implements the existing synchronous lifecycle task contract.
	public override void Finalize()
		=> Task.Run(() => FinalizeAsync()).GetAwaiter().GetResult();
#pragma warning restore CS0465
}
