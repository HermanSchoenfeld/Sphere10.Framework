// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Logic;

public sealed class ApplicationScreenSession {
	internal ApplicationScreenSession(IApplicationBlock block, ShowScreenMenuItem menuItem) {
		Id = Guid.NewGuid();
		Block = block;
		MenuItem = menuItem;
		Parameters = new ReadOnlyDictionary<string, object>(new Dictionary<string, object>(menuItem.Parameters));
	}

	public Guid Id { get; }

	public IApplicationBlock Block { get; }

	public ShowScreenMenuItem MenuItem { get; }

	public Type ScreenType => MenuItem.ScreenType;

	public string Title => MenuItem.Title;

	public IDictionary<string, object> Parameters { get; }

	public IApplicationScreen Screen { get; internal set; }
}
