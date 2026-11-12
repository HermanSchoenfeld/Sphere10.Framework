// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework;

/// <summary>A data source which fetches its data on demand. Until it is loaded, <see cref="ILoadable.RequiresLoad"/> is true and reads load it first.</summary>
public interface ILoadableDataSource<TItem> : IDataSource<TItem>, ILoadable {
}
