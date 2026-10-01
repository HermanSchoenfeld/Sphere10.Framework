// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Hamish Rose
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Threading.Tasks;
using Sphere10.Framework.Web.AspNetCore.Blazor.Components.Wizard;
using Sphere10.Framework.Web.AspNetCore.Blazor.ViewModels;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Components.Modal;

/// <summary>
/// Wizard modal view model
/// </summary>
public class WizardModalViewModel : ModalViewModel {
	/// <summary>
	/// Gets or sets the wizard being hosted in the modal.
	/// </summary>
	public IWizard Wizard { get; set; }

	/// <summary>
	/// Gets or sets the wizard host component instance.
	/// </summary>
	public WizardHost WizardHost;

	public bool CanCancel => Wizard?.IsCancellable == true && (WizardHost?.ViewModel.CanCancel ?? true);

	/// <summary>
	/// Modal closed result. Passes request to the wizard instance to determine whether close OK.
	/// </summary>
	public override async Task<bool> RequestCloseAsync() {
		if (!CanCancel)
			return false;
		if (WizardHost != null)
			return await WizardHost.ViewModel.RequestCancelAsync() && await base.RequestCloseAsync();
		var result = await Wizard.CancelAsync();
		return CanCancel && result.IsSuccess && result.Value && await base.RequestCloseAsync();
	}
}
