// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text.Json;
using Sphere10.Framework.ObjectSpaces;
using Sphere10.Framework.Utils.Profiler.ObjectSpaces.Lab;

namespace Sphere10.Framework.Utils.Profiler.ObjectSpaces;

public sealed class ObjectSpaceBenchmarkFixture : IDisposable {
	private static readonly Expression<Func<ObjectSpaceLabRecord, long>> IdentityMember = record => record.Id;
	private static readonly Expression<Func<ObjectSpaceLabRecord, string>> CodeMember = record => record.Code;
	private static readonly Expression<Func<ObjectSpaceLabRecord, int>> CategoryMember = record => record.Category;
	private readonly ProfilerSettings _settings;
	private readonly ObjectSpaceProfile _profile;
	private readonly string _workDirectory;
	private readonly string _baselinePath;
	private readonly string _workingPath;
	private readonly string _workingPages;
	private ActionScope<string> _directoryLifetime;
	private readonly Dictionary<long, long> _storageSlots = new();
	private byte[] _baselineBytes;
	private MemoryStream _memoryStream;
	private ObjectSpaceLabRecord[] _insertionRecords;
	private ObjectSpaceLabRecord[] _mutationRecords;
	private ObjectSpaceLabRecord[] _replacementRecords;
	private byte[][] _resizedPayloads;
	private byte[] _cleanMerkleRoot;
	private bool _disposed;

	public ObjectSpaceBenchmarkFixture(string profileName)
		: this(ProfilerSettings.Current, profileName) {
	}

	public ObjectSpaceBenchmarkFixture(ProfilerSettings settings, string profileName) {
		Guard.ArgumentNotNull(settings, nameof(settings));
		settings.Validate();
		_settings = settings;
		_profile = _settings.GetProfile(profileName);
		_workDirectory = Path.GetFullPath(Path.Combine(_settings.DataDirectory, Guid.NewGuid().ToString("N")));
		Guard.Ensure(_workDirectory.StartsWith(Path.TrimEndingDirectorySeparator(_settings.DataDirectory) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase), "Benchmark workspace must be inside its data root.");
		Directory.CreateDirectory(_workDirectory);
		_directoryLifetime = Tools.Scope.DeleteDirOnDispose(_workDirectory);
		_baselinePath = Path.Combine(_workDirectory, "baseline.store");
		_workingPath = Path.Combine(_workDirectory, "working.store");
		_workingPages = Path.Combine(_workDirectory, "working-pages");
		try {
			// Persist a deterministic fixture once, then restore outside every measured mutation/first-touch batch.
			using var baselineMemory = Configuration.Storage == ObjectSpaceLabStorage.Memory ? new MemoryStream() : null;
			using var baseline = Configuration.Storage == ObjectSpaceLabStorage.File
				? ObjectSpaceLabFactory.Open(Configuration, _baselinePath, Path.Combine(_workDirectory, "baseline-pages"))
				: ObjectSpaceLabFactory.Open(Configuration, memoryStream: baselineMemory);
			var records = CreateRecords();
			foreach (var record in records) {
				baseline.AcceptNew(record);
				_storageSlots.Add(record.Id, baseline.Save(record));
			}
			ObjectSpaceLabFactory.Complete(baseline);
			VerifyCardinality(baseline, _settings.RecordCount);
			VerifyRecord(baseline.Get(IdentityMember, 1), CreateRecord(1));
			VerifyRecord(baseline.Get(IdentityMember, (long)_settings.RecordCount), CreateRecord(_settings.RecordCount));
			_baselineBytes = baselineMemory?.ToArray();
			BaselineStorageBytes = _baselineBytes?.LongLength ?? new FileInfo(_baselinePath).Length;
			WriteStorageManifest();
		} catch {
			_directoryLifetime.Dispose();
			throw;
		}
		// Mutation targets remain distinct even when reads revisit a small hot set.
		MutationIds = Enumerable.Range(0, _settings.RecordCount).OrderBy(index => unchecked((uint)index * 2654435761U))
			.Take(_settings.OperationCount).Select(index => (long)index + 1).ToArray();
		LookupIds = CreateLookupIds();
		LookupCodes = LookupIds.Select(id => CreateRecord(id, false).Code).ToArray();
		LookupCategories = LookupIds.Select(GetCategory).ToArray();
		LookupSlots = LookupIds.Select(id => _storageSlots[id]).ToArray();
	}

