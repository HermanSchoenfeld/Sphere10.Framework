// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Web.AspNetCore.Blazor;

/// <summary>Immutable Blazor facade over shared menu metadata.</summary>
public class BlazorApplicationMenu : IBlazorApplicationMenu {
	private readonly ApplicationMenu<IBlazorApplicationMenuItem> _definition = new();

	public string Id {
		get => _definition.Id;
		init => _definition.Id = value;
	}

	public string Icon { get; init; }

	public string Text {
		get => _definition.Text;
		init => _definition.Text = value;
	}

	public IBlazorApplicationMenuItem[] Items {
		get => _definition.Items;
		init {
			Guard.ArgumentNotNull(value, nameof(value));
			foreach (var item in value)
				_definition.AddItem(item);
		}
	}
}
