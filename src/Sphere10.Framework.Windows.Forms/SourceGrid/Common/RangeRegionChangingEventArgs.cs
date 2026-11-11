// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms.SourceGrid;

public class RangeRegionChangingEventArgs : EventArgs {
	private RangeRegion _regionToExclude;
	private RangeRegion _regionToInclude;
	private RangeRegion _currentRegion;

	public RangeRegionChangingEventArgs(RangeRegion currentRegion, RangeRegion rangeToExclude, RangeRegion rangeToInclude) {
		_regionToExclude = rangeToExclude;
		_currentRegion = currentRegion;
		_regionToInclude = rangeToInclude;
	}

	public RangeRegion CurrentRegion => _currentRegion;

	public RangeRegion RegionToInclude => _regionToInclude;

	public RangeRegion RegionToExclude => _regionToExclude;
}
