// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows;

/// <summary>Keeps media exactly where its application last reported it: the <see cref="WindowsMediaTrackingStrategy.ProviderReports"/> strategy.</summary>
/// <remarks>It measures nothing itself, so it costs nothing between reports and media follows scrolling only as often as the application reports it.</remarks>
public sealed class ProviderReportMediaTracker : WindowsMediaTrackerBase {
	private WindowsWindowRegion[] _reported = Array.Empty<WindowsWindowRegion>();

	public override WindowsWindowRegion[] Track(WindowsWindowRegion[] reported) {
		Guard.ArgumentNotNull(reported, nameof(reported));
		return _reported = reported;
	}

	public override WindowsWindowRegion[] GetCurrent() => _reported;

	public override void Stop() => _reported = Array.Empty<WindowsWindowRegion>();
}