	public ObjectSpace Store { get; private set; }

	public ObjectSpaceLabConfiguration Configuration => _profile.Configuration;

	public long[] LookupIds { get; }

	public long[] MutationIds { get; }

	public long[] LookupSlots { get; }

	public string[] LookupCodes { get; }

	public int[] LookupCategories { get; }

	public long BaselineStorageBytes { get; }

	public void PrepareRead(bool warm) {
		OpenBaseline();
		if (warm)
			Store.GetAll<ObjectSpaceLabRecord>().ToArray();
	}

	public void PrepareCleanMerkle() {
		Guard.Ensure(Configuration.Merkleized, "Clean Merkle flush requires a Merkleized profile.");
		PrepareRead(true);
		Store.Flush();
		VerifyMerkleIntegrity();
		_cleanMerkleRoot = ReadSpatialMerkleRoot();
	}

	/// <summary>Flushes an already settled store repeatedly; preparation and integrity verification belong outside measurement.</summary>
	public int CleanFlush() {
		for (var index = 0; index < _settings.OperationCount; index++)
			Store.Flush();
		return _settings.OperationCount;
	}

	public void VerifyCleanMerkleResult(int flushes) {
		Guard.Ensure(flushes == _settings.OperationCount, "Unexpected clean flush count.");
		VerifyMerkleIntegrity();
		Guard.Ensure(ByteArrayEqualityComparer.Instance.Equals(ReadSpatialMerkleRoot(), _cleanMerkleRoot), "A clean flush changed the settled spatial Merkle root.");
		ObjectSpaceLabFactory.Complete(Store);
		ReopenCurrent();
		VerifyMerkleIntegrity();
		Guard.Ensure(ByteArrayEqualityComparer.Instance.Equals(ReadSpatialMerkleRoot(), _cleanMerkleRoot), "A settled spatial Merkle root changed across reopen.");
		VerifyCardinality(_settings.RecordCount);
		VerifyRecord(Store.Get(IdentityMember, 1), CreateRecord(1));
		VerifyRecord(Store.Get(IdentityMember, (long)_settings.RecordCount), CreateRecord(_settings.RecordCount));
	}

	/// <summary>Verifies dimension and spatial roots on the owning thread, outside benchmark timing.</summary>
	public void VerifyMerkleIntegrity() {
		if (!Configuration.Merkleized)
			return;
		using var access = Store.EnterAccessScope();
		for (var index = 0; index < Store.Dimensions.Count; index++) {
			var streams = Store.Dimensions[index].Container.ObjectStream.Streams;
			using var dimensionAccess = streams.EnterAccessScope();
			streams.Attachments[Sphere10FrameworkDefaults.DefaultMerkleTreeIndexName].VerifyIntegrity();
		}
		Store.Streams.Attachments[Sphere10FrameworkDefaults.DefaultSpatialMerkleTreeIndexName].VerifyIntegrity();
	}

	public void PrepareEmpty() {
		CloseStore();
		DeleteWorkingFiles();
		Store = OpenWorking();
		_insertionRecords = CreateRecords();
		VerifyCardinality(Store, 0);
	}

	public void PrepareMutation() {
		OpenBaseline();
		_mutationRecords = MutationIds.Select(id => Store.Get(IdentityMember, id)).ToArray();
		_replacementRecords = MutationIds.Select(id => CreateRecord(_settings.RecordCount + id)).ToArray();
	}

	public void PrepareMixedMutation() {
		PrepareMutation();
		foreach (var id in LookupIds)
			Store.Get(IdentityMember, id);
	}

