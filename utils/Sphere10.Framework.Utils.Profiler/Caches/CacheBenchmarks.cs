// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using BenchmarkDotNet.Attributes;

namespace Sphere10.Framework.Utils.Profiler.Caches;

public abstract class CacheBenchmarkBase {
	protected CacheBenchmarkFixture Fixture;

	[ParamsSource(nameof(Profiles))]
	public string Profile { get; set; }

	public string[] Profiles => CacheProfilerSettings.Current.GetProfileNames();

	[GlobalSetup]
	public void GlobalSetup() => Fixture = new CacheBenchmarkFixture(Profile);

	[GlobalCleanup]
	public void GlobalCleanup() => Fixture.Dispose();

	[IterationSetup]
	public void PrepareIteration() => Fixture.PrepareIteration();

	[IterationCleanup]
	public void VerifyIteration() => Fixture.WriteMetrics(Fixture.Verify());
}

public class CacheAccessBenchmarks : CacheBenchmarkBase {
	[Benchmark(Baseline = true)]
	public long TypedHotKeys() => Fixture.TypedHotKeys();

	[Benchmark]
	public long ObjectHotKeys() => Fixture.ObjectHotKeys();

	[Benchmark]
	public long ObjectPreboxedHotKeys() => Fixture.ObjectPreboxedHotKeys();
}

public class CachePressureBenchmarks : CacheBenchmarkBase {
	[Benchmark]
	public long SequentialPressure() => Fixture.SequentialPressure();

	[Benchmark]
	public long HotSetPressure() => Fixture.HotSetPressure();
}
