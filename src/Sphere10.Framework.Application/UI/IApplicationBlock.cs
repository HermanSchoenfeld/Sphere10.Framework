// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Application.UI;

/// <summary>UI-independent application block metadata. Platform adapters retain ownership of presentation resources.</summary>
public interface IApplicationBlock {
	string Id => Name;

	string Name { get; }

	int Position { get; }

	IApplicationMenu[] Menus { get; }

	Type DefaultScreen { get; }

	string DefaultScreenTitle => null;
}
