// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Windows.Forms;
using Sphere10.Framework.Application;
using Microsoft.Extensions.DependencyInjection;

namespace Sphere10.Framework.Windows.Forms;

public sealed partial class DRMProductActivationForm : ApplicationForm {

	public DRMProductActivationForm() {
		InitializeComponent();
	}


	private async void _activateButton_Click(object sender, EventArgs e) {
		try {
			var userInterfaceServices = Sphere10Framework.Instance.ServiceProvider.GetService<IUserInterfaceServices>();
			var productLicenseActivator = Sphere10Framework.Instance.ServiceProvider.GetService<IProductLicenseActivator>();
			var productLicenseProvider = Sphere10Framework.Instance.ServiceProvider.GetService<IProductLicenseProvider>();
			var productLicenseEnforcer = Sphere10Framework.Instance.ServiceProvider.GetService<IProductLicenseEnforcer>();

			using (_loadingCircle.BeginAnimationScope(this, _applicationBanner)) {
				await productLicenseActivator.ActivateLicense(_licenseKeyTextBox.Text);
			}
			userInterfaceServices.ReportInfo(
				"Activation Success",
				"Your software is now activated."
			);

			DialogResult = DialogResult.OK;
			Close();
		} catch (Exception error) {
			var userInterfaceServices = Sphere10Framework.Instance.ServiceProvider.GetService<IUserInterfaceServices>();
			userInterfaceServices.ReportError(
				"Activation Error",
				$"Your key is invalid. Please enter correct key. {error.ToDisplayString()}"
			);
		}
	}

	private void _cancelButton_Click(object sender, EventArgs e) {
		DialogResult = DialogResult.Cancel;
		Close();
	}

}

