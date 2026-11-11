// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using Sphere10.Framework.Application;
using Microsoft.Extensions.DependencyInjection;

namespace Sphere10.Framework.Windows.Forms;

public partial class ProductProductReportBugDialog : ProductSupportDialog, IProductReportBugDialog {

	public ProductProductReportBugDialog() {
		InitializeComponent();
		InitializeSupportLayout(applicationBanner1, groupBox2, [groupBox3, _attachmentsGroupBox, groupBox1], _sendButton, _cancelButton, _loadingCircle);
	}

	// Resolved when used, so the dialog also constructs on a design surface, where no framework runs.
	protected IUserInterfaceServices UserInterfaceServices => Sphere10Framework.Instance.ServiceProvider.GetService<IUserInterfaceServices>();

	private void _cancelButton_Click(object sender, EventArgs e) => Close();

	private async void _sendButton_Click(object sender, EventArgs e) {
		var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync<Exception>(async () => {
			var product = Sphere10Framework.Instance.ServiceProvider.GetRequiredService<IProductInformationProvider>().ProductInformation;
			await ProductSupportSubmission.SendAsync(this, _sendButton, _cancelButton, _loadingCircle, UserInterfaceServices,
				new BugReport(_whoAreYouControl.UserType, _basicContactDetailsControl.ContactEmail, product, _bugReportTextBox.Text),
				[_firstAttachmentControl, _secondAttachmentControl],
				(service, request, cancellationToken) => service.SubmitBugReportAsync(request, cancellationToken)
			);
		});
		if (error != null) {
			UserInterfaceServices.ReportError(error);
		}
	}
}
