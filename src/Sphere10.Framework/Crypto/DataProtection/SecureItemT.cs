// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework;

public sealed class SecureItem<T> : ISecureItem<T> {
	private SecureBytes _secureBytes;
	private IItemSerializer<T> _serializer;

	public SecureItem(T item, IItemSerializer<T> serializer) {
		Guard.ArgumentNotNull(item, nameof(item));
		Guard.ArgumentNotNull(serializer, nameof(serializer));
		_serializer = serializer;
		_secureBytes = new SecureBytes(_serializer.SerializeBytesLE(item));
		_secureBytes.Encrypted += () => { Item = default; };
		_secureBytes.Decrypted += () => { Item = _serializer.DeserializeBytesLE(_secureBytes.Item); };
	}

	public bool Protected => _secureBytes.Protected;

	public T Item {
		get => Protected ? throw new InvalidOperationException("No open scope is present, bytes are encrypted") : field;
		private set => field = value;
	}

	public ActionScope EnterUnprotectedScope() => _secureBytes.EnterUnprotectedScope();

	public void Dispose() {
		_secureBytes.Dispose();
	}


}

