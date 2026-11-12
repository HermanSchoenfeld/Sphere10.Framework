// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows;

/// <summary>Owned-helper transport limits; operation timeouts and result policy remain with the calling service.</summary>
public sealed class WindowsProcessWorkerSessionOptions {
	public TimeSpan TerminationTimeout { get; init; } = TimeSpan.FromSeconds(3);
	public TimeSpan ErrorDrainTimeout { get; init; } = TimeSpan.FromSeconds(1);
	public int MaximumErrorCharacters { get; init; } = 64 * 1024;
	public bool KillDescendants { get; init; }

	internal void Validate() {
		Guard.Argument(TerminationTimeout > TimeSpan.Zero && TerminationTimeout <= TimeSpan.FromMinutes(1), nameof(TerminationTimeout), "Choose a positive bounded cleanup timeout.");
		Guard.Argument(ErrorDrainTimeout > TimeSpan.Zero && ErrorDrainTimeout <= TimeSpan.FromMinutes(1), nameof(ErrorDrainTimeout), "Choose a positive bounded stream-drain timeout.");
		Guard.ArgumentInRange(MaximumErrorCharacters, 0, 1024 * 1024, nameof(MaximumErrorCharacters));
	}
}
