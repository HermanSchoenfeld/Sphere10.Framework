// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld <herman@sphere10.com>
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.VisualRenderer;

internal sealed class VisualTraversal {

	private readonly HashSet<VisualNode> _active = new(ReferenceEqualityComparer.Instance);

	public List<VisualNode> Ancestors { get; } = new();

	public void Enter(VisualNode node) {
		ArgumentNullException.ThrowIfNull(node);
		if (Ancestors.Count >= 256)
			throw new InvalidOperationException("Visual content exceeds the maximum depth of 256.");
		if (!_active.Add(node))
			throw new InvalidOperationException("Visual content contains a recursive containment cycle.");
		Ancestors.Add(node);
	}

	public void Leave(VisualNode node) {
		Ancestors.RemoveAt(Ancestors.Count - 1);
		_active.Remove(node);
	}
}
