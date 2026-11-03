// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Threading.Tasks;

namespace Sphere10.Framework;

/// <summary>Owns a collection of resources disposed through the caller's chosen direction.</summary>
public class DisposableResource : Disposable {

	/// <summary>Resources owned by this instance and its derived implementations.</summary>
	protected Disposables Disposables = new Disposables();

	/// <summary>Awaits asynchronous cleanup of the owned resources.</summary>
	protected override ValueTask DisposeInternalAsync() {
		return Disposables.DisposeAsync();
	}

	/// <summary>Disposes the owned resources synchronously.</summary>
	protected override void DisposeInternal() {
		Disposables.Dispose();
	}
}

