// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;

namespace Sphere10.Framework.Application.UI;

/// <summary>Reusable block metadata and menu storage; platform subclasses own presentation resources.</summary>
public class ApplicationBlock : IApplicationBlock {
	private string _id;
	private readonly List<IApplicationMenu> _menus = new();

	public virtual string Id {
		get => _id ?? Name;
		set => _id = value;
	}

	public virtual string Name { get; set; } = string.Empty;

	public virtual int Position { get; set; }

	public virtual Type DefaultScreen { get; set; }

	public virtual string DefaultScreenTitle { get; set; }

	/// <summary>Returns a membership snapshot; mutate the block through AddMenu and RemoveMenu.</summary>
	public virtual IApplicationMenu[] Menus => _menus.ToArray();

	protected IApplicationMenu[] MenuCollection => _menus.ToArray();

	public virtual void AddMenu(IApplicationMenu menu) {
		Guard.ArgumentNotNull(menu, nameof(menu));
		_menus.Add(menu);
	}

	public virtual bool ContainsMenu(IApplicationMenu menu) => _menus.Contains(menu);

	public virtual void RemoveMenu(IApplicationMenu menu) => _menus.Remove(menu);
}
