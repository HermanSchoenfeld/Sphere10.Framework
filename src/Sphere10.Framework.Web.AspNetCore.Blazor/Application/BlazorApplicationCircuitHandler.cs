// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.Extensions.DependencyInjection;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Web.AspNetCore.Blazor;

/// <summary>Runs application-local lifecycle tasks when an interactive circuit opens and closes.</summary>
internal sealed class BlazorApplicationCircuitHandler : CircuitHandler {
	private readonly IServiceProvider _services;
	private IApplication _application;

	public BlazorApplicationCircuitHandler(IServiceProvider services) {
		Guard.ArgumentNotNull(services, nameof(services));
		_services = services;
	}

	public override async Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken) {
		cancellationToken.ThrowIfCancellationRequested();
		_application ??= _services.GetRequiredService<IApplication>();
		await _application.InitializeAsync(cancellationToken);
	}

	public override async Task OnCircuitClosedAsync(Circuit circuit, CancellationToken cancellationToken) {
		if (_application == null)
			return;
		using var cleanup = Tools.Scope.ExecuteOnDispose(() => _application = null);
		// Circuit teardown must finish even when the circuit's own cancellation token has been canceled.
		await _application.FinalizeAsync();
	}
}
