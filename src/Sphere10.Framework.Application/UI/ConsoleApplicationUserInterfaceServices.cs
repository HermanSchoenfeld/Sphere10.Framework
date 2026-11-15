// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.Framework.Application;

public class ConsoleApplicationUserInterfaceServices : IUserInterfaceServices {
	public void Exit(bool force = false) {
		ApplicationExiting = true;
		Environment.Exit(-1);
	}

	public bool ApplicationExiting { get; set; } = false;

	public string Status { get; set; } = string.Empty;



	public object PrimaryUIController { get; } = new object();

	public Task ShowSendCommentDialog() => throw new NotSupportedException();

	public Task ShowSubmitBugReportDialog() => throw new NotSupportedException();

	public Task ShowRequestFeatureDialog() => throw new NotSupportedException();

	public Task ShowAboutBox() => throw new NotSupportedException();

	public Task ReportErrorAsync(Exception error, CancellationToken cancellationToken = default)
		=> Console.Out.WriteLineAsync(error.ToDisplayString().AsMemory(), cancellationToken);

	public Task ReportErrorAsync(string message, CancellationToken cancellationToken = default)
		=> Console.Out.WriteAsync(message.AsMemory(), cancellationToken);

	public Task ReportErrorAsync(string title, string message, CancellationToken cancellationToken = default)
		=> Console.Out.WriteLineAsync(message.AsMemory(), cancellationToken);

	public async Task ReportFatalErrorAsync(string title, string message, CancellationToken cancellationToken = default) {
		await ReportErrorAsync(title, message, cancellationToken);
		Exit(true);
	}

	public Task ReportInfoAsync(string title, string message, CancellationToken cancellationToken = default)
		=> Console.Out.WriteLineAsync(message.AsMemory(), cancellationToken);

	public bool AskYN(string question) => throw new NotSupportedException();

	public void ExecuteInUIFriendlyContext(Action action, bool executeAsync = false) => action?.Invoke();
}

