// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;

namespace Sphere10.Framework;

/// <summary>
/// A single-stream <see cref="ClusteredStreamsAttachmentBase"/> that hosts a <see cref="StreamMappedBTree{TKey,TValue}"/>
/// on its reserved stream.
/// </summary>
public class BTreeStorageAttachment<TKey, TValue> : ClusteredStreamsAttachmentBase {
	private readonly int _order;
	private readonly IItemSerializer<TKey> _keySerializer;
	private readonly IItemSerializer<TValue> _valueSerializer;
	private readonly IComparer<TKey> _keyComparer;
	private StreamMappedBTree<TKey, TValue> _btree;

	public BTreeStorageAttachment(
		ClusteredStreams streams,
		string attachmentID,
		int order,
		IItemSerializer<TKey> keySerializer,
		IItemSerializer<TValue> valueSerializer,
		IComparer<TKey> keyComparer
	) : base(streams, attachmentID) {
		Guard.ArgumentNotNull(keySerializer, nameof(keySerializer));
		Guard.ArgumentNotNull(valueSerializer, nameof(valueSerializer));
		Guard.ArgumentNotNull(keyComparer, nameof(keyComparer));
		_order = order;
		_keySerializer = keySerializer;
		_valueSerializer = valueSerializer;
		_keyComparer = keyComparer;
	}

	public StreamMappedBTree<TKey, TValue> BTree {
		get {
			CheckAttached();
			return _btree;
		}
	}

	protected override void AttachInternal() {
		_btree = new StreamMappedBTree<TKey, TValue>(
			_order,
			AttachmentStream,
			_keySerializer,
			_valueSerializer,
			_keyComparer
		);
	}

	protected override void VerifyIntegrity() {
	}

	protected override void DetachInternal() {
		_btree?.Dispose();
		_btree = null;
	}
}
