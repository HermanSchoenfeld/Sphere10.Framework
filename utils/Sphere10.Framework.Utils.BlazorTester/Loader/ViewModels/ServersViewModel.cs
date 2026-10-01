// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Hamish Rose
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Sphere10.Framework.Web.AspNetCore.Blazor.Services;
using Sphere10.Framework.Web.AspNetCore.Blazor.ViewModels;

namespace Sphere10.Framework.Utils.BlazorTester.Loader.ViewModels;

/// <summary>
/// View model for servers page.
/// </summary>
public class ServersViewModel : ComponentViewModelBase, IDisposable {
	/// <summary>
	/// Gets the available servers
	/// </summary>
	public Uri[] Servers => EndpointManager.Endpoints;

	/// <summary>
	/// Getse the server config service.
	/// </summary>
	private IEndpointManager EndpointManager { get; }

	/// <summary>
	/// Gets the modal service
	/// </summary>
	private IModalService ModalService { get; }

	/// <summary>
	/// Gets the active server
	/// </summary>
	public Uri ActiveServer => EndpointManager.Endpoint;

	/// <summary>
	/// Gets or sets the new server model
	/// </summary>
	public NewServer NewServer { get; set; } = new();

	/// <summary>
	/// Initializes a new instance of the <see cref="ServersViewModel"/> class.
	/// </summary>
	/// <param name="configService"></param>
	/// <param name="modalService"></param>
	public ServersViewModel(IEndpointManager configService, IModalService modalService) {
		Guard.ArgumentNotNull(configService, nameof(configService));
		EndpointManager = configService;
		ModalService = modalService;
		configService.EndpointChanged += EndpointManagerOnEndpointChanged;
	}

	/// <summary>
	/// Handles active server change
	/// </summary>
	/// <param name="sender"></param>
	/// <param name="e"></param>
	private void EndpointManagerOnEndpointChanged(object sender, EventArgs e) {
		StateHasChangedDelegate?.Invoke();
	}

	/// <summary>
	/// Handles select new active server.
	/// </summary>
	/// <param name="server"></param>
	/// <returns></returns>
	public async Task OnSelectActiveServer(Uri server) {
		await EndpointManager.SetCurrentEndpointAsync(server);
		StateHasChangedDelegate?.Invoke();
	}

	/// <summary>
	/// Handles form submit for new server
	/// </summary>
	/// <returns></returns>
	public async Task OnAddNewServerAsync() {
		if (NewServer.Uri is not null) {
			Uri address = new(NewServer.Uri);
			await EndpointManager.AddEndpointAsync(address);
			StateHasChangedDelegate?.Invoke();
		}
	}
	public void Dispose() {
		EndpointManager.EndpointChanged -= EndpointManagerOnEndpointChanged;
	}

}


public class NewServer {
	[Required(AllowEmptyStrings = false), Url] public string Uri { get; set; }
}


