// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Linq;
using NUnit.Framework;

namespace Sphere10.Framework.Tests;

[TestFixture]
public class SampledCallTreeTests {
	[Test]
	public void InclusiveAndExclusiveCountsUseContextualPathsAndAllInputSamplesAsDenominator() {
		var tree = SampledCallTree<string>.Build(new[] { new[] { "Process", "A", "B" }, new[] { "Process", "A", "C" }, new[] { "Process", "D" }, Array.Empty<string>() });
		Assert.That(tree.TotalSamples, Is.EqualTo(4));
		Assert.That(tree.CountedSamples, Is.EqualTo(3));
		Assert.That(tree.MissingStackSamples, Is.EqualTo(1));
		var a = tree.Nodes.Single(node => node.Frame == "A");
		Assert.That(a.InclusiveSamples, Is.EqualTo(2));
		Assert.That(a.ExclusiveSamples, Is.Zero);
		Assert.That(a.InclusivePercent, Is.EqualTo(50));
		Assert.That(tree.Nodes.Single(node => node.Frame == "B").ExclusiveSamples, Is.EqualTo(1));
		Assert.That(tree.Nodes.Where(node => node.ParentId == null).Sum(node => node.InclusiveSamples), Is.EqualTo(tree.CountedSamples));
	}

	[Test]
	public void RecursionRemainsAContextualChildAndComparerIsUsedAtEveryDepth() {
		var tree = SampledCallTree<string>.Build(new[] { new[] { "A", "A", "B" }, new[] { "a", "a", "b" } }, frameComparer: StringComparer.OrdinalIgnoreCase);
		Assert.That(tree.Nodes.Length, Is.EqualTo(3));
		Assert.That(tree.Nodes[1].ParentId, Is.EqualTo(tree.Nodes[0].Id));
		Assert.That(tree.Nodes[0].Frame, Is.EqualTo(tree.Nodes[1].Frame));
		Assert.That(tree.Nodes.All(node => node.InclusiveSamples == 2), Is.True);
		Assert.That(tree.Nodes[2].ExclusiveSamples, Is.EqualTo(2));
	}

	[Test]
	public void BoundsOmitEntireSamplesWithoutCreatingFalseExclusivePrefixesOrEmptyNodes() {
		var nodes = SampledCallTree<string>.Build(new[] { new[] { "A", "B" }, new[] { "A", "C" } }, maximumNodes: 2);
		Assert.That(nodes.Nodes.Select(node => node.Frame), Is.EqualTo(new[] { "A", "B" }));
		Assert.That(nodes.NodeLimitedSamples, Is.EqualTo(1));
		Assert.That(nodes.Nodes[0].InclusiveSamples, Is.EqualTo(1));
		Assert.That(nodes.Nodes[0].ExclusiveSamples, Is.Zero);
		var depth = SampledCallTree<string>.Build(new[] { new[] { "A", "B", "C" } }, maximumDepth: 2);
		Assert.That(depth.DepthLimitedSamples, Is.EqualTo(1));
		Assert.That(depth.Nodes, Is.Empty);
	}

	[Test]
	public void CallerCheckpointCancelsLongAggregationWithoutReturningPartialCounts() {
		var observations = 0;
		Assert.That(() => SampledCallTree<string>.Build(new[] { new[] { "A" }, new[] { "B" } }, checkpoint: () => {
			if (++observations == 2)
				throw new OperationCanceledException();
		}), Throws.InstanceOf<OperationCanceledException>());
		Assert.That(observations, Is.EqualTo(2));
	}
}
