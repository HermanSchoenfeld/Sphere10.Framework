// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Hamish Rose
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Sphere10.Framework.Web.AspNetCore.Blazor.Modal;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.UI.Dialogs;

public partial class ModalHost {
	protected override ModalResult CanceledResult => ModalResult.Cancel;

	public Task<ModalResult> ShowAsync<T>(ParameterView parameters, ModalOptions options = null) where T : ModalComponent {
		var values = new Dictionary<string, object> {
			[nameof(ModalComponent.Options)] = options ?? new ModalOptions()
		};
		foreach (var parameter in parameters)
			values[parameter.Name] = parameter.Value;
		return ShowCoreAsync<T>(values);
	}

	protected override Task WaitUntilRenderedAsync(ModalComponent component) => component.ModalRendered;

	protected override Task<ModalResult> GetResultAsync(ModalComponent component) => component.ShowAsync();

	protected override Task<bool> RequestCloseAsync(ModalComponent component) => component.RequestCloseAsync();
}
