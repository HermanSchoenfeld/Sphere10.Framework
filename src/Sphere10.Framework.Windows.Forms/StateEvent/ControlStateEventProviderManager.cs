// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using Sphere10.Framework.Application;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Resolves fresh state observers, preferring exact application registrations over inherited defaults.</summary>
public class ControlStateEventProviderManager {
	private static readonly Dictionary<Type, Type> _defaultProviders = new() {
		[typeof(FormEx)] = typeof(FormEx.StateEventProvider),
		[typeof(UserControlEx)] = typeof(UserControlEx.StateEventProvider),
		[typeof(UserControl)] = typeof(ContainerControlStateEventProvider),
		[typeof(ContainerControl)] = typeof(ContainerControlStateEventProvider),
		[typeof(Panel)] = typeof(ContainerControlStateEventProvider),
		[typeof(GroupBox)] = typeof(ContainerControlStateEventProvider),
		[typeof(SplitContainer)] = typeof(SplitContainerControlStateEventProvider),
		[typeof(TabControl)] = typeof(TabControlStateEventProvider),
		[typeof(PathSelectorControl)] = typeof(PathSelectorControl.StateEventProvider),
		[typeof(NumericUpDown)] = typeof(CommonControlStateEventProvider),
		[typeof(TextBox)] = typeof(CommonControlStateEventProvider),
		[typeof(TextBoxBase)] = typeof(CommonControlStateEventProvider),
		[typeof(ComboBox)] = typeof(CommonControlStateEventProvider),
		[typeof(RadioButton)] = typeof(CommonControlStateEventProvider),
		[typeof(CheckBox)] = typeof(CommonControlStateEventProvider),
		[typeof(CheckedListBox)] = typeof(CommonControlStateEventProvider),
		[typeof(ListBox)] = typeof(CommonControlStateEventProvider),
		[typeof(DateTimePicker)] = typeof(CommonControlStateEventProvider),
		[typeof(MonthCalendar)] = typeof(CommonControlStateEventProvider),
		[typeof(TrackBar)] = typeof(CommonControlStateEventProvider),
		[typeof(ScrollBar)] = typeof(CommonControlStateEventProvider),
		[typeof(DomainUpDown)] = typeof(CommonControlStateEventProvider),
		[typeof(PropertyGrid)] = typeof(CommonControlStateEventProvider),
		[typeof(SourceGrid.GridVirtual)] = typeof(SourceGridControlStateEventProvider),
		[typeof(ToolStrip)] = typeof(ToolStripControlStateEventProvider),
		[typeof(DataVisualization.Chart)] = typeof(ChartControlStateEventProvider),
		[typeof(Expando)] = typeof(ExpandoControlStateEventProvider),
		[typeof(TaskPane)] = typeof(ContainerControlStateEventProvider),
		[typeof(PadLockButton)] = typeof(PadLockButtonControlStateEventProvider),
		[typeof(Crud.CrudComboBox)] = typeof(CrudComboBoxControlStateEventProvider)
	};
	private readonly HashSet<string> _controlStateEventProviders;

	static ControlStateEventProviderManager() {
		Instance = new ControlStateEventProviderManager();
	}

	/// <summary>Captures registration names without constructing providers or inspecting unrelated assemblies.</summary>
	public ControlStateEventProviderManager() {
		_controlStateEventProviders = new HashSet<string>(StringComparer.Ordinal);
		Refresh();
	}

	/// <summary>The normal process-wide registration resolver.</summary>
	public static ControlStateEventProviderManager Instance {
		get;
	}

	/// <summary>Gets a fresh observer for the exact control or its nearest supported ancestor.</summary>
	public IControlStateEventProvider GetControlStateProvider<T>()
		where T : Control {
		return GetControlStateProvider(typeof(T));
	}

	/// <summary>Gets a fresh observer, rejecting controls without editable state support.</summary>
	public IControlStateEventProvider GetControlStateProvider(Type controlType) {
		Guard.ArgumentNotNull(controlType, nameof(controlType));
		if (!TryGetControlStateProvider(controlType, out var provider))
			throw new InvalidOperationException($"No {nameof(IControlStateEventProvider)} found for {controlType.Name}");
		return provider;
	}

	/// <summary>Tries application registrations and built-in observers without requiring Framework startup.</summary>
	public bool TryGetControlStateProvider(Type controlType, out IControlStateEventProvider provider) {
		var lookup = GetRegisteredProviders();
		for (var type = controlType; type != null && typeof(Control).IsAssignableFrom(type); type = type.BaseType) {
			if (_controlStateEventProviders.Contains(type.FullName) && lookup != null) {
				// Provider construction happens outside the availability fallback: custom factory failures propagate.
				provider = lookup[type.FullName];
				if (provider != null)
					return true;
			}
			provider = CreateDefaultProvider(type);
			if (provider != null)
				return true;
		}
		provider = null;
		return false;
	}

	/// <summary>Refreshes only names, leaving optional and unused provider implementations lazy.</summary>
	public void Refresh() {
		_controlStateEventProviders.Clear();
		// Registrations already use exact control names. Resolve only when a live control requests its observer.
		if (NamedLookupInfo.TryGetMap(typeof(IControlStateEventProvider), out var serviceMap))
			_controlStateEventProviders.UnionWith(serviceMap.Keys);
	}

	/// <summary>Checks whether the exact control type has a registered or built-in observer.</summary>
	public bool HasControlStateProvider<TControl>() {
		return HasControlStateEventProvider(typeof(TControl));
	}

	/// <summary>Checks exact support without constructing a provider.</summary>
	public bool HasControlStateEventProvider(Type type) {
		return FindControlStateEventProvider(type, false, out _);
	}

	/// <summary>Finds the exact supported control type, optionally walking nearest ancestors first.</summary>
	public bool FindControlStateEventProvider(Type controlType, bool searchSubClasses, out Type implementationForBaseType) {
		for (var type = controlType; type != null && typeof(Control).IsAssignableFrom(type); type = searchSubClasses ? type.BaseType : null) {
			if (_controlStateEventProviders.Contains(type.FullName) || _defaultProviders.ContainsKey(type)) {
				implementationForBaseType = type;
				return true;
			}
		}
		implementationForBaseType = null;
		return false;
	}

	/// <summary>Registers the same built-in observers used for standalone construction, retaining application overrides.</summary>
	internal static void RegisterDefaultProviders(IServiceCollection services) {
		foreach (var registration in _defaultProviders) {
			if (!services.HasNamedImplementationFor<IControlStateEventProvider>(registration.Key.FullName))
				services.AddNamed(ServiceDescriptor.Transient(typeof(IControlStateEventProvider), registration.Value), registration.Key.FullName);
		}
	}

	/// <summary>Creates an independent built-in attachment for one exact native control type.</summary>
	internal static IControlStateEventProvider CreateDefaultProvider(Type controlType) {
		return _defaultProviders.TryGetValue(controlType, out var providerType)
			? (IControlStateEventProvider)providerType.ActivateWithCompatibleArgs()
			: null;
	}

	// Shutdown can leave the global provider reference pointing at a disposed container.
	private static INamedLookup<IControlStateEventProvider> GetRegisteredProviders() {
		var serviceProvider = Sphere10Framework.Instance.ServiceProvider;
		if (serviceProvider == null)
			return null;

		INamedLookup<IControlStateEventProvider> lookup = null;
		Tools.Exceptions.ExecuteCapturingException<ObjectDisposedException>(() => {
			lookup = serviceProvider.GetService<INamedLookup<IControlStateEventProvider>>();
		}, out _);
		return lookup;
	}
}
