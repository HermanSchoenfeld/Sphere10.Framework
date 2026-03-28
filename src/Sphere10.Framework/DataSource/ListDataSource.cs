// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Sphere10.Framework;

public class ListDataSource<TEntity> : FutureListDataSource<TEntity> {

	public ListDataSource(IExtendedList<TEntity> list) : base(Tools.Values.Future.Explicit(list)) {
	}
	
}
