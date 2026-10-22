// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.Framework.Windows;

/// <summary>Only read-only, on-demand Windows power reports; returned localized output is not a parsed inventory.</summary>
public class PowerCfgSentry : ProcessSentry {
	public PowerCfgSentry() : base(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "powercfg.exe")) { }

	public virtual async Task<ProcessExecutionResult> ReadReportAsync(PowerCfgReport report, CancellationToken cancellationToken = default) {
		var arguments = report switch {
			PowerCfgReport.Requests => new[] { "/requests" },
			PowerCfgReport.WakeTimers => new[] { "/waketimers" },
			PowerCfgReport.LastWake => new[] { "/lastwake" },
			PowerCfgReport.AvailableSleepStates => new[] { "/availablesleepstates" },
			PowerCfgReport.WakeArmedDevices => new[] { "/devicequery", "wake_armed" },
			_ => throw new ArgumentOutOfRangeException(nameof(report))
		};
		using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		deadline.CancelAfter(TimeSpan.FromSeconds(15));
		try {
			return await Task.Run(() => RunCapturedAsync(arguments, deadline.Token), deadline.Token).ConfigureAwait(false);
		} catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) {
			throw new TimeoutException("The Windows power report exceeded its 15-second deadline.");
		}
	}
}
