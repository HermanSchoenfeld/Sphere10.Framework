// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using BenchmarkDotNet.Attributes;

namespace Sphere10.Framework.Utils.Profiler.ObjectSpaces;

public abstract class ObjectSpaceBenchmarkBase {
	protected ObjectSpaceBenchmarkFixture Fixture;

	[ParamsSource(nameof(Profiles))]
	public string Profile { get; set; }

	public virtual string[] Profiles => ProfilerSettings.Current.GetProfileNames();

	[GlobalSetup]
	public void GlobalSetup() {
		Fixture = new ObjectSpaceBenchmarkFixture(Profile);
		PrepareGlobal();
	}

	[GlobalCleanup]
	public void GlobalCleanup() {
		using (Fixture)
			if (Fixture != null)
				VerifyGlobal();
	}

	protected virtual void PrepareGlobal() {
	}

	protected virtual void VerifyGlobal() {
	}
}

public class ObjectSpaceWarmReadBenchmarks : ObjectSpaceBenchmarkBase {
	private decimal _result;
	private bool _scan;
	private int _misses;

	[Benchmark]
	public decimal WarmIdentityLookup() {
		_scan = false;
		return _result = Fixture.IdentityLookup();
	}

	[Benchmark]
	public decimal WarmPhysicalSlotLookup() {
		_scan = false;
		return _result = Fixture.PhysicalSlotLookup();
	}

	[Benchmark]
	public decimal WarmScan() {
		_scan = true;
		return _result = Fixture.Scan();
	}

	[Benchmark]
	public int MissingIdentityLookup() => _misses = Fixture.IdentityMisses();

	protected override void VerifyGlobal() {
		if (_misses > 0)
			Guard.Ensure(_misses == ProfilerSettings.Current.OperationCount, "Missing-key lookup returned an unexpected hit.");
		else
			Fixture.VerifyReadResult(_result, _scan);
	}

	protected override void PrepareGlobal() => Fixture.PrepareRead(true);
}

public class ObjectSpaceSecondaryIndexBenchmarks : ObjectSpaceBenchmarkBase {
	private decimal _balances;
	private long _categoryCount;
	private bool _categoryLookup;

	public override string[] Profiles => ProfilerSettings.Current.GetProfileNames(fullIndexesOnly: true);

	[Benchmark]
	public decimal WarmCodeLookup() {
		_categoryLookup = false;
		return _balances = Fixture.CodeLookup();
	}

	[Benchmark]
	public long WarmCategoryLookup() {
		_categoryLookup = true;
		return _categoryCount = Fixture.CategoryLookup();
	}

	protected override void VerifyGlobal() {
		if (_categoryLookup)
			Fixture.VerifyCategoryResult(_categoryCount);
		else
			Fixture.VerifyReadResult(_balances);
	}

	protected override void PrepareGlobal() => Fixture.PrepareRead(true);
}

public class ObjectSpaceFirstTouchBenchmarks : ObjectSpaceBenchmarkBase {
	private decimal _result;
	private bool _scan;
	private bool _executed;

	[IterationSetup]
	public void IterationSetup() {
		Fixture.PrepareRead(false);
		_executed = false;
	}

	[Benchmark]
	public decimal FirstTouchIdentityLookup() {
		_scan = false;
		_result = Fixture.IdentityLookup();
		_executed = true;
		return _result;
	}

	[Benchmark]
	public decimal FirstTouchScan() {
		_scan = true;
		_result = Fixture.Scan();
		_executed = true;
		return _result;
	}

	[IterationCleanup]
	public void IterationCleanup() {
		if (_executed)
			Fixture.VerifyReadResult(_result, _scan);
		Fixture.CloseStore();
	}
}

public class ObjectSpaceMutationBenchmarks : ObjectSpaceBenchmarkBase {
	private string _operation;
	private bool _executed;
	private decimal _mixedReadResult;

	[IterationSetup(Target = nameof(InsertAndPersist))]
	public void PrepareInsert() {
		Fixture.PrepareEmpty();
		_executed = false;
	}

