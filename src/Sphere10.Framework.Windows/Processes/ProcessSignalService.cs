// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Sphere10.Framework;
using Sphere10.Framework.Windows;

namespace Sphere10.Framework.Windows;

public static class ProcessSignalService {
	public const string WorkerArgument = "--sphere10-console-signal";

	public static async Task SendConsoleAsync(ProcessIdentity identity, uint signal, CancellationToken cancellationToken = default, WindowsProcessWorkerHost workerHost = null) {
		Guard.Argument(signal is WinAPI.KERNEL32.CTRL_C_EVENT or WinAPI.KERNEL32.CTRL_BREAK_EVENT, nameof(signal), "Choose CTRL+C or CTRL+BREAK.");
		Guard.ArgumentGT(identity.ProcessId, 0, nameof(identity));
		Guard.Ensure(identity.IsVerified && ProcessCommandService.IsCurrent(identity), "The selected process exited or its identity changed.");
		workerHost ??= new WindowsProcessWorkerHost();
		var request = workerHost.CreateStartInfo(WorkerArgument, identity.ProcessId.ToString(CultureInfo.InvariantCulture),
			identity.StartTimeUtc.Value.Ticks.ToString(CultureInfo.InvariantCulture), signal.ToString(CultureInfo.InvariantCulture));
		using var worker = await Task.Run(() => Process.Start(request), cancellationToken);
		Guard.Ensure(worker != null, "Could not start the isolated console signal sender.");
		await using var termination = Tools.Scope.ExecuteOnDisposeAsync<Process>(static async worker => {
			if (!worker.HasExited) {
				worker.Kill();
				await worker.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(3));
			}
		}, worker);
		await worker.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);
		Guard.Ensure(worker.ExitCode == 0, $"Console signal failed (code {worker.ExitCode}); the process may have no accessible console.");
	}

	public static bool TryRunWorker(string[] args, out int exitCode) {
		exitCode = 0;
		if (args == null || args.Length == 0 || args[0] != WorkerArgument)
			return false;
		Tools.Exceptions.ExecuteCapturingException(() => {
			Guard.Argument(args.Length == 4, nameof(args), "Invalid signal sender arguments.");
			var identity = new ProcessIdentity(int.Parse(args[1], CultureInfo.InvariantCulture), new DateTime(long.Parse(args[2], CultureInfo.InvariantCulture), DateTimeKind.Utc));
			var signal = uint.Parse(args[3], CultureInfo.InvariantCulture);
			// Sync-over-async pattern
			ProcessCommandService.ExecuteAsync(identity, process => Tools.Windows.Processes.SendConsoleSignal(process.Id, signal)).GetAwaiter().GetResult();
		}, out var error);
		if (error != null)
			exitCode = 1;
		return true;
	}
}
