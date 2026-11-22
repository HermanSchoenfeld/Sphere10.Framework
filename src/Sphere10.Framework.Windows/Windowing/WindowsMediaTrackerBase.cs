// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows;

/// <summary>Strategy for where the known media of windows is between two reports from their applications.</summary>
/// <remarks>
/// An application reports where its media is at its own pace; a browser that is scrolling does so about twice a second. The
/// media source hands every report to its tracker and publishes what the tracker returns. A tracker that follows the content
/// by other means raises <see cref="Moved"/> when the media moved without a report, and the source then publishes
/// <see cref="GetCurrent"/>. <see cref="Create"/> picks the implementation for a <see cref="WindowsMediaTrackingStrategy"/>.
/// <see cref="Track"/> and <see cref="GetCurrent"/> are called on the source's dispatcher; <see cref="Moved"/> is raised on
/// whatever thread measured the movement.
/// </remarks>
public abstract class WindowsMediaTrackerBase : SyncDisposable {
	public event EventHandlerEx Moved;

	/// <summary>Creates the tracker that implements a strategy. The caller starts, stops and disposes it.</summary>
	public static WindowsMediaTrackerBase Create(WindowsMediaTrackingStrategy strategy) => strategy switch {
		WindowsMediaTrackingStrategy.ProviderReports => new ProviderReportMediaTracker(),
		WindowsMediaTrackingStrategy.ScreenMotion => new ScreenMotionMediaTracker(),
		_ => throw new ArgumentOutOfRangeException(nameof(strategy), strategy, "Choose a tracking strategy.")
	};

	/// <summary>Begins following media. A tracker that needs no resources of its own does nothing.</summary>
	public virtual void Start() {
	}

	/// <summary>Stops following media and forgets what it tracked. The tracker can be started again.</summary>
	public virtual void Stop() {
	}

	/// <summary>Takes the regions the applications just reported, for every window with media, and returns the regions to show now.</summary>
	public abstract WindowsWindowRegion[] Track(WindowsWindowRegion[] reported);

	/// <summary>The regions to show now, including movement the tracker measured itself since the last report.</summary>
	public abstract WindowsWindowRegion[] GetCurrent();

	protected virtual void OnMoved() {
	}

	protected void NotifyMoved() {
		OnMoved();
		Moved?.Invoke();
	}

	protected override void DisposeInternal() => Stop();
}
