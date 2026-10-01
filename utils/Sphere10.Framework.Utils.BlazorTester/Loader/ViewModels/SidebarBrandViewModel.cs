// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Hamish Rose
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sphere10.Framework.Web.AspNetCore.Blazor.Services;
using Sphere10.Framework.Web.AspNetCore.Blazor.ViewModels;

namespace Sphere10.Framework.Utils.BlazorTester.Loader.ViewModels;

public class SidebarBrandViewModel : ComponentViewModelBase, IDisposable {
	private IEndpointManager EndpointManager { get; }

	public Uri Endpoint => EndpointManager.Endpoint;

	public IEnumerable<Uri> Endpoints => EndpointManager.Endpoints;

	public SidebarBrandViewModel(IEndpointManager endpointManager) {
		Guard.ArgumentNotNull(endpointManager, nameof(endpointManager));
		EndpointManager = endpointManager;
		EndpointManager.EndpointChanged += EndpointManagerOnEvent;
		EndpointManager.EndpointAdded += EndpointManagerOnEvent;
	}

	private void EndpointManagerOnEvent(object sender, EventArgs e) {
		StateHasChangedDelegate?.Invoke();
	}

	public async Task OnSelectEndpointAsync(Uri server) => await EndpointManager.SetCurrentEndpointAsync(server);

	public void Dispose() {
		EndpointManager.EndpointChanged -= EndpointManagerOnEvent;
		EndpointManager.EndpointAdded -= EndpointManagerOnEvent;
	}
}


