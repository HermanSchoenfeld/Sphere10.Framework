// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.


namespace Sphere10.Framework.Web.AspNetCore.Blazor;

public abstract class BlazorApplicationBlockCatalogDecorator<TConcrete> : IBlazorApplicationBlockCatalog where TConcrete : IBlazorApplicationBlockCatalog {
	protected readonly TConcrete InternalCatalog;

	protected BlazorApplicationBlockCatalogDecorator(TConcrete catalog) {
		Guard.ArgumentNotNull(catalog, nameof(catalog));
		InternalCatalog = catalog;
	}

	public virtual IBlazorApplicationBlock[] Blocks => InternalCatalog.Blocks;

	public virtual IBlazorApplicationBlock Get(string id) => InternalCatalog.Get(id);
}

public abstract class BlazorApplicationBlockCatalogDecorator : BlazorApplicationBlockCatalogDecorator<IBlazorApplicationBlockCatalog> {
	protected BlazorApplicationBlockCatalogDecorator(IBlazorApplicationBlockCatalog catalog)
		: base(catalog) {
	}
}

