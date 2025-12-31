// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using Sphere10.Framework.ObjectSpaces;

namespace Sphere10.Framework;

public interface IStreamMappedRecyclableList<TItem> : IRecyclableList<TItem>, IStreamMappedCollection<TItem>, ILoadable, IDisposable {

	IItemSerializer<TItem> ItemSerializer { get; }

	IEqualityComparer<TItem> ItemComparer { get; }

}

