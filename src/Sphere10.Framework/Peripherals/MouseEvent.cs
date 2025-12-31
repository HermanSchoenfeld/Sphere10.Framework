// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework;

public class MouseEvent : EventArgs {
	// Methods
	public MouseEvent(string processName, int x, int y, DateTime time) {
		ProcessName = processName;
		X = x;
		Y = y;
		Time = time;
	}


	public virtual string ProcessName { get; private set; }

	public virtual DateTime Time { get; private set; }

	public int X { get; private set; }

	public int Y { get; private set; }

}

