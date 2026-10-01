// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Collections.Generic;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Web.AspNetCore.Blazor;

/// <summary>Blazor platform facade over the portable snapshot catalog.</summary>
public class BlazorApplicationBlockCatalog : BlazorApplicationBlockCatalogBase {
	private readonly ApplicationBlockCatalog<IBlazorApplicationBlock> _catalog;

	public BlazorApplicationBlockCatalog(IEnumerable<IBlazorApplicationBlock> blocks) {
		_catalog = new ApplicationBlockCatalog<IBlazorApplicationBlock>(blocks, BlazorApplicationBlockSnapshot.Create,
			block => BlazorApplicationBlockSnapshot.GetDefaultScreen(block), BlazorApplicationBlockSnapshot.ValidateScreenType);
	}

	public override IBlazorApplicationBlock[] Blocks => _catalog.Blocks;

	public override IBlazorApplicationBlock Get(string id) => _catalog.Get(id);
}

/// <summary>Retains explicit implementations of the Blazor catalog members.</summary>
public interface IBlazorApplicationBlockCatalog : IApplicationBlockCatalog<IBlazorApplicationBlock> {
	new IBlazorApplicationBlock[] Blocks { get; }

	new IBlazorApplicationBlock Get(string id);

	IBlazorApplicationBlock[] IApplicationBlockCatalog<IBlazorApplicationBlock>.Blocks => Blocks;

	IBlazorApplicationBlock IApplicationBlockCatalog<IBlazorApplicationBlock>.Get(string id) => Get(id);
}

public abstract class BlazorApplicationBlockCatalogBase : ApplicationBlockCatalogBase<IBlazorApplicationBlock>, IBlazorApplicationBlockCatalog {
}
