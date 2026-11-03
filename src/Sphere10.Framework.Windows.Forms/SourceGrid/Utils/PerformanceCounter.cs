// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows.Forms.SourceGrid.Utils;

public class PerformanceCounter : IDisposable, IPerformanceCounter {
	private DateTime _start = DateTime.MinValue;

	public PerformanceCounter() {
		_start = DateTime.Now;
	}

	public double GetSeconds() {
		var span = DateTime.Now - _start;
		return span.TotalSeconds;
	}

	public double GetMilisec() {
		var span = DateTime.Now - _start;
		return span.TotalMilliseconds;
	}

	public void Dispose() {
	}
}
