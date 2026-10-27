// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Threading;


namespace Sphere10.Framework;

public static class SemaphoreSlimExtensions {
	public static ActionScope<SemaphoreSlim> EnterWaitScope(this SemaphoreSlim semaphore) {
		Guard.ArgumentNotNull(semaphore, nameof(semaphore));
		semaphore.Wait();
		return Tools.Scope.ExecuteOnDispose<SemaphoreSlim>(static gate => gate.Release(), semaphore);
	}

}

