// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework {

	[Flags]
	public enum SizingMask : int  {
		None				= -1,
		CocoaAutoresizing	= 1 << 0,
		UserResizingMask	= 1 << 1,
		Fit					= 1 << 2,
		Compact				= 1 << 3 | UserResizingMask,
		Expand				= 1 << 4 | UserResizingMask,
		FitCompact			= UserResizingMask | Fit | Compact,
		FitExpanding		= UserResizingMask | Fit | Expand
	}
}


