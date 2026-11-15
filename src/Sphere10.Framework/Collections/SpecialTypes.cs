// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework;

/// <summary>A keyed item added, removed or modified between two sequences. Kind determines which values are present.</summary>
public sealed record CollectionDifference<T, TKey>(TKey Key, T Previous, T Current, CollectionDifferenceKind Kind);
