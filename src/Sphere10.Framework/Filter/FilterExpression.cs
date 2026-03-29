// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework;

/// <summary>
/// Base class for a hierarchical, recursive filter expression tree.
/// A node is either a <see cref="FilterCondition"/> (leaf) or a <see cref="FilterGroup"/> (composite).
/// </summary>
public abstract class FilterExpression {
}
