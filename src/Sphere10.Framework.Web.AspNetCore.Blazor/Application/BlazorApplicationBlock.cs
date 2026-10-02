// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Hamish Rose
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Linq;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Web.AspNetCore.Blazor;

/// <summary>Immutable Blazor facade over shared application-block metadata.</summary>
public class BlazorApplicationBlock : IBlazorApplicationBlock {
	public const string DefaultIconUrl = "";

	private readonly ApplicationBlock _definition = new();
	private IBlazorApplicationMenuItem[] _toolBarItems = Array.Empty<IBlazorApplicationMenuItem>();

	public string Id {
		get => _definition.Id;
		init => _definition.Id = value;
	}

	public int Position {
		get => _definition.Position;
		init => _definition.Position = value;
	}

	public string Title {
		get => _definition.Name;
		init => _definition.Name = value;
	}

	public string Name => _definition.Name;

	public string IconUrl { get; init; }

	public string Tooltip { get; init; }

	public IBlazorApplicationMenu[] Menus {
		get => _definition.Menus.Cast<IBlazorApplicationMenu>().ToArray();
		init {
			Guard.ArgumentNotNull(value, nameof(value));
			foreach (var menu in value)
				_definition.AddMenu(menu);
		}
	}

	public IBlazorApplicationMenuItem[] ToolBarItems {
		get => _toolBarItems.ToArray();
		init {
			Guard.ArgumentNotNull(value, nameof(value));
			_toolBarItems = value.ToArray();
		}
	}

	public Type DefaultScreen {
		get => _definition.DefaultScreen;
		init => _definition.DefaultScreen = value;
	}

	public ScreenActivationMode? DefaultScreenActivationMode {
		get => _definition.DefaultScreenActivationMode;
		init => _definition.DefaultScreenActivationMode = value;
	}

	public ScreenKind DefaultScreenKind {
		get => _definition.DefaultScreenKind;
		init => _definition.DefaultScreenKind = value;
	}

	public string DefaultScreenTitle {
		get => _definition.DefaultScreenTitle;
		init => _definition.DefaultScreenTitle = value;
	}
}
