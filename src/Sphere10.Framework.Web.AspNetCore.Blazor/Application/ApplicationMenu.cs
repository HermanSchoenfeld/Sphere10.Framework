// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using Sphere10.Framework.Application.UI;
using SharedUI = global::Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Web.AspNetCore.Blazor;

/// <summary>Immutable Blazor facade over shared menu metadata.</summary>
public class ApplicationMenu : IApplicationMenu {
	private readonly MenuMetadata _definition;
	private string _id;

	public ApplicationMenu()
		: this(true) {
	}

	public ApplicationMenu(bool ownsItems) => _definition = new MenuMetadata(this, ownsItems);

	public IApplicationBlock Parent {
		get => (IApplicationBlock)_definition.Parent;
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

	public IApplicationMenuItem[] Items {
		get => _definition.Items;
		init {
			Guard.ArgumentNotNull(value, nameof(value));
			foreach (var item in value)
				_definition.AddItem(item);
		}
	}

	internal void AddItem(IApplicationMenuItem item) => _definition.AddItem(item);

	internal void RemoveItem(IApplicationMenuItem item) => _definition.RemoveItem(item);

	private sealed class MenuMetadata : ApplicationMenuBase<IApplicationMenuItem> {
		private readonly IApplicationMenu _owner;
		private readonly bool _ownsItems;

		public MenuMetadata(IApplicationMenu owner, bool ownsItems) {
			_owner = owner;
			_ownsItems = ownsItems;
		}

		protected override SharedUI.IApplicationMenu ItemOwner => _owner;

		protected override bool OwnsItems => _ownsItems;
	}
}
