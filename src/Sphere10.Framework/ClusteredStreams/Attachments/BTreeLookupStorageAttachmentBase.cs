// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Collections.Generic;

namespace Sphere10.Framework;

/// <summary>
/// A single-stream <see cref="ClusteredStreamsAttachmentBase"/> that hosts a <see cref="StreamMappedBTreeLookup{TKey,TValue}"/>
/// on its reserved stream. This is the ILookup analog of <see cref="BTreeStorageAttachmentBase{TKey,TValue}"/>.
/// </summary>
public abstract class BTreeLookupStorageAttachmentBase<TKey, TValue> : ClusteredStreamsAttachmentBase {
	private readonly int _order;
	private readonly IItemSerializer<TKey> _keySerializer;
	private readonly IItemSerializer<TValue> _valueSerializer;
	private readonly IComparer<TKey> _keyComparer;
	private StreamMappedBTreeLookup<TKey, TValue> _lookup;

	public BTreeLookupStorageAttachmentBase(
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

	protected StreamMappedBTreeLookup<TKey, TValue> BTreeLookup {
		get {
			CheckAttached();
			return _lookup;
		}
	}

	protected override void AttachInternal() {
		_lookup = new StreamMappedBTreeLookup<TKey, TValue>(
			_order,
			AttachmentStream,
			_keySerializer,
			_valueSerializer,
			_keyComparer
		);
	}

	protected override void DetachInternal() {
		_lookup?.Dispose();
		_lookup = null;
	}
}
