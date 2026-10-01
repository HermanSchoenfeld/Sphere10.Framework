// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Collections.Generic;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Logic;

public abstract class ApplicationBlockCatalogDecorator<TConcrete> : IApplicationBlockCatalog where TConcrete : IApplicationBlockCatalog {
	protected readonly TConcrete InternalCatalog;

	protected ApplicationBlockCatalogDecorator(TConcrete catalog) {
		Guard.ArgumentNotNull(catalog, nameof(catalog));
		InternalCatalog = catalog;
	}

	public virtual IReadOnlyList<IApplicationBlock> Blocks => InternalCatalog.Blocks;

	public virtual IApplicationBlock Get(string id) => InternalCatalog.Get(id);
}

public abstract class ApplicationBlockCatalogDecorator : ApplicationBlockCatalogDecorator<IApplicationBlockCatalog> {
	protected ApplicationBlockCatalogDecorator(IApplicationBlockCatalog catalog)
		: base(catalog) {
	}
}

