// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using Sphere10.Framework.Application;

namespace Sphere10.Framework.Windows.Forms;

internal static class ProductSupportSubmission {
	internal static async Task SendAsync<TRequest>(Form dialog, Button sendButton, IUserInterfaceServices userInterface, TRequest request, Func<IProductSupportService, TRequest, CancellationToken, Task<Result>> send)
		where TRequest : ClientRequest {
		if (!sendButton.Enabled)
			return;
		if (!string.Equals(request.Sender, "ANONYMOUS", StringComparison.OrdinalIgnoreCase) && !Tools.Mail.IsValidEmail(request.Sender)) {
			await userInterface.ReportErrorAsync("Invalid email", "Enter a valid email address or choose Anonymous.");
			return;
		}
		if (string.IsNullOrWhiteSpace(request.Description) || request.Description.Length > 16000) {
			await userInterface.ReportErrorAsync("Request details", "Enter a description between 1 and 16000 characters.");
			return;
		}
		var service = Sphere10Framework.Instance.ServiceProvider.GetService<IProductSupportService>();
		if (service == null) {
			await userInterface.ReportErrorAsync("Support unavailable", "This application's support service has not been configured.");
			return;
		}
		using var cancellation = new CancellationTokenSource();
		var closeRequested = false;
		var saved = false;
		void OnClosing(object sender, FormClosingEventArgs args) {
			closeRequested = true;
			args.Cancel = true;
			cancellation.Cancel();
		}
		dialog.FormClosing += OnClosing;
		sendButton.Enabled = false;
		using (Tools.Scope.ExecuteOnDispose(() => {
			dialog.FormClosing -= OnClosing;
			if (!sendButton.IsDisposed)
				sendButton.Enabled = true;
		})) {
			try {
				var result = await send(service, request, cancellation.Token);
				if (result.IsFailure)
					await userInterface.ReportErrorAsync("Request failed", string.Join(Environment.NewLine, result.ErrorMessages));
				else {
					saved = true;
					var information = string.Join(Environment.NewLine, result.GetMessages());
					await userInterface.ReportInfoAsync("Request saved", string.IsNullOrWhiteSpace(information) ? "Your request has been saved." : information);
				}
			} catch (OperationCanceledException) when (cancellation.IsCancellationRequested) {
			} catch (Exception error) {
				userInterface.ReportError(error);
			}
		}
		if (saved || closeRequested)
			dialog.Close();
	}
}