	public void PreparePayloadMutation() {
		PrepareMutation();
		_resizedPayloads = _mutationRecords.Select((record, index) => CreateResizedPayload(record.Id, record.Payload.Length, index)).ToArray();
	}

	public void PrepareTransaction() {
		Guard.Ensure(Configuration.Storage == ObjectSpaceLabStorage.File, "Transactions require file storage.");
		PrepareMutation();
		Update(false);
		Store.Flush();
	}

	public void PrepareReopen() {
		OpenBaseline();
		CloseStore();
		if (Configuration.Storage == ObjectSpaceLabStorage.Memory) {
			_memoryStream = new MemoryStream(_baselineBytes.Length);
			_memoryStream.Write(_baselineBytes);
			_memoryStream.Position = 0;
		}
	}

	public long InsertAndPersist() {
		foreach (var record in _insertionRecords) {
			Store.AcceptNew(record);
			Store.Save(record);
		}
		ObjectSpaceLabFactory.Complete(Store);
		return Store.Count<ObjectSpaceLabRecord>();
	}

	public decimal IdentityLookup() {
		var balance = 0M;
		foreach (var id in LookupIds)
			balance += Store.Get(IdentityMember, id).Balance;
		return balance;
	}

	public decimal PhysicalSlotLookup() {
		var balance = 0M;
		foreach (var slot in LookupSlots)
			balance += Store.Get<ObjectSpaceLabRecord>(slot).Balance;
		return balance;
	}

	public int IdentityMisses() {
		var misses = 0;
		foreach (var id in LookupIds)
			if (!Store.TryGet(IdentityMember, _settings.RecordCount + id, out _))
				misses++;
		return misses;
	}

	public decimal CodeLookup() {
		var balance = 0M;
		foreach (var code in LookupCodes)
			balance += Store.Get(CodeMember, code).Balance;
		return balance;
	}

	public long CategoryLookup() {
		using var access = Store.EnterAccessScope();
		var dimension = (ObjectSpace.Dimension<ObjectSpaceLabRecord>)Store.Dimensions.Single(dimension => dimension.Definition.ObjectType == typeof(ObjectSpaceLabRecord));
		var index = dimension.Container.ObjectStream.GetIndexFor(CategoryMember);
		var count = 0L;
		foreach (var category in LookupCategories)
			foreach (var slot in index.Values[category])
				count += Store.Get<ObjectSpaceLabRecord>(slot).Id > 0 ? 1 : 0;
		return count;
	}

	public decimal Scan() {
		var balance = 0M;
		foreach (var record in Store.GetAll<ObjectSpaceLabRecord>())
			balance += record.Balance;
		return balance;
	}

	public int UpdateAndPersist(bool reindex) {
		Update(reindex);
		ObjectSpaceLabFactory.Complete(Store);
		return _mutationRecords.Length;
	}

	public decimal MixedReadWriteAndPersist() {
		var balance = 0M;
		var writeOrdinal = 0;
		for (var index = 0; index < _mutationRecords.Length; index++) {
			if (!IsMixedWrite(index)) {
				balance += Store.Get(IdentityMember, LookupIds[index]).Balance;
				continue;
			}
			UpdateRecord(_mutationRecords[index], writeOrdinal++ % 2 == 0);
		}
		ObjectSpaceLabFactory.Complete(Store);
		return balance;
	}

	public int ResizePayloadAndPersist() {
		for (var index = 0; index < _mutationRecords.Length; index++) {
			var record = _mutationRecords[index];
			record.Payload = _resizedPayloads[index];
			if (!Configuration.AutoSave)
				Store.Save(record);
		}
		ObjectSpaceLabFactory.Complete(Store);
		return _mutationRecords.Length;
	}

	public int DeleteAndPersist() {
		foreach (var record in _mutationRecords)
			Store.Delete(record);
		ObjectSpaceLabFactory.Complete(Store);
		return _mutationRecords.Length;
	}

	public long DeleteRecycleAndPersist() {
		foreach (var record in _mutationRecords)
			Store.Delete(record);
		foreach (var replacement in _replacementRecords) {
			Store.AcceptNew(replacement);
			Store.Save(replacement);
		}
		ObjectSpaceLabFactory.Complete(Store);
		return Store.Count<ObjectSpaceLabRecord>();
	}

