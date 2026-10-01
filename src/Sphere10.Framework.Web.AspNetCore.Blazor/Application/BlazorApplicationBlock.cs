// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Hamish Rose
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Logic;

public class ApplicationBlock : IApplicationBlock {
	public const string DefaultIconUrl = "";

	public string Id { get; init; }

	public int Position { get; init; }

	public string Title { get; init; }

	public string Name => Title;

	public string IconUrl { get; init; }

	public string Tooltip { get; init; }

	public IReadOnlyList<IApplicationMenu> Menus { get; init; } = Array.Empty<IApplicationMenu>();

	public Type DefaultScreen { get; init; }

	public string DefaultScreenTitle { get; init; }
}
