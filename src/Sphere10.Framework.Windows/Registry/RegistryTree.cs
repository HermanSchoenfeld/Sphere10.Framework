// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Windows;

/// <summary>A caller-owned registry tree whose values and child nodes may be supplied by cached futures.</summary>
public sealed record RegistryTree {
	public RegistryTree(RegistryValue[] values, RegistrySubTree[] subKeys)
		: this(Tools.Values.Future.Explicit(values), Tools.Values.Future.Explicit(subKeys)) {
		Guard.ArgumentNotNull(values, nameof(values));
		Guard.ArgumentNotNull(subKeys, nameof(subKeys));
	}

	public RegistryTree(IFuture<RegistryValue[]> values, IFuture<RegistrySubTree[]> subKeys) {
		Guard.ArgumentNotNull(values, nameof(values));
		Guard.ArgumentNotNull(subKeys, nameof(subKeys));
		ValuesFuture = values;
		SubKeysFuture = subKeys;
	}

	public IFuture<RegistryValue[]> ValuesFuture { get; }
	public IFuture<RegistrySubTree[]> SubKeysFuture { get; }
	public RegistryValue[] Values => ValuesFuture.Value;
	public RegistrySubTree[] SubKeys => SubKeysFuture.Value;

	public override string ToString() => nameof(RegistryTree);
}
