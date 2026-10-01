// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Application.UI;

/// <summary>Copies platform definitions into validated structural snapshots.</summary>
public interface IApplicationBlockSnapshot<TBlock, TMenu, TItem>
	where TBlock : class, IApplicationBlock
	where TMenu : class, IApplicationMenu
	where TItem : class, IApplicationMenuItem {
	TBlock Create(TBlock block);

	TMenu CreateMenu(TMenu menu);

	TItem CreateItem(TItem item);

	TItem GetDefaultScreen(TBlock block);
}
