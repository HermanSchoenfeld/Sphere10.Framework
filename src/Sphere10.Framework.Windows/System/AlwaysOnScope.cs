// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Threading.Tasks;

namespace Sphere10.Framework.Windows;

public readonly struct AlwaysOnScope : IScope {
	private readonly uint _priorState;
	public AlwaysOnScope(bool system, bool display) {
		var mode = (uint)WinAPI.KERNEL32.ES_CONTINUOUS;
		if (system)
			mode = mode | WinAPI.KERNEL32.ES_SYSTEM_REQUIRED;
		if (display)
			mode = mode | WinAPI.KERNEL32.ES_DISPLAY_REQUIRED;
		_priorState = WinAPI.KERNEL32.SetThreadExecutionState(mode);

	}

	public void Dispose() {
		if (_priorState != 0)
			WinAPI.KERNEL32.SetThreadExecutionState(_priorState);
	}

	public ValueTask DisposeAsync() => _priorState == 0
		? ValueTask.CompletedTask
		: ValueTask.FromException(new NotSupportedException("Execution-state restoration must run on the thread that entered the scope. Use Dispose instead."));
}

