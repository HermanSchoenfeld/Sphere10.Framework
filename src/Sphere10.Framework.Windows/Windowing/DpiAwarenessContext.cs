// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Windows;

/// <summary>The Windows awareness modes available for an individual native thread/window.</summary>
public enum DpiAwarenessContext {
	Unaware = -1,
	SystemAware = -2,
	PerMonitorAware = -3,
	PerMonitorAwareV2 = -4,
	UnawareGdiScaled = -5
}
