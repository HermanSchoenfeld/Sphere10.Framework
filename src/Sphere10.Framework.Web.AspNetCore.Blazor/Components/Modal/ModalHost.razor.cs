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

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Components.Modal;

/// <summary>Hosts a modal component until its interaction finishes.</summary>
public sealed partial class ModalHost : ModalHostBase<ModalComponentBase, ModalResult> {
	public Task<ModalResult> ShowAsync<T>(ParameterView? parameterView = null) where T : ModalComponentBase {
		var parameters = new Dictionary<string, object>();
		if (parameterView.HasValue) {
			foreach (var parameter in parameterView.Value)
				parameters[parameter.Name] = parameter.Value;
		}
		return ShowCoreAsync<T>(parameters);
	}

	protected override Task WaitUntilRenderedAsync(ModalComponentBase component) => component.ModalRendered;

	protected override Task<ModalResult> GetResultAsync(ModalComponentBase component) => component.ShowAsync();

	protected override Task<bool> RequestCloseAsync(ModalComponentBase component) => component.OnCloseAsync();
}
