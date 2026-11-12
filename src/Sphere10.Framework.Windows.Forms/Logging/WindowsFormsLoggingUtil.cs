// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using Sphere10.Framework.Windows.Forms;
using Sphere10.Framework.Windows;
using Sphere10.Framework;

namespace Tools;

public static partial class WinForms {

	/// <summary>Discovers the existing optional output selector and application-owned drain registration.</summary>
	public static class Logging {
		/// <summary>Creates a caller-owned selector on the UI thread. Logging remains off until explicitly configured and started; borrowed loggers and hosts remain caller-owned.</summary>
		public static LogOutputSelectorControl CreateOutputSelector() => new LogOutputSelectorControl();

		/// <summary>Registers output cleanup with the current application's existing LogOutputLifetime service. Call and release on the owner's UI thread; returns null when that service is unavailable.</summary>
		public static IDisposable RegisterOutputLifetime(Control owner, Func<Task> closeOutput) => LogOutputLifetime.RegisterCurrent(owner, closeOutput);

		/// <summary>Registers existing chart behaviors with the current application lifetime. Registration closes the behaviors and joins their accepted output writes; it does not own their chart controls.</summary>
		public static IDisposable RegisterOutputLifetime(Control owner, params ChartLogOutputBehavior[] outputs) => LogOutputLifetime.RegisterCurrent(owner, outputs);
	}
}
