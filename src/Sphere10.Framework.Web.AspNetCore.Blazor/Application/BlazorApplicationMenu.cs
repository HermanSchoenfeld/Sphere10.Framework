// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Logic;

public class ApplicationMenu : IApplicationMenu {
	public string Id { get; init; }

	public string Icon { get; init; }

	public string Text { get; init; }

	public IReadOnlyList<IApplicationMenuItem> Items { get; init; } = Array.Empty<IApplicationMenuItem>();

	IEnumerable<IApplicationMenuItem> IApplicationMenu.Items => Items;
}