	public long ReopenAndHydrate() {
		Store = OpenWorking();
		var first = Store.Get(IdentityMember, LookupIds[0]);
		return Store.Count<ObjectSpaceLabRecord>() + first.Id;
	}

	public void Commit() => ((FileObjectSpace)Store).Commit();

	public void Rollback() => ((FileObjectSpace)Store).Rollback();

	public void VerifyCardinality(int expected) => VerifyCardinality(Store, expected);

	public void VerifyTransaction(bool committed) {
		// Reacquire after rollback: the old tracked records belong to the discarded view.
		ReopenCurrent();
		foreach (var id in MutationIds) {
			var expected = CreateRecord(id);
			if (committed)
				expected.Balance += 1M;
			VerifyRecord(Store.Get(IdentityMember, id), expected);
		}
		VerifyCardinality(_settings.RecordCount);
	}

	public void VerifyMutation(string operation) {
		ReopenCurrent();
		VerifyCardinality(operation == "delete" ? _settings.RecordCount - MutationIds.Length : _settings.RecordCount);
		var writeOrdinal = 0;
		for (var index = 0; index < MutationIds.Length; index++) {
			var id = MutationIds[index];
			if (operation is "delete" or "recycle") {
				Guard.Ensure(!Store.TryGet(IdentityMember, id, out _), "A deleted identity remains in the persisted index.");
				if (operation == "recycle")
					VerifyRecord(Store.Get(IdentityMember, _settings.RecordCount + id), CreateRecord(_settings.RecordCount + id));
				continue;
			}
			var expected = CreateRecord(id);
			var updated = operation != "payload" && (operation != "mixed" || IsMixedWrite(index));
			if (operation == "payload")
				expected.Payload = CreateResizedPayload(id, expected.Payload.Length, index);
			var reindexed = operation == "reindex" || operation == "mixed" && updated && writeOrdinal++ % 2 == 0;
			if (updated)
				expected.Balance += 1M;
			if (reindexed) {
				expected.Code += "-updated";
				expected.Category += 1000000;
			}
			var actual = Store.Get(IdentityMember, id);
			VerifyRecord(actual, expected);
			if (Configuration.IndexProfile == ObjectSpaceLabIndexProfile.Full) {
				Guard.Ensure(ReferenceEquals(Store.Get(CodeMember, expected.Code), actual), "An updated code index failed to resolve the persisted record.");
				if (reindexed) {
					Guard.Ensure(!Store.TryGet(CodeMember, CreateRecord(id, false).Code, out _), "An obsolete code remains in the persisted index.");
					using var access = Store.EnterAccessScope();
					var dimension = (ObjectSpace.Dimension<ObjectSpaceLabRecord>)Store.Dimensions.Single(dimension => dimension.Definition.ObjectType == typeof(ObjectSpaceLabRecord));
					var categories = dimension.Container.ObjectStream.GetIndexFor(CategoryMember);
					Guard.Ensure(categories.Values[expected.Category].Any(slot => Store.Get<ObjectSpaceLabRecord>(slot).Id == id), "The updated category is absent from the persisted nonunique index.");
					Guard.Ensure(!categories.Values[GetCategory(id)].Any(slot => Store.Get<ObjectSpaceLabRecord>(slot).Id == id), "An obsolete category remains in the persisted nonunique index.");
				}
			}
		}
	}

	public void VerifyInserted() {
		ReopenCurrent();
		VerifyCardinality(_settings.RecordCount);
		VerifyRecord(Store.Get(IdentityMember, 1), CreateRecord(1));
		VerifyRecord(Store.Get(IdentityMember, (long)_settings.RecordCount), CreateRecord(_settings.RecordCount));
	}

	public void VerifyReadResult(decimal actual, bool scan = false) {
		var ids = scan ? Enumerable.Range(1, _settings.RecordCount).Select(id => (long)id) : LookupIds;
		var expected = ids.Sum(id => CreateRecord(id, false).Balance);
		Guard.Ensure(actual == expected, "Read benchmark returned an incorrect balance checksum.");
	}

