// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Linq;
using BenchmarkDotNet.Attributes;

namespace Sphere10.Framework.Utils.Profiler.ObjectSpaces;

public class ObjectSpaceMerkleBenchmarks : ObjectSpaceBenchmarkBase {
	private int _flushes;

	public override string[] Profiles => ProfilerSettings.Current.Profiles
		.Where(profile => profile.Configuration.Merkleized).Select(profile => profile.Name).ToArray();

	[Benchmark]
	public int CleanFlush() => _flushes = Fixture.CleanFlush();

	protected override void PrepareGlobal() => Fixture.PrepareCleanMerkle();

	protected override void VerifyGlobal() => Fixture.VerifyCleanMerkleResult(_flushes);
}
