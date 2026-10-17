// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.Framework.Windows;

public sealed record WindowsWindowInfo(IntPtr Handle, string Title, string ClassName, int ProcessId, uint ThreadId, string ProcessName,
	DateTime? ProcessStartTimeUtc, bool IsVisible, string Error) {
	public bool IsMinimized { get; init; }
}
