// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.Framework.Windows;

/// <summary>Structured command wrapper for the Windows in-box pktmon.exe. The caller owns capture/session/filter policy.</summary>
public class PktMonSentry : ProcessSentry {
	public PktMonSentry()
		: base(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "pktmon.exe")) {
	}

	public virtual Task<ProcessExecutionResult> ReadStatusAsync(CancellationToken cancellationToken = default)
		=> ExecuteCommandAsync(new[] { "status" }, cancellationToken);

	public virtual Task<ProcessExecutionResult> ReadFiltersAsync(CancellationToken cancellationToken = default)
		=> ExecuteCommandAsync(new[] { "filter", "list" }, cancellationToken);

	public virtual Task<ProcessExecutionResult> ReadHelpAsync(CancellationToken cancellationToken = default)
		=> ExecuteCommandAsync(new[] { "start", "help" }, cancellationToken);

	public virtual Task<ProcessExecutionResult> StartCaptureAsync(string filePath, int maximumMegabytes, bool networkAdaptersOnly, CancellationToken cancellationToken = default) {
		Guard.ArgumentNotNullOrWhitespace(filePath, nameof(filePath));
		Guard.ArgumentGT(maximumMegabytes, 0, nameof(maximumMegabytes));
		return ExecuteCommandAsync(new[] { "start", "--capture", "--comp", networkAdaptersOnly ? "nics" : "all", "--type", "all", "--pkt-size", "0",
			"--file-name", Path.GetFullPath(filePath), "--file-size", maximumMegabytes.ToString(CultureInfo.InvariantCulture), "--log-mode", "circular" }, cancellationToken);
	}

	public virtual Task<ProcessExecutionResult> StopCaptureAsync(CancellationToken cancellationToken = default)
		=> ExecuteCommandAsync(new[] { "stop" }, cancellationToken);

	public virtual Task<ProcessExecutionResult> ConvertToTextAsync(string filePath, string outputPath, CancellationToken cancellationToken = default) {
		Guard.ArgumentNotNullOrWhitespace(filePath, nameof(filePath));
		Guard.ArgumentNotNullOrWhitespace(outputPath, nameof(outputPath));
		return ExecuteCommandAsync(new[] { "etl2txt", Path.GetFullPath(filePath), "--out", Path.GetFullPath(outputPath), "--hex" }, cancellationToken);
	}

	public virtual Task<ProcessExecutionResult> ConvertToPcapngAsync(string filePath, string outputPath, CancellationToken cancellationToken = default) {
		Guard.ArgumentNotNullOrWhitespace(filePath, nameof(filePath));
		Guard.ArgumentNotNullOrWhitespace(outputPath, nameof(outputPath));
		return ExecuteCommandAsync(new[] { "etl2pcap", Path.GetFullPath(filePath), "--out", Path.GetFullPath(outputPath) }, cancellationToken);
	}

	protected virtual async Task<ProcessExecutionResult> ExecuteCommandAsync(string[] arguments, CancellationToken cancellationToken) {
		using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		deadline.CancelAfter(TimeSpan.FromSeconds(30));
		try {
			return await RunCapturedAsync(arguments, deadline.Token).ConfigureAwait(false);
		} catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) {
			throw new TimeoutException("The pktmon command exceeded its 30-second deadline; its owned helper process was terminated and drained.");
		}
	}
}
