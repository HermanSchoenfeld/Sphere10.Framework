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
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Application;

public interface IUserInterfaceServices {
	/// <summary>Application interaction notifications.</summary>
	event EventHandlerEx Changed { add { } remove { } }

	IApplicationScreenHost ScreenHost => null;

	Task<bool> ExecuteMenuItemAsync(IApplicationMenuItem item, CancellationToken cancellationToken = default) => throw new NotSupportedException("This UI does not execute application menu commands.");


	void Exit(bool force = false);

	bool ApplicationExiting { get; set; }

	string Status { get; set; }

	object PrimaryUIController { get; }

	Task ShowSendCommentDialog();

	Task ShowSubmitBugReportDialog();

	Task ShowRequestFeatureDialog();

	Task ShowAboutBox();

	/// <summary>Reports an error, completing when the user dismisses the notification.</summary>
	Task ReportErrorAsync(Exception error, CancellationToken cancellationToken = default);

	Task ReportErrorAsync(string message, CancellationToken cancellationToken = default);

	Task ReportErrorAsync(string title, string message, CancellationToken cancellationToken = default);

	Task ReportFatalErrorAsync(string title, string message, CancellationToken cancellationToken = default);

	/// <summary>Reports information, completing when the user dismisses the notification.</summary>
	Task ReportInfoAsync(string title, string message, CancellationToken cancellationToken = default);

	bool AskYN(string question);

	Task<bool> AskYNAsync(string question, CancellationToken cancellationToken = default) {
		cancellationToken.ThrowIfCancellationRequested();
		return Task.FromResult(AskYN(question));
	}

	void ExecuteInUIFriendlyContext(Action action, bool executeAsync = false);

	Task ExecuteInUIFriendlyContextAsync(Func<Task> action, CancellationToken cancellationToken = default) =>
		UserInterfaceServicesDispatch.ExecuteAsync(this, action, cancellationToken);

}

internal static class UserInterfaceServicesDispatch {
	internal static async Task ExecuteAsync(IUserInterfaceServices ui, Func<Task> action, CancellationToken cancellationToken) {
		Guard.ArgumentNotNull(action, nameof(action));
		cancellationToken.ThrowIfCancellationRequested();
		var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		ui.ExecuteInUIFriendlyContext(async () => {
			try {
				cancellationToken.ThrowIfCancellationRequested();
				await action();
				completion.TrySetResult();
			} catch (OperationCanceledException) {
				completion.TrySetCanceled(cancellationToken);
			} catch (Exception error) {
				completion.TrySetException(error);
			}
		}, true);
		await completion.Task.WaitAsync(cancellationToken);
	}
}

