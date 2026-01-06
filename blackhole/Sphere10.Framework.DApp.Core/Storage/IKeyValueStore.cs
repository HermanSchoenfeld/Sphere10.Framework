// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.IO;
using System.Linq;

namespace Sphere10.Framework.DApp.Core.Storage;

public interface IKeyValueStore<T> : ISynchronizedObject {
	IQueryable<T> GetKeys();

	Stream OpenRead(T key);

	Stream OpenWrite(T key);
}

