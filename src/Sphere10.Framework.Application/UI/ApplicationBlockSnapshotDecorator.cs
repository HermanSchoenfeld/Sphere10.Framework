// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Application.UI;

public abstract class ApplicationBlockSnapshotDecorator<TBlock, TMenu, TItem, TConcrete> : IApplicationBlockSnapshot<TBlock, TMenu, TItem>
	where TBlock : class, IApplicationBlock
	where TMenu : class, IApplicationMenu
	where TItem : class, IApplicationMenuItem
	where TConcrete : IApplicationBlockSnapshot<TBlock, TMenu, TItem> {
	protected readonly TConcrete InternalSnapshot;

	protected ApplicationBlockSnapshotDecorator(TConcrete snapshot) {
		Guard.ArgumentNotNull(snapshot, nameof(snapshot));
		InternalSnapshot = snapshot;
	}

	public virtual TBlock Create(TBlock block) => InternalSnapshot.Create(block);

	public virtual TMenu CreateMenu(TMenu menu) => InternalSnapshot.CreateMenu(menu);

	public virtual TItem CreateItem(TItem item) => InternalSnapshot.CreateItem(item);

	public virtual TItem GetDefaultScreen(TBlock block) => InternalSnapshot.GetDefaultScreen(block);
}

public abstract class ApplicationBlockSnapshotDecorator<TBlock, TMenu, TItem>
	: ApplicationBlockSnapshotDecorator<TBlock, TMenu, TItem, IApplicationBlockSnapshot<TBlock, TMenu, TItem>>
	where TBlock : class, IApplicationBlock
	where TMenu : class, IApplicationMenu
	where TItem : class, IApplicationMenuItem {
	protected ApplicationBlockSnapshotDecorator(IApplicationBlockSnapshot<TBlock, TMenu, TItem> snapshot)
		: base(snapshot) {
	}
}
