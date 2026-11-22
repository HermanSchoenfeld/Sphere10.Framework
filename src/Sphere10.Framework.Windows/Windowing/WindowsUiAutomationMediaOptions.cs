// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows;

/// <summary>Bounds media discovery, retained provider references and isolated query time.</summary>
public sealed record WindowsUiAutomationMediaOptions {
	public int MaximumNodes { get; init; } = 512;
	public int MaximumDepth { get; init; } = 24;
	public int MaximumRegions { get; init; } = 128;
	/// <summary>How many containers that are out of view themselves are opened per discovery to find content overflowing them. Zero opens none.</summary>
	public int MaximumOverflowedContainers { get; init; } = 16;
	public int TimeoutMilliseconds { get; init; } = 750;
	/// <summary>How long known media is refreshed without walking the provider tree again, unless a read asks for it.</summary>
	public TimeSpan RediscoveryInterval { get; init; } = TimeSpan.FromSeconds(5);
	public int MaximumCachedWindows { get; init; } = 32;
	public TimeSpan CacheLifetime { get; init; } = TimeSpan.FromSeconds(10);

	/// <summary>The shortest time between two reads of one window while it keeps scrolling or changing. A shorter gap follows scrolling more closely and asks more of the application.</summary>
	public int RefreshIntervalMilliseconds { get; init; } = 50;

	/// <summary>How a window's known media is followed between two reports from its application.</summary>
	public WindowsMediaTrackingStrategy TrackingStrategy { get; init; }

	/// <summary>Images narrower or shorter than this many physical pixels, such as icons, are not media. Zero keeps every image; videos are always kept.</summary>
	public int MinimumImageSize { get; init; }

	public void Validate() {
		Guard.ArgumentInRange(MaximumNodes, 1, 10000, nameof(MaximumNodes));
		Guard.ArgumentInRange(MaximumDepth, 1, 64, nameof(MaximumDepth));
		Guard.ArgumentInRange(MaximumRegions, 1, 1024, nameof(MaximumRegions));
		Guard.ArgumentInRange(MaximumOverflowedContainers, 0, 256, nameof(MaximumOverflowedContainers));
		Guard.ArgumentInRange(TimeoutMilliseconds, 100, 30000, nameof(TimeoutMilliseconds));
		Guard.ArgumentInRange(MaximumCachedWindows, 1, 64, nameof(MaximumCachedWindows));
		Guard.ArgumentInRange(MinimumImageSize, 0, 4096, nameof(MinimumImageSize));
		Guard.ArgumentInRange(RefreshIntervalMilliseconds, 5, 1000, nameof(RefreshIntervalMilliseconds));
		Guard.Argument(Enum.IsDefined(TrackingStrategy), nameof(TrackingStrategy), "Choose a tracking strategy.");
		Guard.Argument(RediscoveryInterval >= TimeSpan.FromMilliseconds(100) && RediscoveryInterval <= TimeSpan.FromMinutes(1), nameof(RediscoveryInterval), "Choose a bounded discovery interval.");
		Guard.Argument(CacheLifetime >= RediscoveryInterval && CacheLifetime <= TimeSpan.FromMinutes(5), nameof(CacheLifetime), "Retain windows for at least one discovery interval, up to five minutes.");
	}
}
