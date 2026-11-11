// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework;

/// <summary>Invokes a supplied action when disposed.</summary>
public sealed class ActionDisposable : SyncDisposable {
	private readonly Action _endAction;

	/// <summary>Retains the action to invoke during managed cleanup.</summary>
	public ActionDisposable(Action endAction) => _endAction = endAction;

	/// <summary>Invokes the retained action when one was supplied.</summary>
	protected override void DisposeInternal() => _endAction?.Invoke();
}

