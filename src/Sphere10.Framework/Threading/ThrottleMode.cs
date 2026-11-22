// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework;

/// <summary>Controls asynchronous notification delivery by Throttle.ExecuteAsync.</summary>
public enum ThrottleMode {
	/// <summary>Run every requested callback in FIFO order without an interval delay.</summary>
	None,

	/// <summary>Run immediately, then deliver the latest pending callback at the configured interval while requests continue.</summary>
	NotifyEveryInterval,
	
	/// <summary>Deliver only the latest callback after requests have stopped for the configured interval.</summary>
	NotifyOnEventBurstFinished,
	
	/// <summary>Run the first callback immediately and deliver the latest additional request after a quiet interval.</summary>
	NotifyOnEventBurstStartAndFinished
}
