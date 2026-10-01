// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sphere10.Framework.Web.AspNetCore.Blazor.Services;

namespace Sphere10.Framework.Utils.BlazorTester.Loader.Services;

/// <summary>Per-session endpoint selections for the gallery's in-memory data service.</summary>
public class DefaultEndpointManager : IEndpointManager {
	public event EventHandler<EventArgs> EndpointChanged;
	public event EventHandler<EventArgs> EndpointAdded;

	private readonly List<Uri> _endpoints = new() { new Uri("https://demo.sphere10.local/") };

	public DefaultEndpointManager() {
		Endpoint = _endpoints[0];
	}

	public Uri Endpoint { get; private set; }

	public Uri[] Endpoints => _endpoints.ToArray();

	public Task<Result<bool>> ValidateEndpointAsync(Uri uri) => Task.FromResult<Result<bool>>(uri is { IsAbsoluteUri: true } && (uri.Scheme == "https" || uri.Scheme == "http"));

	public Task SetCurrentEndpointAsync(Uri uri) {
		Guard.Argument(_endpoints.Contains(uri), nameof(uri), "Select an available endpoint.");
		Endpoint = uri;
		EndpointChanged?.Invoke(this, EventArgs.Empty);
		return Task.CompletedTask;
	}

	public async Task AddEndpointAsync(Uri uri) {
		var result = await ValidateEndpointAsync(uri);
		Guard.Argument(result, nameof(uri), "Enter an absolute HTTP or HTTPS address.");
		if (_endpoints.Contains(uri))
			return;
		_endpoints.Add(uri);
		EndpointAdded?.Invoke(this, EventArgs.Empty);
	}
}
