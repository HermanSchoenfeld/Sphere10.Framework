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
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;
using SharedUI = global::Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Web.AspNetCore.Blazor;

/// <summary>Runs application-local lifecycle tasks when an interactive circuit opens and closes.</summary>
internal sealed class ApplicationCircuitHandler : CircuitHandler {
	private readonly IServiceProvider _services;
	private SharedUI.IApplication _application;

	/// <summary>Borrows the services belonging to this circuit.</summary>
	public ApplicationCircuitHandler(IServiceProvider services) {
		Guard.ArgumentNotNull(services, nameof(services));
		_services = services;
	}

	/// <summary>Initializes this circuit's application under its user-settings binding.</summary>
	public override async Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken) {
		cancellationToken.ThrowIfCancellationRequested();

		// Circuit initialization selects its browser provider through the same facade used by native applications.
		using var settings = UserSettings.UseProvider(_services.GetRequiredService<ISettingsProvider>());
		_application ??= _services.GetRequiredService<SharedUI.IApplication>();
		await _application.InitializeAsync(cancellationToken);
	}

	/// <summary>Completes circuit finalization while its user provider remains bound and alive.</summary>
	public override async Task OnCircuitClosedAsync(Circuit circuit, CancellationToken cancellationToken) {
		if (_application == null)
			return;
		using var settings = UserSettings.UseProvider(_services.GetRequiredService<ISettingsProvider>());
		using var cleanup = Tools.Scope.ExecuteOnDispose(() => _application = null);
		// Circuit teardown must finish even when the circuit's own cancellation token has been canceled.
		await _application.FinalizeAsync();
	}

	/// <summary>Binds the circuit provider for each browser callback and restores the previous flow even when it fails.</summary>
	public override Func<CircuitInboundActivityContext, Task> CreateInboundActivityHandler(Func<CircuitInboundActivityContext, Task> next) {
		Guard.ArgumentNotNull(next, nameof(next));
		return async context => {
			// A static process-wide browser provider would mix users; the existing call context carries only this activity's provider.
			using var settings = UserSettings.UseProvider(_services.GetRequiredService<ISettingsProvider>());
			await next(context);
		};
	}
}
