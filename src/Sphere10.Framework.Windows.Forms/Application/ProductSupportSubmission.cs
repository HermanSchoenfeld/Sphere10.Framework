// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using Sphere10.Framework.Application;

namespace Sphere10.Framework.Windows.Forms;

internal static class ProductSupportSubmission {
	internal static async Task SendAsync<TRequest>(Form dialog, Button sendButton, Button cancelButton, LoadingCircle loadingCircle, IUserInterfaceServices userInterface, TRequest request,
		IEnumerable<ImageAttachmentSelectorControl> attachmentControls, Func<IProductSupportService, TRequest, CancellationToken, Task<Result>> send)
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
		var attachments = new List<ClientRequestAttachment>();
		foreach (var control in attachmentControls.Where(control => control.AttachmentSource != ImageAttachmentSelectorControl.ImageAttachmentSource.None)) {
			if (!control.TryGetAttachment(out var fileName, out var data)) {
				await userInterface.ReportErrorAsync("Attachment", control.AttachmentSource == ImageAttachmentSelectorControl.ImageAttachmentSource.Clipboard
					? "The clipboard holds no image. Copy a screenshot or choose a file."
					: "Choose a file that exists and can be read.");
				return;
			}
			if (data.Length > ClientRequestAttachment.MaxBytes) {
				await userInterface.ReportErrorAsync("Attachment", $"{fileName} is larger than {Tools.Memory.GetBytesReadable(ClientRequestAttachment.MaxBytes)}.");
				return;
			}
			if (fileName.Length > ClientRequestAttachment.MaxFileNameLength) {
				await userInterface.ReportErrorAsync("Attachment", $"The file name must be at most {ClientRequestAttachment.MaxFileNameLength} characters.");
				return;
			}
			attachments.Add(new ClientRequestAttachment(fileName, data));
		}
		if (attachments.Count > ClientRequest.MaxAttachments) {
			await userInterface.ReportErrorAsync("Attachment", $"Attach at most {ClientRequest.MaxAttachments} files.");
			return;
		}
		request.Attachments = attachments.ToArray();
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
		}))
		using (loadingCircle.BeginAnimationScope(dialog, cancelButton)) {
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
