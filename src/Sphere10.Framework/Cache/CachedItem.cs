// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework;

/// <summary>Object-valued specialization of a typed cached entry.</summary>
public class CachedItem : CachedItem<object> {
}

/// <summary>Typed cached value and live expiration, access and reaping metadata.</summary>
public class CachedItem<T> : IDisposable {
	public virtual T Value { get; internal set; }

	public virtual CachedItemTraits Traits { get; internal set; } = CachedItemTraits.Default;

	public virtual DateTime FetchedOn { get; internal set; }

	public virtual DateTime LastAccessedOn { get; internal set; }

	public virtual uint AccessedCount { get; internal set; }

	public virtual long Size { get; internal set; }

	internal virtual long InsertionOrder { get; set; }

	internal virtual long AccessOrder { get; set; }

	/// <summary>Whether the cache may evict this entry under capacity pressure.</summary>
	public bool CanPurge {
		get => Traits.HasFlag(CachedItemTraits.CanPurge);
		set => Traits = Traits.CopyAndSetFlags(CachedItemTraits.CanPurge, value);
	}

	/// <summary>Explicitly disposes the value. Cache eviction itself does not own or dispose cached payloads.</summary>
	public virtual void Dispose() {
		if (Value is IDisposable disposable) {
			disposable.Dispose();
			// Preserve a disposable value type's state after its interface invocation boxes it.
			Value = (T)disposable;
		}
	}
}
