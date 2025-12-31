// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Collections.Generic;

namespace Sphere10.Framework;

public class NotPersistedDictionary<T1, T2> : DictionaryDecorator<T1, T2>, IPersistedDictionary<T1, T2> {

	public NotPersistedDictionary() : base(new Dictionary<T1, T2>()) {

	}
	public void Load() {
	}

	public void Save() {
	}

	public void Delete() {
	}
}

