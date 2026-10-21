// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Sphere10.Framework.Application;
using Microsoft.Extensions.DependencyInjection;

namespace Sphere10.Framework.Windows.Forms;

public class ControlStateEventProviderManager {
	private readonly HashSet<string> _controlStateEventProviders;

	static ControlStateEventProviderManager() {
		Instance = new ControlStateEventProviderManager();
	}

	public ControlStateEventProviderManager() {
		_controlStateEventProviders = new HashSet<string>(StringComparer.Ordinal);
		Refresh();
	}

	public static ControlStateEventProviderManager Instance { get; }


	public void Refresh() {
		_controlStateEventProviders.Clear();
		// Registrations already use exact control names. Resolve the provider only when a live control requests it;
		// enumerating unrelated assembly types here loads optional designer graphs and can disable valid providers.
		if (NamedLookupInfo.TryGetMap(typeof(IControlStateEventProvider), out var serviceMap))
			_controlStateEventProviders.UnionWith(serviceMap.Keys);
	}

	public bool HasControlStateProvider<TControl>() {
		return HasControlStateEventProvider(typeof(TControl));
	}

	public bool HasControlStateEventProvider(Type type)
		=> FindControlStateEventProvider(type, false, out _);

	public bool FindControlStateEventProvider(Type controlType, bool searchSubClasses, out Type implementationForBaseType) {
		var typesToSearch = new[] { controlType };
		if (searchSubClasses)
			typesToSearch = typesToSearch.Concat(controlType.GetAncestorClasses().Where(c => c.IsAssignableTo(typeof(Control)))).ToArray();
		foreach (var type in typesToSearch) {
			if (type != null && _controlStateEventProviders.Contains(type.FullName)) {
				implementationForBaseType = type;
				return true;
			}
		}
		implementationForBaseType = null;
		return false;
	}

	public IControlStateEventProvider GetControlStateProvider<T>() where T : Control
		=> GetControlStateProvider(typeof(T));

	public IControlStateEventProvider GetControlStateProvider(Type controlType) {
		if (!TryGetControlStateProvider(controlType, out var provider))
			throw new InvalidOperationException($"No {nameof(IControlStateEventProvider)} found for {controlType.Name}");
		return provider;
	}

	public bool TryGetControlStateProvider(Type controlType, out IControlStateEventProvider provider) {

		if (FindControlStateEventProvider(controlType, true, out var actualType)) {
			provider = Sphere10Framework.Instance.ServiceProvider.GetNamedService<IControlStateEventProvider>(actualType.FullName);
			return provider != null;
		}
		provider = null;
		return false;
	}

}

