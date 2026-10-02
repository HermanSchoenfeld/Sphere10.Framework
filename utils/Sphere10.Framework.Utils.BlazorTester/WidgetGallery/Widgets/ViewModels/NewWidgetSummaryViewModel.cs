// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Hamish Rose
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Threading.Tasks;
using Sphere10.Framework.Web.AspNetCore.Blazor.Components.Wizard;
using Sphere10.Framework.Utils.BlazorTester.WidgetGallery.Widgets.Models;

namespace Sphere10.Framework.Utils.BlazorTester.WidgetGallery.Widgets.ViewModels;

public class NewWidgetSummaryViewModel : BlazorWizardStepViewModelBase<NewWidgetModel> {
	/// <inheritdoc />
	public override Task<Result> OnNextAsync() {
		return Task.FromResult(Result.Success);
	}

	/// <inheritdoc />
	public override Task<Result> OnPreviousAsync() {
		return Task.FromResult(Result.Success);
	}
}


