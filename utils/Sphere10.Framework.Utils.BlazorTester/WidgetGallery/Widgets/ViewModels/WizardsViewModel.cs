// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Hamish Rose
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Collections.Generic;
using System.Threading.Tasks;
using Sphere10.Framework.Web.AspNetCore.Blazor.Components.Wizard;
using Sphere10.Framework.Web.AspNetCore.Blazor.Services;
using Sphere10.Framework.Web.AspNetCore.Blazor.ViewModels;
using Sphere10.Framework.Utils.BlazorTester.WidgetGallery.Widgets.Components;
using Sphere10.Framework.Utils.BlazorTester.WidgetGallery.Widgets.Models;

namespace Sphere10.Framework.Utils.BlazorTester.WidgetGallery.Widgets.ViewModels;

public class WizardsViewModel : ComponentViewModelBase {
	/// <summary>
	/// Gets the wizard builder.
	/// </summary>
	private IWizardBuilder<NewWidgetModel> Builder { get; }

	/// <summary>
	/// Gets list of widgets 
	/// </summary>
	public List<NewWidgetModel> Widgets { get; } = new();

	/// <summary>
	/// Wizards view model
	/// </summary>
	/// <param name="builder"></param>
	public WizardsViewModel(IWizardBuilder<NewWidgetModel> builder) {
		Builder = builder;
	}

	/// <summary>
	/// Creates a new instance of the wizard model.
	/// </summary>
	/// <returns> new wizard model insteance</returns>
	public IWizard NewWidetWizard() {
		IWizard wizard = Builder.NewWizard("New Widget")
			.WithModel(new NewWidgetModel())
			.AddStep<NewWidgetWizardStep>()
			.AddStep<NewWidgetSummaryStep>()
			.OnCancelled(modal => {
				var result = new Result<bool>(false);
				result.AddError("Cancel not allowed!");
				return Task.FromResult(result);
			})
			.OnFinished(model => {
				Widgets.Add(model);
				return Task.FromResult<Result<bool>>(true);
			})
			.Build();

		return wizard;
	}
}


