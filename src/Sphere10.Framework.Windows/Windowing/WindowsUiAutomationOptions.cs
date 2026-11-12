// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Windows;

/// <summary>Explicit budgets for one read-only selected-window Control View capture.</summary>
public sealed record WindowsUiAutomationOptions {
	public int MaximumNodes { get; init; } = 1000;
	public int MaximumDepth { get; init; } = 12;
	public int TimeoutMilliseconds { get; init; } = 5000;
	public bool IncludePatternSupport { get; init; } = true;

	public void Validate() {
		Guard.ArgumentInRange(MaximumNodes, 1, 10000, nameof(MaximumNodes));
		Guard.ArgumentInRange(MaximumDepth, 1, 32, nameof(MaximumDepth));
		Guard.ArgumentInRange(TimeoutMilliseconds, 1000, 30000, nameof(TimeoutMilliseconds));
	}
}
