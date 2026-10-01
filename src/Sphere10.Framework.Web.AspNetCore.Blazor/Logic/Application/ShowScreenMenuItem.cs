// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Hamish Rose
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.AspNetCore.Components;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Logic;

public class ShowScreenMenuItem : ApplicationMenuItem {
	private Type _screenType;
	private IReadOnlyDictionary<string, object> _parameters = new ReadOnlyDictionary<string, object>(new Dictionary<string, object>());

	public Type ScreenType {
		get => _screenType;
		init {
			Guard.ArgumentNotNull(value, nameof(value));
			Guard.Argument(typeof(IComponent).IsAssignableFrom(value) && typeof(IApplicationScreen).IsAssignableFrom(value)
				&& !value.IsAbstract && !value.ContainsGenericParameters, nameof(value), "A concrete Blazor application screen is required.");
			_screenType = value;
		}
	}

	public ScreenActivationMode ActivationMode { get; init; }

	public IReadOnlyDictionary<string, object> Parameters {
		get => _parameters;
		init {
			Guard.ArgumentNotNull(value, nameof(value));
			_parameters = new ReadOnlyDictionary<string, object>(new Dictionary<string, object>(value));
		}
	}

	public static ShowScreenMenuItem For<TScreen>(string icon, string title) where TScreen : IApplicationScreen
		=> new() { Id = typeof(TScreen).Name, Icon = icon, Title = title, ScreenType = typeof(TScreen) };
}
