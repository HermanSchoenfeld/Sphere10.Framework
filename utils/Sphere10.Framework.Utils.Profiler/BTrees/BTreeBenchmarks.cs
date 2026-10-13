// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using BenchmarkDotNet.Attributes;

namespace Sphere10.Framework.Utils.Profiler.BTrees;

public abstract class BTreeBenchmarkBase {
	protected BTreeBenchmarkFixture Fixture;

	[ParamsSource(nameof(Profiles))]
	public string Profile { get; set; }

	public virtual string[] Profiles => BTreeProfilerSettings.Current.GetProfileNames();

	[GlobalSetup]
	public void GlobalSetup() {
		Fixture = new BTreeBenchmarkFixture(Profile);
		PrepareGlobal();
	}

	[GlobalCleanup]
	public void GlobalCleanup() {
		using (Fixture)
			VerifyGlobal();
	}

	protected virtual void PrepareGlobal() {
	}

	protected virtual void VerifyGlobal() {
	}
}

public class BTreeReadBenchmarks : BTreeBenchmarkBase {
	private string _workload;
	private long _result;

	[Benchmark]
	public long WarmLookup() {
		_workload = nameof(WarmLookup);
		return _result = Fixture.WarmLookup();
	}

	[Benchmark]
	public int MissingLookup() {
		_workload = nameof(MissingLookup);
		return (int)(_result = Fixture.MissingLookup());
	}

	[Benchmark]
	public long Scan() {
		_workload = nameof(Scan);
		return _result = Fixture.Scan();
	}

	[Benchmark]
	public long RangeLookup() {
		_workload = nameof(RangeLookup);
		return _result = Fixture.RangeLookup();
	}

	protected override void PrepareGlobal() => Fixture.PrepareBaseline(true);

	protected override void VerifyGlobal() {
		Fixture.VerifyRead(_workload, _result);
		Fixture.Verify();
	}
}

public class BTreeLookupReadBenchmarks : BTreeBenchmarkBase {
	private long _result;
	private bool _scan;

	public override string[] Profiles => BTreeProfilerSettings.Current.GetProfileNames(lookup: true);

	[Benchmark]
	public long GroupLookup() {
		_scan = false;
		return _result = Fixture.GroupLookup();
	}

	[Benchmark]
	public long Scan() {
		_scan = true;
		return _result = Fixture.Scan();
	}

	protected override void PrepareGlobal() => Fixture.PrepareBaseline();

	protected override void VerifyGlobal() {
		Fixture.VerifyRead(_scan ? nameof(BTreeReadBenchmarks.Scan) : nameof(GroupLookup), _result);
		Fixture.Verify();
	}
}

public class BTreeMutationBenchmarks : BTreeBenchmarkBase {
	[IterationSetup]
	public void PrepareIteration() => Fixture.PrepareBaseline();

	[IterationCleanup]
	public void VerifyIteration() => Fixture.Verify();

	[Benchmark]
	public long Update() => Fixture.Update();

	[Benchmark]
	public long DeleteAndRecycle() => Fixture.DeleteAndRecycle();

	[Benchmark]
	public long MixedAndPersist() => Fixture.MixedAndPersist();
}

public class BTreeLookupMutationBenchmarks : BTreeMutationBenchmarks {
	public override string[] Profiles => BTreeProfilerSettings.Current.GetProfileNames(lookup: true);
}

public class BTreeInsertBenchmarks : BTreeBenchmarkBase {
	public override string[] Profiles => System.Array.ConvertAll(BTreeProfilerSettings.Current.Profiles, profile => profile.Name);

	[IterationSetup]
	public void PrepareIteration() => Fixture.ResetEmpty();

	[IterationCleanup]
	public void VerifyIteration() => Fixture.Verify();

	[Benchmark]
	public long InsertAndPersist() {
		var count = Fixture.Insert();
		Fixture.Persist();
		return count;
	}
}

public class BTreeReopenBenchmarks : BTreeBenchmarkBase {
	private long _result;

	public override string[] Profiles => BTreeProfilerSettings.Current.GetProfileNames(files: true);

	[Benchmark]
	public long ReopenAndLookup() => _result = Fixture.ReopenAndLookup();

	protected override void PrepareGlobal() => Fixture.PrepareBaseline();

	protected override void VerifyGlobal() {
		Fixture.VerifyRead(nameof(BTreeReadBenchmarks.WarmLookup), _result);
		Fixture.Verify();
	}
}

public class BTreeTransactionBenchmarks : BTreeBenchmarkBase {
	public override string[] Profiles => BTreeProfilerSettings.Current.GetProfileNames(transactional: true);

	[IterationSetup]
	public void PrepareIteration() {
		Fixture.PrepareBaseline();
		Fixture.Update();
	}

	[IterationCleanup]
	public void VerifyIteration() => Fixture.Verify();

	[Benchmark]
	public void CommitPreparedChanges() => Fixture.Persist();

	[Benchmark]
	public void RollbackPreparedChanges() => Fixture.RollbackPreparedChanges();
}

public class BTreeLookupTransactionBenchmarks : BTreeTransactionBenchmarks {
	public override string[] Profiles => BTreeProfilerSettings.Current.GetProfileNames(lookup: true, transactional: true);
}
