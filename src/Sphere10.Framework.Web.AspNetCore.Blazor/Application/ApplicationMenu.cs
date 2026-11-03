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
	private readonly MenuMetadata _definition;
	private string _id;

	public BlazorApplicationMenu()
		: this(true) {
	}

	public BlazorApplicationMenu(bool ownsItems) {
		_definition = new MenuMetadata(this, ownsItems);
	}

	public IBlazorApplicationBlock Parent {
		get => (IBlazorApplicationBlock)_definition.Parent;
		set => _definition.Parent = value;
	}

	public string Id {
		get => _id ?? Text;
		init => _id = value;
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

	internal void AddItem(IBlazorApplicationMenuItem item) => _definition.AddItem(item);

	internal void RemoveItem(IBlazorApplicationMenuItem item) => _definition.RemoveItem(item);

	private sealed class MenuMetadata : ApplicationMenuBase<IBlazorApplicationMenuItem> {
		private readonly IBlazorApplicationMenu _owner;
		private readonly bool _ownsItems;

		public MenuMetadata(IBlazorApplicationMenu owner, bool ownsItems) {
			_owner = owner;
			_ownsItems = ownsItems;
		}

		protected override IApplicationMenu ItemOwner => _owner;

		protected override bool OwnsItems => _ownsItems;
	}
}
