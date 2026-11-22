// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Windows;

/// <summary>How a window's known media is followed between two reports from its application. Selects the <see cref="WindowsMediaTrackerBase"/> implementation.</summary>
public enum WindowsMediaTrackingStrategy {
	/// <summary>Media stays where the application last reported it. Applications report scrolling at their own pace: browsers about twice a second.</summary>
	ProviderReports = 0,

	/// <summary>Between reports, media moves with the content as measured from the screen's own frames, so it follows scrolling frame by frame.</summary>
	ScreenMotion = 1
}
