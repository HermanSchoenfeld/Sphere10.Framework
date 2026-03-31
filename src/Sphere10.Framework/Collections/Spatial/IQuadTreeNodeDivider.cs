// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework;

/// <summary>
/// Defines a strategy for dividing a <see cref="QuadTreeNode{TBounds}"/> into sub-nodes.
/// </summary>
public interface IQuadTreeNodeDivider<TBounds> where TBounds : struct, IIntegerRectangle<TBounds> {
	void CreateSubNodes(QuadTreeNode<TBounds> parentNode);

	/// <summary>
	/// Expand tree by creating a new root that contains the current root.
	/// </summary>
	QuadTreeNode<TBounds> CreateNewRoot(QuadTreeNode<TBounds> currentRoot);
}
