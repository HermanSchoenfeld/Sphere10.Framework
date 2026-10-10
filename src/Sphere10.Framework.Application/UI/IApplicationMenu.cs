// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Application.UI;

public interface IApplicationMenu {
	IApplicationBlock Parent { get; set; }

	string Text { get; }

	IApplicationMenuItem[] Items { get; }
}

public interface IApplicationMenu<out TItem> : IApplicationMenu
	where TItem : class, IApplicationMenuItem {
	new TItem[] Items { get; }

	#region Base Interface Bridges

	IApplicationMenuItem[] IApplicationMenu.Items => Items;

	#endregion
}
