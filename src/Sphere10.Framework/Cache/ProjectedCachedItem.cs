// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework;

/// <summary>A converted value view over an existing cached entry, retaining the source entry's live metadata.</summary>
public sealed class ProjectedCachedItem<TFrom, TTo> : CachedItem<TTo> {

	private readonly CachedItem<TFrom> _source;
	private readonly Func<TFrom, TTo> _projection;
	private readonly Func<TTo, TFrom> _inverseProjection;

	public ProjectedCachedItem(CachedItem<TFrom> source, Func<TFrom, TTo> projection, Func<TTo, TFrom> inverseProjection) {
		Guard.ArgumentNotNull(source, nameof(source));
		Guard.ArgumentNotNull(projection, nameof(projection));
		Guard.ArgumentNotNull(inverseProjection, nameof(inverseProjection));
		_source = source;
		_projection = projection;
		_inverseProjection = inverseProjection;
	}

	public override TTo Value {
		get => _projection(_source.Value);
		internal set => _source.Value = _inverseProjection(value);
	}

	public override CachedItemTraits Traits {
		get => _source.Traits;
		internal set => _source.Traits = value;
	}

	public override DateTime FetchedOn {
		get => _source.FetchedOn;
		internal set => _source.FetchedOn = value;
	}

	public override DateTime LastAccessedOn {
		get => _source.LastAccessedOn;
		internal set => _source.LastAccessedOn = value;
	}

	public override uint AccessedCount {
		get => _source.AccessedCount;
		internal set => _source.AccessedCount = value;
	}

	public override long Size {
		get => _source.Size;
		internal set => _source.Size = value;
	}

	internal override long InsertionOrder {
		get => _source.InsertionOrder;
		set => _source.InsertionOrder = value;
	}

	internal override long AccessOrder {
		get => _source.AccessOrder;
		set => _source.AccessOrder = value;
	}

	/// <summary>Explicit entry disposal delegates to the source value, rather than disposing a converted temporary value.</summary>
	public override void Dispose() => _source.Dispose();
}
