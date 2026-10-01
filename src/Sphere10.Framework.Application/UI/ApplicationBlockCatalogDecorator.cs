// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Application.UI;

public abstract class ApplicationBlockCatalogDecorator<TBlock, TConcrete> : IApplicationBlockCatalog<TBlock>
	where TBlock : class, IApplicationBlock
	where TConcrete : IApplicationBlockCatalog<TBlock> {
	protected readonly TConcrete InternalCatalog;

	protected ApplicationBlockCatalogDecorator(TConcrete catalog) {
		Guard.ArgumentNotNull(catalog, nameof(catalog));
		InternalCatalog = catalog;
	}

	public virtual TBlock[] Blocks => InternalCatalog.Blocks;

	public virtual TBlock Get(string id) => InternalCatalog.Get(id);
}

public abstract class ApplicationBlockCatalogDecorator<TBlock> : ApplicationBlockCatalogDecorator<TBlock, IApplicationBlockCatalog<TBlock>>
	where TBlock : class, IApplicationBlock {
	protected ApplicationBlockCatalogDecorator(IApplicationBlockCatalog<TBlock> catalog)
		: base(catalog) {
	}
}
