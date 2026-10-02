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
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Web.AspNetCore.Blazor;

public class BlazorScreenMenuItem : BlazorApplicationMenuItem, IScreenMenuItem {
	private Type _screenType;
	private IReadOnlyDictionary<string, object> _parameters = new ReadOnlyDictionary<string, object>(new Dictionary<string, object>());

	public Type ScreenType {
		get => _screenType;
		init {
			Tools.UI.ValidateScreenType(value, typeof(IBlazorApplicationScreen));
			_screenType = value;
		}
	}

	public ScreenActivationMode ActivationMode { get; init; } = ScreenActivationMode.MultiInstance;

	public ScreenKind ScreenKind { get; init; }

	public bool IsDefault { get; init; }

	public IReadOnlyDictionary<string, object> Parameters {
		get => _parameters;
		init {
			Guard.ArgumentNotNull(value, nameof(value));
			_parameters = new ReadOnlyDictionary<string, object>(new Dictionary<string, object>(value));
		}
	}

	ScreenActivationMode? IScreenMenuItem.ActivationMode => ActivationMode;

	string IScreenMenuItem.ScreenTitle => Title;

	public static BlazorScreenMenuItem For<TScreen>(string icon, string title) where TScreen : IBlazorApplicationScreen
		=> new() { Id = typeof(TScreen).Name, Icon = icon, Title = title, ScreenType = typeof(TScreen) };
}
