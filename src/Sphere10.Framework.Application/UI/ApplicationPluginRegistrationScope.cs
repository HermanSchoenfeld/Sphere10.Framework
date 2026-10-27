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
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;

namespace Sphere10.Framework.Application.UI;

/// <summary>Reserves a startup plugin's name and removes its new lifecycle registrations if startup registration fails.</summary>
internal sealed class ApplicationPluginRegistrationScope : SyncDisposable, IScope {
	// Commit and rollback refer to the same reserved plugin registration.
	private static readonly ConditionalWeakTable<IServiceCollection, HashSet<string>> _loadingNames = new();
	private static readonly CriticalObject _reservation = new();
	private readonly IServiceCollection _services;
	private readonly string _name;
	private readonly HashSet<ServiceDescriptor> _existingDescriptors;
	private bool _committed;

	public ApplicationPluginRegistrationScope(IServiceCollection services, IApplicationPlugin plugin) {
		Guard.ArgumentNotNull(services, nameof(services));
		Guard.ArgumentNotNull(plugin, nameof(plugin));
		_services = services;
		_name = plugin.Name;
		Guard.Argument(!string.IsNullOrWhiteSpace(_name), nameof(plugin), "A plugin name is required.");
		_existingDescriptors = new HashSet<ServiceDescriptor>(services.Where(IsNamedLifecycleRegistration), ReferenceEqualityComparer.Instance);
		using var access = _reservation.EnterAccessScope();
		var names = _loadingNames.GetValue(services, _ => new HashSet<string>(StringComparer.Ordinal));
		Guard.Argument(names.Add(_name), nameof(plugin), $"Plugin name '{_name}' is already loading.");
	}

	public void Commit() => _committed = true;

	protected override void FreeManagedResources() {
		using var reservation = Tools.Scope.ExecuteOnDispose(() => {
			using var access = _reservation.EnterAccessScope();
			if (_loadingNames.TryGetValue(_services, out var names)) {
				names.Remove(_name);
				if (names.Count == 0)
					_loadingNames.Remove(_services);
			}
		});
		if (_committed)
			return;
		for (var index = _services.Count - 1; index >= 0; index--) {
			var descriptor = _services[index];
			if (IsNamedLifecycleRegistration(descriptor) && !_existingDescriptors.Contains(descriptor))
				_services.RemoveAt(index);
		}
	}

	private bool IsNamedLifecycleRegistration(ServiceDescriptor descriptor)
		=> descriptor.IsKeyedService && descriptor.ServiceKey is string name && StringComparer.Ordinal.Equals(name, _name)
			&& (descriptor.ServiceType == typeof(IApplicationInitializer) || descriptor.ServiceType == typeof(IApplicationFinalizer));
}
