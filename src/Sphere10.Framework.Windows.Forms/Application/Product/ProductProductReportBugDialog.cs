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

public partial class ProductProductReportBugDialog : ApplicationForm, IProductReportBugDialog {

	public ProductProductReportBugDialog() {
		InitializeComponent();
		UserInterfaceServices = Sphere10Framework.Instance.ServiceProvider.GetService<IUserInterfaceServices>();
	}

	protected IUserInterfaceServices UserInterfaceServices { get; }

	private void _cancelButton_Click(object sender, EventArgs e) {
		Close();
	}

	private async void _sendButton_Click(object sender, EventArgs e) {
		var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync<Exception>(async () => {
			var product = Sphere10Framework.Instance.ServiceProvider.GetRequiredService<IProductInformationProvider>().ProductInformation;
			await ProductSupportSubmission.SendAsync(this, _sendButton, UserInterfaceServices,
				new BugReport(_whoAreYouControl.UserType, _basicContactDetailsControl.ContactEmail, product, _bugReportTextBox.Text),
				(service, request, cancellationToken) => service.SubmitBugReportAsync(request, cancellationToken)
			);
		});
		if (error != null) {
			UserInterfaceServices.ReportError(error);
		}
	}
}