	public void VerifyCategoryResult(long actual) {
		var counts = Enumerable.Range(1, _settings.RecordCount).Select(id => GetCategory(id)).GroupBy(category => category).ToDictionary(group => group.Key, group => group.LongCount());
		Guard.Ensure(actual == LookupCategories.Sum(category => counts[category]), "Category lookup returned an incorrect result count.");
	}

	public void VerifyMixedReadResult(decimal actual) {
		var changed = new HashSet<long>();
		var expected = 0M;
		for (var index = 0; index < MutationIds.Length; index++) {
			if (IsMixedWrite(index))
				changed.Add(MutationIds[index]);
			else
				expected += LookupIds[index] * 1.25M + (changed.Contains(LookupIds[index]) ? 1M : 0M);
		}
		Guard.Ensure(actual == expected, "Mixed workload returned an incorrect read checksum.");
	}

	public void CloseStore() {
		var store = Store;
		var memory = _memoryStream;
		Store = null;
		_memoryStream = null;
		using (memory)
			store?.Dispose();
	}

	public void Dispose() {
		if (_disposed)
			return;
		_disposed = true;
		try {
			CloseStore();
		} finally {
			_directoryLifetime.Dispose();
		}
	}

	private void OpenBaseline() {
		CloseStore();
		DeleteWorkingFiles();
		if (Configuration.Storage == ObjectSpaceLabStorage.File) {
			File.Copy(_baselinePath, _workingPath, true);
			File.Copy(_baselinePath + ".objectspace.json", _workingPath + ".objectspace.json", true);
		} else {
			_memoryStream = new MemoryStream(_baselineBytes.Length);
			_memoryStream.Write(_baselineBytes);
			_memoryStream.Position = 0;
		}
		Store = OpenWorking();
		VerifyCardinality(_settings.RecordCount);
	}

	private void ReopenCurrent() {
		var bytes = _memoryStream?.ToArray();
		CloseStore();
		if (bytes != null) {
			_memoryStream = new MemoryStream(bytes.Length);
			_memoryStream.Write(bytes);
			_memoryStream.Position = 0;
		}
		Store = OpenWorking();
	}

	private byte[] ReadSpatialMerkleRoot() {
		using var access = Store.EnterAccessScope();
		var digestSize = Hashers.GetDigestSizeBytes(Configuration.HashFunction);
		return Store.Streams.Header.MapExtensionProperty(0, sizeof(bool) + digestSize,
			new ConstantSizeNullableByteArraySerializer(digestSize)).Value;
	}

	private ObjectSpace OpenWorking() {
		if (Configuration.Storage == ObjectSpaceLabStorage.Memory)
			_memoryStream ??= new MemoryStream();
		return Configuration.Storage == ObjectSpaceLabStorage.File
			? ObjectSpaceLabFactory.Open(Configuration, _workingPath, _workingPages)
			: ObjectSpaceLabFactory.Open(Configuration, memoryStream: _memoryStream);
	}

	private ObjectSpaceLabRecord[] CreateRecords() {
		var records = Enumerable.Range(1, _settings.RecordCount).Select(id => CreateRecord(id));
		if (_settings.ShuffledInsertion)
			records = records.OrderBy(record => unchecked((uint)record.Id * 2654435761U));
		return records.ToArray();
	}

	private void Update(bool reindex) {
		foreach (var record in _mutationRecords)
			UpdateRecord(record, reindex);
	}

	private void UpdateRecord(ObjectSpaceLabRecord record, bool reindex) {
		record.Balance += 1M;
		if (reindex) {
			record.Code += "-updated";
			record.Category += 1000000;
		}
		if (!Configuration.AutoSave)
			Store.Save(record);
	}

	private bool IsMixedWrite(int index) => index * 37 % 100 >= _settings.MixedReadPercentage;

