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

public class BlazorScreenMenuItem : BlazorApplicationMenuItemBase, IScreenMenuItem {
	private Type _screenType;
	private ApplicationScreenDefinition _screenDefinition;
	private string _screenTitle;
	private ScreenActivationMode _activationMode = ScreenActivationMode.MultiInstance;
	private ScreenKind _screenKind;
	private IReadOnlyDictionary<string, object> _parameters = new ReadOnlyDictionary<string, object>(new Dictionary<string, object>());

	public Type ScreenType {
		get => _screenDefinition?.ScreenType ?? _screenType;
		init {
			Tools.UI.ValidateScreenType(value, typeof(IBlazorApplicationScreen));
			_screenType = value;
		}
	}

	/// <summary>Optional screen caption, independent of the navigation command label.</summary>
	public string ScreenTitle {
		get => _screenDefinition?.Title ?? _screenTitle;
		init => _screenTitle = value;
	}

	public ScreenActivationMode ActivationMode {
		get => _screenDefinition?.ActivationMode ?? _activationMode;
		init => _activationMode = value;
	}

	public ScreenKind ScreenKind {
		get => _screenDefinition?.ScreenKind ?? _screenKind;
		init => _screenKind = value;
	}

	public ApplicationScreenDefinition ScreenDefinition {
		get => _screenDefinition ??= new ApplicationScreenDefinition(_screenType, _screenTitle ?? Title, _activationMode, _screenKind);
		init {
			Guard.ArgumentNotNull(value, nameof(value));
			Tools.UI.ValidateScreenType(value.ScreenType, typeof(IBlazorApplicationScreen));
			_screenDefinition = value;
		}
	}

	public bool IsDefault { get; init; }

	public IReadOnlyDictionary<string, object> Parameters {
		get => _parameters;
		init {
			Guard.ArgumentNotNull(value, nameof(value));
			_parameters = new ReadOnlyDictionary<string, object>(new Dictionary<string, object>(value));
		}
	}

	public static BlazorScreenMenuItem For<TScreen>(string icon, string title)
		where TScreen : IBlazorApplicationScreen => new() { Id = typeof(TScreen).Name, Icon = icon, Title = title, ScreenType = typeof(TScreen) };
}
