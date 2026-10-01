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
	private readonly IEndpointManager _endpointManager;
	private bool _disposed;

	public SidebarBrandViewModel(IEndpointManager endpointManager) {
		Guard.ArgumentNotNull(endpointManager, nameof(endpointManager));
		_endpointManager = endpointManager;
		_endpointManager.EndpointChanged += EndpointManagerOnEvent;
		_endpointManager.EndpointAdded += EndpointManagerOnEvent;
	}

	public Uri Endpoint => _endpointManager.Endpoint;

	public Uri[] Endpoints => _endpointManager.Endpoints;

	public Task OnSelectEndpointAsync(Uri server) => _endpointManager.SetCurrentEndpointAsync(server);

	public void Dispose() {
		if (_disposed)
			return;
		_disposed = true;
		_endpointManager.EndpointChanged -= EndpointManagerOnEvent;
		_endpointManager.EndpointAdded -= EndpointManagerOnEvent;
	}

	private void EndpointManagerOnEvent(object sender, EventArgs args) {
		if (!_disposed)
			StateHasChangedDelegate?.Invoke();
	}
}