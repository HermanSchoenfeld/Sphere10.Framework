// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;

namespace Sphere10.Framework;

/// <summary>
/// A single-stream <see cref="ClusteredStreamsAttachmentBase"/> that hosts a configured stream-mapped B-tree
/// on its reserved stream.
/// </summary>
public abstract class BTreeStorageAttachmentBase<TKey, TValue> : ClusteredStreamsAttachmentBase {
	private readonly IItemSerializer<TKey> _keySerializer;
	private readonly IItemSerializer<TValue> _valueSerializer;
	private readonly IComparer<TKey> _keyComparer;
	private BTreeBase<TKey, TValue> _btree;

	public BTreeStorageAttachmentBase(
		ClusteredStreams streams,
		string attachmentID,
		int order,
		IItemSerializer<TKey> keySerializer,
		IItemSerializer<TValue> valueSerializer,
		IComparer<TKey> keyComparer
	) : this(streams, attachmentID, keySerializer, valueSerializer, keyComparer, new BTreeSpec(BTreeType.BTree, order)) {
	}

	public BTreeStorageAttachmentBase(
		ClusteredStreams streams,
		string attachmentID,
		IItemSerializer<TKey> keySerializer,
		IItemSerializer<TValue> valueSerializer,
		IComparer<TKey> keyComparer,
		BTreeSpec bTreeSpec
	) : base(streams, attachmentID) {
		Guard.ArgumentNotNull(bTreeSpec, nameof(bTreeSpec));
		Spec = bTreeSpec;
		Guard.ArgumentNotNull(keySerializer, nameof(keySerializer));
		Guard.ArgumentNotNull(valueSerializer, nameof(valueSerializer));
		Guard.ArgumentNotNull(keyComparer, nameof(keyComparer));
		_keySerializer = keySerializer;
		_valueSerializer = valueSerializer;
		_keyComparer = keyComparer;
	}

	public BTreeSpec Spec { get; }

	protected StreamMappedBTree<TKey, TValue> BTree => Tree as StreamMappedBTree<TKey, TValue>
		?? throw new InvalidOperationException("This attachment uses BTreePlus. Use its Tree property for implementation-independent access.");

	protected BTreeBase<TKey, TValue> Tree {
		get {
			CheckAttached();
			return _btree;
		}
	}

	protected override void AttachInternal() {
		var stream = AttachmentStream;
		_btree = Spec.Type == BTreeType.BTreePlus
			? new StreamMappedBTreePlus<TKey, TValue>(Spec.Order, stream, _keySerializer, _valueSerializer, _keyComparer)
			: new StreamMappedBTree<TKey, TValue>(Spec.Order, stream, _keySerializer, _valueSerializer, _keyComparer);
	}

	public override void VerifyIntegrity() {
	}

	protected override void DetachInternal() {
		(_btree as IDisposable)?.Dispose();
		_btree = null;
	}
}