	[IterationSetup(Targets = new[] { nameof(UpdateAndPersist), nameof(ReindexAndPersist), nameof(DeleteAndPersist), nameof(DeleteRecycleAndPersist) })]
	public void PrepareMutation() {
		Fixture.PrepareMutation();
		_executed = false;
	}

	[IterationSetup(Target = nameof(MixedReadWriteAndPersist))]
	public void PrepareMixedMutation() {
		Fixture.PrepareMixedMutation();
		_executed = false;
	}

	[IterationSetup(Target = nameof(ResizePayloadAndPersist))]
	public void PreparePayloadMutation() {
		Fixture.PreparePayloadMutation();
		_executed = false;
	}

	[Benchmark]
	public long InsertAndPersist() {
		_operation = "insert";
		var result = Fixture.InsertAndPersist();
		_executed = true;
		return result;
	}

	[Benchmark]
	public int UpdateAndPersist() {
		_operation = "update";
		var result = Fixture.UpdateAndPersist(false);
		_executed = true;
		return result;
	}

	[Benchmark]
	public int ReindexAndPersist() {
		_operation = "reindex";
		var result = Fixture.UpdateAndPersist(true);
		_executed = true;
		return result;
	}

	[Benchmark]
	public decimal MixedReadWriteAndPersist() {
		_operation = "mixed";
		_mixedReadResult = Fixture.MixedReadWriteAndPersist();
		_executed = true;
		return _mixedReadResult;
	}

	[Benchmark]
	public int ResizePayloadAndPersist() {
		_operation = "payload";
		var result = Fixture.ResizePayloadAndPersist();
		_executed = true;
		return result;
	}

	[Benchmark]
	public int DeleteAndPersist() {
		_operation = "delete";
		var result = Fixture.DeleteAndPersist();
		_executed = true;
		return result;
	}

	[Benchmark]
	public long DeleteRecycleAndPersist() {
		_operation = "recycle";
		var result = Fixture.DeleteRecycleAndPersist();
		_executed = true;
		return result;
	}

	[IterationCleanup]
	public void IterationCleanup() {
		if (_executed) {
			if (_operation == "mixed")
				Fixture.VerifyMixedReadResult(_mixedReadResult);
			if (_operation == "insert")
				Fixture.VerifyInserted();
			else
				Fixture.VerifyMutation(_operation);
		}
		Fixture.CloseStore();
	}
}

public class ObjectSpaceReopenBenchmarks : ObjectSpaceBenchmarkBase {
	private long _result;
	private bool _executed;

	[IterationSetup]
	public void IterationSetup() {
		Fixture.PrepareReopen();
		_executed = false;
	}

	[Benchmark]
	public long ReopenAndHydrate() {
		_result = Fixture.ReopenAndHydrate();
		_executed = true;
		return _result;
	}

	[IterationCleanup]
	public void IterationCleanup() {
		if (_executed) {
			Guard.Ensure(_result == ProfilerSettings.Current.RecordCount + Fixture.LookupIds[0], "Reopen returned incorrect metadata or an incorrect initial row.");
			Fixture.VerifyCardinality(ProfilerSettings.Current.RecordCount);
		}
		Fixture.CloseStore();
	}
}

public class ObjectSpaceTransactionBenchmarks : ObjectSpaceBenchmarkBase {
	private bool _committed;
	private bool _executed;

	public override string[] Profiles => ProfilerSettings.Current.GetProfileNames(fileOnly: true);

	[IterationSetup]
	public void IterationSetup() {
		Fixture.PrepareTransaction();
		_executed = false;
	}

	[Benchmark]
	public void CommitPreparedChanges() {
		Fixture.Commit();
		_committed = true;
		_executed = true;
	}

	[Benchmark]
	public void RollbackPreparedChanges() {
		Fixture.Rollback();
		_committed = false;
		_executed = true;
	}

	[IterationCleanup]
	public void IterationCleanup() {
		if (_executed)
			Fixture.VerifyTransaction(_committed);
		Fixture.CloseStore();
	}
}
