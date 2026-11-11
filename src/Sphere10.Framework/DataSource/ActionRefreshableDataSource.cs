// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework;

/// <summary>A refreshable data source whose fresh sources a caller-supplied factory creates, for clients which compose a source without deriving one.</summary>
public class ActionRefreshableDataSource<TItem, TConcrete> : RefreshableDataSourceBase<TItem, TConcrete>
	where TConcrete : IDataSource<TItem> {
	private readonly Func<TConcrete> _createDataSource;

	/// <summary>Creates each fresh source with the factory; the first one is served, unloaded, until the first refresh.</summary>
	public ActionRefreshableDataSource(Func<TConcrete> createDataSource)
		: base(CreateInitial(createDataSource)) {
		_createDataSource = createDataSource;
	}

	/// <summary>Serves the initial source as published data (loaded or not) and creates each fresh source with the factory.</summary>
	public ActionRefreshableDataSource(TConcrete initial, Func<TConcrete> createDataSource)
		: base(initial, true) {
		Guard.ArgumentNotNull(createDataSource, nameof(createDataSource));
		_createDataSource = createDataSource;
	}

	protected override TConcrete CreateDataSource() => _createDataSource();

	private static TConcrete CreateInitial(Func<TConcrete> createDataSource) {
		Guard.ArgumentNotNull(createDataSource, nameof(createDataSource));
		var initial = createDataSource();
		Guard.Ensure(initial != null, "A fresh data source must be created.");
		return initial;
	}
}


/// <summary>Interface-typed convenience form of <see cref="ActionRefreshableDataSource{TItem, TConcrete}"/>.</summary>
public class ActionRefreshableDataSource<TItem> : ActionRefreshableDataSource<TItem, IDataSource<TItem>> {
	public ActionRefreshableDataSource(Func<IDataSource<TItem>> createDataSource)
		: base(createDataSource) {
	}

	public ActionRefreshableDataSource(IDataSource<TItem> initial, Func<IDataSource<TItem>> createDataSource)
		: base(initial, createDataSource) {
	}
}
