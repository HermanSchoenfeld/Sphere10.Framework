// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework;

/// <summary>One contextual call-path node. Recursion is represented by distinct parent/depth nodes, not collapsed by frame value.</summary>
public sealed record SampledCallTreeNode<TFrame>(int Id, int? ParentId, int Depth, TFrame Frame, long InclusiveSamples, long ExclusiveSamples,
	double InclusivePercent, double ExclusivePercent);