	private long[] CreateLookupIds() {
		if (_settings.LookupPattern == ObjectSpaceLookupPattern.Sequential)
			return Enumerable.Range(1, _settings.OperationCount).Select(index => (long)index).ToArray();
		if (_settings.LookupPattern == ObjectSpaceLookupPattern.Shuffled)
			return MutationIds.ToArray();
		var hotCount = Math.Max(1, _settings.RecordCount * _settings.HotSetPercent / 100);
		var hotIds = Enumerable.Range(0, hotCount).OrderBy(index => unchecked((uint)index * 2654435761U)).Select(index => (long)index + 1).ToArray();
		return Enumerable.Range(0, _settings.OperationCount).Select(index => hotIds[index % hotIds.Length]).ToArray();
	}

	private ObjectSpaceLabRecord CreateRecord(long id, bool includePayload = true) {
		var payloadBytes = includePayload ? _settings.PayloadBytes : 0;
		if (includePayload && _settings.PayloadPattern == ObjectSpacePayloadPattern.Variable && payloadBytes > 0)
			payloadBytes = (int)((ulong)id * 2654435761UL % ((ulong)payloadBytes + 1));
		var record = ObjectSpaceLabFactory.CreateRecord(id, payloadBytes);
		record.Category = GetCategory(id);
		return record;
	}

	private int GetCategory(long id) {
		if (_settings.HotCategoryPercent == 0)
			return (int)((id - 1) % _settings.CategoryCount);
		return (id - 1) * 37 % 100 < _settings.HotCategoryPercent ? 0 : 1 + (int)((id - 1) % (_settings.CategoryCount - 1));
	}

	private byte[] CreateResizedPayload(long id, int length, int index) {
		var resizedLength = index % 2 == 0 ? length + Math.Max(1, _settings.PayloadBytes / 2) : length / 2;
		return Tools.Collection.GenerateArray(resizedLength, position => (byte)((id + position + 1) % 251));
	}

	private void DeleteWorkingFiles() {
		if (Directory.Exists(_workingPages))
			Tools.FileSystem.DeleteDirectory(_workingPages);
		if (File.Exists(_workingPath))
			File.Delete(_workingPath);
		if (File.Exists(_workingPath + ".objectspace.json"))
			File.Delete(_workingPath + ".objectspace.json");
	}

	private static void VerifyCardinality(ObjectSpace store, int expected)
		=> Guard.Ensure(store.Count<ObjectSpaceLabRecord>() == expected, $"Expected {expected} saved records in the benchmark fixture.");

	private static void VerifyRecord(ObjectSpaceLabRecord actual, ObjectSpaceLabRecord expected)
		=> Guard.Ensure(actual.Id == expected.Id && actual.Code == expected.Code && actual.Category == expected.Category
			&& actual.Balance == expected.Balance && actual.Notes == expected.Notes && actual.Payload.SequenceEqual(expected.Payload), "Benchmark data failed round-trip validation.");

	private void WriteStorageManifest() {
		var directory = Path.Combine(_settings.OutputDirectory, "storage", _settings.RunId);
		Directory.CreateDirectory(directory);
		var manifest = new {
			Profile = _profile.Name,
			Configuration,
			_settings.RecordCount,
			_settings.PayloadBytes,
			_settings.ShuffledInsertion,
			_settings.LookupPattern,
			_settings.HotSetPercent,
			_settings.PayloadPattern,
			_settings.CategoryCount,
			_settings.HotCategoryPercent,
			_settings.WorkloadMix,
			_profile.ComparisonGroup,
			BaselineStorageBytes,
			LongIdentifierNodeBytes = 24L * Configuration.IndexBTreeOrder + (Configuration.IndexBTreeSpec?.Type == BTreeType.BTreePlus ? 25 : 17),
			Note = "Storage bytes exclude configuration manifests and transient transaction pages; page cache budget excludes tracked object instances."
		};
		File.WriteAllText(Path.Combine(directory, $"{Guid.NewGuid():N}.json"), JsonSerializer.Serialize(manifest, ProfilerSettings.JsonOptions));
	}
}
