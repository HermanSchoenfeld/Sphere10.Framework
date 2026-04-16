// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using Sphere10.Framework.Mapping;

namespace Sphere10.Framework.ObjectSpaces;

/// <summary>
/// Core implementation of an object space that orchestrates clustered streams, dimensions, serializers, and indexes for persisted objects.
/// Supports optional garbage collection of non-root dimension objects via reference counting.
/// </summary>
public class ObjectSpace : SyncLoadableBase, ICriticalObject, IDisposable {

	protected readonly ClusteredStreams _streams;
	protected readonly DictionaryList<Type, Dimension> _dimensions;
	private readonly InstanceTracker _instanceTracker;
	private bool _loaded;
	protected readonly bool AutoSave;

	/// <summary>
	/// Whether GC is enabled for this ObjectSpace (set from <see cref="ObjectSpaceTraits.GarbageCollect"/>).
	/// </summary>
	protected readonly bool GarbageCollectEnabled;

	/// <summary>
	/// Set of all CLR types registered as dimensions, used by <see cref="ObjectSpaceReferenceSerializer{TItem}"/>
	/// to distinguish dimension objects (external references) from component objects (inline serialization).
	/// </summary>
	private readonly HashSet<Type> _dimensionTypes;

	/// <summary>
	/// Persisted out-refs index: maps each object's <see cref="ObjectSpaceObjectReference"/> to the set of
	/// dimension objects it references. Stored in a reserved stream within the top-level ClusteredStreams.
	/// Null when GC is not enabled.
	/// </summary>
	private Dictionary<ObjectSpaceObjectReference, HashSet<ObjectSpaceObjectReference>> _persistedOutRefs;

	/// <summary>
	/// Persisted in-refs index: maps each object's <see cref="ObjectSpaceObjectReference"/> to the set of
	/// dimension objects that reference it. The inverse of <see cref="_persistedOutRefs"/>.
	/// An empty entry on a non-root object = garbage. Null when GC is not enabled.
	/// </summary>
	private Dictionary<ObjectSpaceObjectReference, HashSet<ObjectSpaceObjectReference>> _persistedInRefs;

	protected ObjectSpace(ClusteredStreams streams, ObjectSpaceDefinition objectSpaceDefinition, SerializerFactory serializerFactory, ComparerFactory comparerFactory, FileAccessMode accessMode = FileAccessMode.Default) {
		Guard.ArgumentNotNull(streams, nameof(streams));
		Guard.ArgumentNotNull(objectSpaceDefinition, nameof(objectSpaceDefinition));
		Guard.ArgumentNotNull(serializerFactory, nameof(serializerFactory));
		Guard.ArgumentNotNull(comparerFactory, nameof(comparerFactory));
		_streams = streams;
		Definition = objectSpaceDefinition;
		Serializers = serializerFactory;
		Comparers = comparerFactory;
		_loaded = false;
		_dimensions = new DictionaryList<Type, Dimension>(TypeEquivalenceComparer.Instance, ReferenceEqualityComparer.Instance);
		_instanceTracker = new InstanceTracker();
		AutoSave = objectSpaceDefinition.Traits.HasFlag(ObjectSpaceTraits.AutoSave);
		GarbageCollectEnabled = objectSpaceDefinition.Traits.HasFlag(ObjectSpaceTraits.GarbageCollect);
		Disposables = Disposables.None;
		FlushOnDispose = true;

		// Build the set of dimension types for external reference classification
		_dimensionTypes = new HashSet<Type>(
			objectSpaceDefinition.Dimensions.Select(d => d.ObjectType),
			TypeEquivalenceComparer.Instance
		);

		// Initialize persisted ref indexes (populated during LoadInternal if GC is enabled)
		_persistedOutRefs = null;
		_persistedInRefs = null;
	}

	public override bool RequiresLoad => !_loaded || _streams.RequiresLoad;
	
	public ObjectSpaceDefinition Definition { get; }

	public SerializerFactory Serializers { get; }

	public ComparerFactory Comparers { get; }

	public ICriticalObject ParentCriticalObject { get => _streams.ParentCriticalObject; set => _streams.ParentCriticalObject = value; }
	
	public object Lock => _streams.Lock;
	
	public bool IsLocked => _streams.IsLocked;

	internal ClusteredStreams Streams => _streams;

	public IReadOnlyList<Dimension> Dimensions => _dimensions;

	public Disposables Disposables { get; }

	protected bool FlushOnDispose { get; set; }

	public IDisposable EnterAccessScope() => _streams.EnterAccessScope();

	public IEnumerable<TItem> GetAll<TItem>() {
		throw new NotImplementedException();
	}

	public TItem Get<TItem>(long index) {
		if (!TryGet<TItem>(index, out var item))
			throw new InvalidOperationException($"No {typeof(TItem).ToStringCS()} at index {index} found");
		return item;
	}

	public TItem Get<TItem, TMember>(Expression<Func<TItem, TMember>> memberExpression, TMember memberValue) {
		if (!TryGet(memberExpression, memberValue, out var item))
			throw new InvalidOperationException($"No {typeof(TItem).ToStringCS()} item found with member '{memberExpression.ToMember().Name}' matching '{memberValue}'");
		return item;
	}

	public bool TryGet<TItem>(long index, out TItem item) {
		using (EnterAccessScope()) {

			// First try return an already fetched instance
			if (_instanceTracker.TryGet(index, out item))
				return true;	
			
			// Get underlying stream mapped collection
			var dimension = GetDimension<TItem>();
			
			// Range check
			if (0 > index || index >= dimension.Container.Count)
				return false;

			// Reap check
			if (dimension.Container.ObjectStream.IsReaped(index))
				return false;

			// Deserialize from stream
			item = dimension.Container.ObjectStream.LoadItem(index);

			// Track item instance
			_instanceTracker.Track(item, index);
			dimension.Definition.ChangeTracker.SetChanged(item, false);

			// Eagerly assign the ObjectSpaceObjectReference (Task 6) so it is known before any serialization
			var dimIdx = GetDimensionIndex(typeof(TItem));
			_instanceTracker.TrackRef(item, new ObjectSpaceObjectReference(dimIdx, index));

			return true;
		}
	}

	public bool TryGet<TItem, TMember>(Expression<Func<TItem, TMember>> memberExpression, TMember memberValue, out TItem item) {
		using (EnterAccessScope()) {

			// Get underlying stream mapped collection
			var dimension = GetDimension<TItem>();

			// Get index for member
			var index = dimension.Container.ObjectStream.GetUniqueIndexFor(memberExpression);

			// Find the item in the index
			if (!index.Values.TryGetValue(memberValue, out var itemIndex)) {
				item = default;
				return false;
			}

			// Get the item referenced by the index
			// NOTE: the item could be cached and contain an unsaved update to the member
			if (!TryGet(itemIndex, out item))
				throw new InvalidOperationException($"Index for {memberExpression.ToMember()} referenced a non-existent item at {itemIndex}");

			return true;
		}
	}

	public TItem New<TItem>() where TItem : new() {
		var instance = new TItem();
		AcceptNew(instance);
		return instance;
	}

	public int AcceptNew<TItem>(TItem item)
		=> AcceptNewInternal(typeof(TItem), item);

	public int AcceptNew(object item)
		=> AcceptNewInternal(item.GetType(), item);

	public long Count<TItem>() => CountInternal(typeof(TItem));

	public long Save<TItem>(TItem item) 
		=> SaveInternal(typeof(TItem), item);

	public long Save(object item) 
		=> SaveInternal(item.GetType(), item);

	public void Delete<TItem>(TItem item) 
		=> DeleteInternal(typeof(TItem), item);

	public void Delete(object item) 
		=> DeleteInternal(item.GetType(), item);

	/// <summary>
	/// Clears all data in the object space. This is a destructive operation and cannot be undone. Must pass <b>"I CONSENT TO CLEAR ALL DATA"</b> for execution.
	/// </summary>
	/// <param name="consentGuard">Must be: "I CONSENT TO CLEAR ALL DATA"</param>
	public void Clear(string consentGuard) {
		Guard.ArgumentNotNull(consentGuard, nameof(consentGuard));
		Guard.Argument(consentGuard == "I CONSENT TO CLEAR ALL DATA", nameof(consentGuard), "Consent guard not provided");
		using (EnterAccessScope()) {
			foreach(var dimension in Dimensions) {
				dimension.Container.Clear();
			}
		}
		_instanceTracker.Clear();

		// Also clear the persisted ref indexes when GC is enabled
		_persistedOutRefs?.Clear();
		_persistedInRefs?.Clear();
	}

	public virtual void Flush() {
		// save any modified objects (persistence ignorance)
		if (AutoSave)
			SaveModifiedObjects();

		// ensure all dirty merkle-trees are fully calculated
		foreach(var dimension in _dimensions.Values) 
		foreach(var merkleTreeIndex in dimension.Container.ObjectStream.Streams.Attachments.Values.Where(x => x is MerkleTreeIndex).Cast<MerkleTreeIndex>())
			merkleTreeIndex.Flush();

		// ensure any spatial merkle-trees are fully calculated
		foreach (var spatialTreeIndex in _streams.Attachments.Values.Where(x => x is ObjectSpaceMerkleTreeIndex).Cast<ObjectSpaceMerkleTreeIndex>())
			spatialTreeIndex.Flush();

		// flush the underlying stream that maps entire object-space
		Streams.RootStream.Flush();
	}

	public virtual void Dispose() {
		if (FlushOnDispose)
			Flush();
		Unload();
		_streams.Dispose();
		Disposables.Dispose();
	}

	protected override void LoadInternal() {
		const int ObjectSpaceMerkleTreeReservedStreamIndex = 0;
		Guard.Against(_loaded, "ObjectSpace already loaded.");

		Definition.Validate().ThrowOnFailure();

		// Load ObjectStream streams
		if (_streams.RequiresLoad)
			_streams.Load();

	
		// Ensure all dimension streams are created on the first load
		var isFirstTimeLoad = (_streams.Header.StreamCount - _streams.Header.ReservedStreams) == 0;
		if (isFirstTimeLoad) {
			// Create dimension streams
			for (var i = 0; i < Definition.Dimensions.Length; i++) {
				_streams.AddBytes(ReadOnlySpan<byte>.Empty);
			}
		}

		// Check streams and dimensions match
		var dimensionStreams = _streams.Header.StreamCount - _streams.Header.ReservedStreams;
		Guard.Ensure(dimensionStreams == Definition.Dimensions.Length, $"Unexpected stream count {Definition?.Dimensions.Length}, expected {dimensionStreams}");

		// Add dimensions to object space
		for (var ix = 0; ix < Definition.Dimensions.Length; ix++) {
			var dimensionDefinition = Definition.Dimensions[ix];
			var dimension = BuildDimension(dimensionDefinition, ix);
			_dimensions.Add(dimensionDefinition.ObjectType, dimension);
		}

		// Attach object space merkle-tree (if applicable)
		if (Definition.Traits.HasFlag(ObjectSpaceTraits.Merkleized)) {
			if (isFirstTimeLoad) {
				// On first load, we need to pre-fill the spatial-tree with an empty tree that denotes all null leafs for each dimension.
				// This is to ensure when spatial tree is loaded that it's buffer matches what is expected. This is ugly
				// but safe since the merkle-tree and it's mapped stream are kept 1-1 consistent at all times.
				_streams.UpdateBytes(
					ObjectSpaceMerkleTreeReservedStreamIndex, 
					MerkleTreeStorageAttachment.GenerateBytes(
						Definition.HashFunction, 
						Tools.Collection.RepeatValue(Hashers.ZeroHash(Definition.HashFunction), dimensionStreams)
					)
				);
			}

			var spaceTree = new ObjectSpaceMerkleTreeIndex(
				this,
				_streams,
				Sphere10FrameworkDefaults.DefaultSpatialMerkleTreeIndexName,
				Sphere10FrameworkDefaults.DefaultMerkleTreeIndexName,
				Definition.HashFunction, 
				isFirstTimeLoad
			);
			_streams.RegisterAttachment(spaceTree);

		}

		_loaded = true;

		// Initialize persisted ref indexes if GC is enabled (Task 11)
		if (GarbageCollectEnabled) {
			_persistedOutRefs = new Dictionary<ObjectSpaceObjectReference, HashSet<ObjectSpaceObjectReference>>();
			_persistedInRefs = new Dictionary<ObjectSpaceObjectReference, HashSet<ObjectSpaceObjectReference>>();
		}
	}
	
	protected void SaveModifiedObjects() {
		Guard.Ensure(AutoSave, "AutoSave is not enabled");
		using (EnterAccessScope()) {
			var changedObjects = 
				_instanceTracker
					.GetInstances()
					.Select(x => (Type: x.GetType(), Instance: x))
					.Where(x => _dimensions[x.Type].Definition.ChangeTracker.HasChanged(x.Instance))
					.Select(x => x.Instance)
					.ToArray();
			foreach(var changedObject in changedObjects) {
				// check if still changed (prior connected object may have saved it recursively)
				if (_dimensions[changedObject.GetType()].Definition.ChangeTracker.HasChanged(changedObject))
					Save(changedObject);
			}
		}
	}

	protected void Unload() {
		// close all object containers when rolling back
		foreach (var disposable in _dimensions.Values.Cast<IDisposable>())
			disposable.Dispose();
		_dimensions.Clear();
		_instanceTracker.Clear();

		// Clear persisted ref indexes on unload
		_persistedOutRefs?.Clear();
		_persistedInRefs?.Clear();
		
		// unsubscribe to RollingBack event prevent re-entrant unloads (disposal of Streams will result in internal rollback event)
		_streams.UnloadAttachments();


		// Mark as loaded
		_loaded = false;
	}

	protected int AcceptNewInternal(Type itemType, object item) {
		var dimension = GetDimension(itemType);
		if (AutoSave)
			dimension.Definition.ChangeTracker.SetChanged(item, true);

		// Track the new instance with a provisional index based on dimension count + new instance count
		var provisionalIndex = _instanceTracker.TrackNew(item, dimension.Container.Count);

		// Eagerly assign an ObjectSpaceObjectReference with the provisional ObjectIndex (Task 6).
		// This ensures the reference is always available before serialization begins.
		// It will be replaced with the real persisted index during SaveInternal.
		var dimIdx = GetDimensionIndex(itemType);
		_instanceTracker.TrackRef(item, new ObjectSpaceObjectReference(dimIdx, provisionalIndex));

		return (int)provisionalIndex;
	}

	protected long CountInternal(Type itemType) {
		using (EnterAccessScope()) {
			// Get underlying stream mapped collection
			var dimension = GetDimension(itemType);
			return dimension.Container.Count;
		}
	}

	protected long SaveInternal(Type itemType, object item) {
		using (EnterAccessScope()) {
			// Get the item index (if applicable)
			if (!_instanceTracker.TryGetIndexOf(item, out var index)) 
				index = AcceptNewInternal(itemType, item);

			// Get underlying stream mapped collection
			var dimension = GetDimension(itemType);

			if (!_instanceTracker.IsProvisional(item)) {
				// Update existing persisted object
				dimension.Container.Update(index, item);
			} else {
				// Add new object to storage
				var provisionalIndex = index;
				dimension.Container.Add(item, out index);
				_instanceTracker.MarkPersisted(item);

				// Update tracked index and ObjectSpaceObjectReference only if the real index differs from provisional
				if (index != provisionalIndex) {
					_instanceTracker.Track(item, index);
					var dimIdx = GetDimensionIndex(itemType);
					_instanceTracker.TrackRef(item, new ObjectSpaceObjectReference(dimIdx, index));
				}
			}

			// Mark as unchanged
			dimension.Definition.ChangeTracker.SetChanged(item, false);

			// --- GC reference tracking (Tasks 7, 8, 12) ---
			if (GarbageCollectEnabled)
				UpdateRefsAfterSave(itemType, item, index);

			return index;
		}
	}

	protected void DeleteInternal(Type itemType, object item) {
		using (EnterAccessScope()) {
			// Get tracked index of item instance
			if (!_instanceTracker.TryGetIndexOf(item, out var index)) 
				throw new InvalidOperationException($"Instance of {item.GetType().ToStringCS()} was not tracked");

			// Capture the object's refs before untracking for GC processing
			ObjectSpaceObjectReference selfRef = default;
			HashSet<ObjectSpaceObjectReference> previousOutRefs = null;
			if (GarbageCollectEnabled) {
				_instanceTracker.TryGetRef(item, out selfRef);
				previousOutRefs = _instanceTracker.GetOrCreateOutRefs(item);
			}

			// Check if persisted before untracking (provisional objects haven't been written to storage)
			var isPersisted = !_instanceTracker.IsProvisional(item);

			// Stop tracking instance
			_instanceTracker.Untrack(item);

			// Remove it from the dimension only if it was persisted
			var dimension = GetDimension(itemType);
			if (isPersisted)
				dimension.Container.Recycle(index);

			// --- GC reference cleanup and cascade (Tasks 8, 9, 12) ---
			if (GarbageCollectEnabled && previousOutRefs is not null)
				CascadeDeleteRefs(selfRef, previousOutRefs);
		}
	}

	protected virtual Dimension BuildDimension(ObjectSpaceDefinition.DimensionDefinition dimensionDefinition, int spatialStreamIndex) {

		// Get the stream designated for this dimension from the object space
		var dimensionStream = _streams.Open(_streams.Header.ReservedStreams + spatialStreamIndex, false, true); // note: locking scope is kept here?

		// Create a ClusteredStreamCollection which maps over the dimension's stream. This will be used by the ObjectStream to store the objects.
		var clusteredDimensionStream = new ClusteredStreams(
			dimensionStream,
			SanitizeContainerClusterSize(dimensionDefinition.AverageObjectSizeBytes),
			ClusteredStreamsPolicy.FastAllocate,
			dimensionDefinition.Indexes.Length,
			_streams.Endianness,
			false
		) {
			OwnsStream = true
		};

		// Construct the object stream collection which uses the clustered stream collection which maps over the dimension's stream
		var objectStream =
			typeof(ObjectStream<>)
				.MakeGenericType(dimensionDefinition.ObjectType)
				.ActivateWithCompatibleArgs(
					clusteredDimensionStream,
					CreateItemSerializer(dimensionDefinition.ObjectType),
					false
				) as ObjectStream;
		objectStream.OwnsStreams = true;

		// construct the required indexes for the dimension
		foreach (var index in dimensionDefinition.Indexes) {
			IClusteredStreamsAttachment metaDataObserver = index.Type switch {
				ObjectSpaceDefinition.IndexType.Identifier => BuildIdentifier(objectStream, dimensionDefinition, index),
				ObjectSpaceDefinition.IndexType.UniqueKey => BuildUniqueKey(objectStream, dimensionDefinition, index),
				ObjectSpaceDefinition.IndexType.Index => BuildIndex(objectStream, dimensionDefinition, index),
				ObjectSpaceDefinition.IndexType.RecyclableIndexStore => BuildRecyclableIndexStore(objectStream, dimensionDefinition, index),
				ObjectSpaceDefinition.IndexType.MerkleTree => BuildMerkleTreeIndex(objectStream, dimensionDefinition, index),
				_ => throw new ArgumentOutOfRangeException()
			};
			objectStream.Streams.RegisterAttachment(metaDataObserver);
		}
	
		// Construct a suitable a comparer
		var comparer = Comparers.GetEqualityComparer(dimensionDefinition.ObjectType);

		// Construct the object collection which uses the underlying object stream
		var list = (IStreamMappedCollection)typeof(StreamMappedRecyclableList<>)
			.MakeGenericType(dimensionDefinition.ObjectType)
			.ActivateWithCompatibleArgs(
				objectStream,
				dimensionDefinition.Indexes.First(x => x.Type == ObjectSpaceDefinition.IndexType.RecyclableIndexStore).Name,
				comparer,
				false
			);
		Tools.Reflection.SetPropertyValue(list, nameof(StreamMappedRecyclableList<int>.OwnsContainer), true);

		// load list if applicable
		if (list is ILoadable { RequiresLoad: true } loadable)
			loadable.Load();


		// construct a typed dimension object for client
		var dimension = (Dimension)Activator.CreateInstance(
			typeof(Dimension<>).MakeGenericType(dimensionDefinition.ObjectType),
			new object[] { dimensionDefinition, list }
		);

		return dimension;
	}

	protected virtual IClusteredStreamsAttachment BuildIdentifier(ObjectStream dimension, ObjectSpaceDefinition.DimensionDefinition dimensionDefinition, ObjectSpaceDefinition.IndexDefinition indexDefinition) {
		// NOTE: same as BuildUniqueKey, but may have differentiating functionality in future
		var keyComparer = Comparers.GetEqualityComparer(indexDefinition.Member.PropertyType);
		var keySerializer = Serializers.GetSerializer(indexDefinition.Member.PropertyType);
		return
			keySerializer.IsConstantSize ?
				IndexFactory.CreateUniqueMemberIndex(dimension, indexDefinition.Name, indexDefinition.Member, keySerializer, keyComparer) :
				IndexFactory.CreateUniqueMemberChecksumIndex(dimension, indexDefinition.Name, indexDefinition.Member, keySerializer, null, null, keyComparer, indexDefinition.NullPolicy);
	}

	protected virtual IClusteredStreamsAttachment BuildUniqueKey(ObjectStream dimension, ObjectSpaceDefinition.DimensionDefinition dimensionDefinition, ObjectSpaceDefinition.IndexDefinition indexDefinition) {
		var keyComparer = Comparers.GetEqualityComparer(indexDefinition.Member.PropertyType);
		var keySerializer = Serializers.GetSerializer(indexDefinition.Member.PropertyType);
		return
			keySerializer.IsConstantSize ?
				IndexFactory.CreateUniqueMemberIndex(dimension, indexDefinition.Name, indexDefinition.Member, keySerializer, keyComparer) :
				IndexFactory.CreateUniqueMemberChecksumIndex(dimension, indexDefinition.Name, indexDefinition.Member, keySerializer, null, null, keyComparer, indexDefinition.NullPolicy);
	}

	protected virtual IClusteredStreamsAttachment BuildIndex(ObjectStream dimension, ObjectSpaceDefinition.DimensionDefinition dimensionDefinition, ObjectSpaceDefinition.IndexDefinition indexDefinition) {
		var keyComparer = Comparers.GetEqualityComparer(indexDefinition.Member.PropertyType);
		var keySerializer = Serializers.GetSerializer(indexDefinition.Member.PropertyType);
		return
			keySerializer.IsConstantSize ?
				IndexFactory.CreateMemberIndex(dimension, indexDefinition.Name, indexDefinition.Member, keySerializer, keyComparer) :
				IndexFactory.CreateMemberChecksumIndex(dimension, indexDefinition.Name, indexDefinition.Member, keySerializer, null, null, keyComparer, indexDefinition.NullPolicy);
	}

	protected virtual IClusteredStreamsAttachment BuildRecyclableIndexStore(ObjectStream dimension, ObjectSpaceDefinition.DimensionDefinition dimensionDefinition, ObjectSpaceDefinition.IndexDefinition indexDefinition) {
		return new RecyclableIndexIndex(dimension, indexDefinition.Name);
	}

	protected virtual IClusteredStreamsAttachment BuildMerkleTreeIndex(ObjectStream dimension, ObjectSpaceDefinition.DimensionDefinition dimensionDefinition, ObjectSpaceDefinition.IndexDefinition indexDefinition) {
		return new MerkleTreeIndex(dimension, indexDefinition.Name, new ObjectStreamItemHasher(dimension, Definition.HashFunction), Definition.HashFunction);
	}
	
	protected virtual IItemSerializer CreateItemSerializer(Type objectType) {
		// Get the base serializer from the factory (typically a CompositeSerializer wrapped in ReferenceSerializer)
		var baseSerializer = Serializers.GetSerializer(objectType);

		// If GC is enabled, wrap the serializer with ObjectSpaceReferenceSerializer to support
		// external references for dimension objects. The wrapper creates an ObjectSpaceSerializationContext
		// so that dimension-typed properties are serialized as lightweight ObjectSpaceObjectReference
		// pointers instead of inline objects.
		if (GarbageCollectEnabled) {
			var wrapperType = typeof(ObjectSpaceReferenceSerializer<>).MakeGenericType(objectType);
			baseSerializer = (IItemSerializer)Activator.CreateInstance(
				wrapperType,
				baseSerializer,
				_dimensionTypes,
				_instanceTracker,
				(Func<ObjectSpaceObjectReference, object>)ResolveExternalReference
			);
		}

		return baseSerializer;
	}

	protected virtual int SanitizeContainerClusterSize(int? clusterSizeB)
		=> Tools.Values.ClipValue(
			clusterSizeB.GetValueOrDefault(), 
			Sphere10FrameworkDefaults.SmallestRecommendedClusterSize, 
			Sphere10FrameworkDefaults.LargestRecommendedClusterSize
		);

	/// <summary>
	/// Returns the ordinal index of the dimension for the given CLR type.
	/// This index is the <see cref="ObjectSpaceObjectReference.DimensionIndex"/> component.
	/// </summary>
	private short GetDimensionIndex(Type itemType) {
		for (var i = 0; i < Definition.Dimensions.Length; i++) {
			if (TypeEquivalenceComparer.Instance.Equals(Definition.Dimensions[i].ObjectType, itemType))
				return (short)i;
		}
		throw new InvalidOperationException($"Type '{itemType.ToStringCS()}' is not a registered dimension");
	}

	/// <summary>
	/// Resolves an <see cref="ObjectSpaceObjectReference"/> to a live object instance.
	/// Checks the InstanceTracker cache first (no I/O); if not found, loads from the dimension's stream.
	/// Used as the resolution callback in <see cref="ObjectSpaceSerializationContext"/>.
	/// </summary>
	private object ResolveExternalReference(ObjectSpaceObjectReference objRef) {
		// Fast path: check if the object is already in the instance cache
		if (_instanceTracker.TryResolveRef(objRef, out var item))
			return item;

		// Slow path: load from the dimension's stream and cache it
		var dimensionDef = Definition.Dimensions[objRef.DimensionIndex];
		var dimension = _dimensions[dimensionDef.ObjectType];
		item = dimension.Container.ObjectStream.LoadItem(objRef.ObjectIndex);

		// Track the loaded instance
		_instanceTracker.Track(item, objRef.ObjectIndex);
		dimension.Definition.ChangeTracker.SetChanged(item, false);
		_instanceTracker.TrackRef(item, objRef);
		return item;
	}

	// --- GC helper methods (Tasks 7, 8, 9, 12) ---

	/// <summary>
	/// Called after a successful save to update the in-memory and persisted reference indexes.
	/// Diffs the new out-refs (from serialization) against the previous out-refs and updates in-refs accordingly.
	/// If any former target now has zero in-refs and is non-root, it is garbage-collected.
	/// </summary>
	private void UpdateRefsAfterSave(Type itemType, object item, long index) {
		if (!_instanceTracker.TryGetRef(item, out var selfRef))
			return;

		// Get the new out-refs collected during the most recent serialization pass
		// NOTE: The serialization context's CollectedOutRefs should have been populated during the
		// most recent dimension.Container.Update/Add call. For now, use the in-memory cache which
		// is the ground truth for this object's references after serialization.
		var currentOutRefs = _instanceTracker.GetOrCreateOutRefs(item);

		// Read previous persisted out-refs (empty if this is a new object)
		var previousPersistedOutRefs = _persistedOutRefs != null && _persistedOutRefs.TryGetValue(selfRef, out var prevRefs)
			? prevRefs
			: new HashSet<ObjectSpaceObjectReference>();

		// Compute the diff: which refs were added, which were removed
		var addedRefs = new HashSet<ObjectSpaceObjectReference>(currentOutRefs);
		addedRefs.ExceptWith(previousPersistedOutRefs);

		var removedRefs = new HashSet<ObjectSpaceObjectReference>(previousPersistedOutRefs);
		removedRefs.ExceptWith(currentOutRefs);

		// Update persisted in-refs for removed targets: remove self from their in-refs
		foreach (var removedTarget in removedRefs) {
			if (_persistedInRefs != null && _persistedInRefs.TryGetValue(removedTarget, out var targetInRefs))
				targetInRefs.Remove(selfRef);

			// Also update in-memory in-refs if the target is loaded
			if (_instanceTracker.TryGetInRefs(removedTarget, out var memInRefs))
				memInRefs.Remove(selfRef);
		}

		// Update persisted in-refs for added targets: add self to their in-refs
		foreach (var addedTarget in addedRefs) {
			if (_persistedInRefs != null) {
				if (!_persistedInRefs.TryGetValue(addedTarget, out var targetInRefs)) {
					targetInRefs = new HashSet<ObjectSpaceObjectReference>();
					_persistedInRefs[addedTarget] = targetInRefs;
				}
				targetInRefs.Add(selfRef);
			}

			// Also update in-memory in-refs if the target is loaded
			if (_instanceTracker.TryResolveRef(addedTarget, out var targetObj))
				_instanceTracker.GetOrCreateInRefs(targetObj).Add(selfRef);
		}

		// Replace persisted out-refs for this object
		if (_persistedOutRefs != null)
			_persistedOutRefs[selfRef] = new HashSet<ObjectSpaceObjectReference>(currentOutRefs);

		// Cascade GC: check if any removed targets are now orphaned (zero in-refs on non-root dimension)
		foreach (var removedTarget in removedRefs)
			TryCollectIfOrphaned(removedTarget);
	}

	/// <summary>
	/// Called after a delete to remove the deleted object's out-refs from all targets' in-refs,
	/// then cascade-collects any targets that become orphaned.
	/// </summary>
	private void CascadeDeleteRefs(ObjectSpaceObjectReference selfRef, HashSet<ObjectSpaceObjectReference> previousOutRefs) {
		// Remove self from each target's persisted in-refs
		foreach (var targetRef in previousOutRefs) {
			if (_persistedInRefs != null && _persistedInRefs.TryGetValue(targetRef, out var targetInRefs))
				targetInRefs.Remove(selfRef);

			// Also update in-memory in-refs
			if (_instanceTracker.TryGetInRefs(targetRef, out var memInRefs))
				memInRefs.Remove(selfRef);
		}

		// Remove persisted entries for the deleted object itself
		_persistedOutRefs?.Remove(selfRef);
		_persistedInRefs?.Remove(selfRef);

		// Cascade: check each former target — if non-root and zero in-refs, collect it
		foreach (var targetRef in previousOutRefs)
			TryCollectIfOrphaned(targetRef);
	}

	/// <summary>
	/// Checks whether the object at <paramref name="targetRef"/> is an orphan (non-root dimension, zero in-refs)
	/// and if so, deletes it. This may cascade further as the deleted object's own out-refs are cleaned up.
	/// Uses a queue to avoid stack overflow from deep reference chains.
	/// </summary>
	private void TryCollectIfOrphaned(ObjectSpaceObjectReference targetRef) {
		// Use a queue to iteratively process orphans instead of recursion (handles cycles, avoids stack overflow)
		var collectionQueue = new Queue<ObjectSpaceObjectReference>();
		collectionQueue.Enqueue(targetRef);

		while (collectionQueue.Count > 0) {
			var candidate = collectionQueue.Dequeue();

			// Skip if this dimension is a root — root objects are never auto-collected
			if (candidate.DimensionIndex < 0 || candidate.DimensionIndex >= Definition.Dimensions.Length)
				continue;
			var dimensionDef = Definition.Dimensions[candidate.DimensionIndex];
			if (dimensionDef.IsRoot)
				continue;

			// Check persisted in-refs: only collect if truly zero incoming references
			if (_persistedInRefs != null && _persistedInRefs.TryGetValue(candidate, out var inRefs) && inRefs.Count > 0)
				continue;

			// This object is garbage — collect it
			// First, read its out-refs so we can cascade
			HashSet<ObjectSpaceObjectReference> outRefsToClean = null;
			if (_persistedOutRefs != null && _persistedOutRefs.TryGetValue(candidate, out var outs))
				outRefsToClean = new HashSet<ObjectSpaceObjectReference>(outs);

			// Recycle from dimension if persisted (the object exists in storage)
			var dimension = _dimensions[dimensionDef.ObjectType];
			if (candidate.ObjectIndex >= 0 && candidate.ObjectIndex < dimension.Container.Count) {
				dimension.Container.Recycle(candidate.ObjectIndex);
			}

			// Untrack from InstanceTracker if loaded
			if (_instanceTracker.TryResolveRef(candidate, out var obj))
				_instanceTracker.Untrack(obj);

			// Clean up persisted indexes
			_persistedOutRefs?.Remove(candidate);
			_persistedInRefs?.Remove(candidate);

			// Cascade: remove this object from each of its targets' in-refs, then check if those targets are now orphaned
			if (outRefsToClean is not null) {
				foreach (var subTarget in outRefsToClean) {
					if (_persistedInRefs != null && _persistedInRefs.TryGetValue(subTarget, out var subInRefs))
						subInRefs.Remove(candidate);

					if (_instanceTracker.TryGetInRefs(subTarget, out var memSubInRefs))
						memSubInRefs.Remove(candidate);

					// Queue this target for orphan check
					collectionQueue.Enqueue(subTarget);
				}
			}
		}
	}

	/// <summary>
	/// Explicit full-scan garbage collection. Iterates all objects in non-root dimensions and collects
	/// any with empty persisted in-refs. This is a safety net / consistency check — the primary GC
	/// mechanism is the cascading check in <see cref="SaveInternal"/> and <see cref="DeleteInternal"/>.
	/// </summary>
	public void CollectGarbage() {
		if (!GarbageCollectEnabled)
			return;

		using (EnterAccessScope()) {
			// Collect all non-root objects with zero in-refs
			var orphans = new List<ObjectSpaceObjectReference>();

			for (var dimIdx = 0; dimIdx < Definition.Dimensions.Length; dimIdx++) {
				var dimensionDef = Definition.Dimensions[dimIdx];

				// Skip root dimensions — their objects are never garbage-collected
				if (dimensionDef.IsRoot)
					continue;

				var dimension = _dimensions[dimensionDef.ObjectType];
				var count = dimension.Container.Count;

				// Scan all rows in this non-root dimension
				for (long rowIdx = 0; rowIdx < count; rowIdx++) {
					// Skip recycled (already deleted) rows
					if (dimension.Container.ObjectStream.IsReaped(rowIdx))
						continue;

					var objRef = new ObjectSpaceObjectReference((short)dimIdx, rowIdx);

					// Check if this object has any incoming references
					var hasInRefs = _persistedInRefs != null
						&& _persistedInRefs.TryGetValue(objRef, out var inRefs)
						&& inRefs.Count > 0;

					if (!hasInRefs)
						orphans.Add(objRef);
				}
			}

			// Collect each orphan (this may cascade further)
			foreach (var orphan in orphans)
				TryCollectIfOrphaned(orphan);
		}
	}

	private Dimension<TItem> GetDimension<TItem>() 
		=> (Dimension<TItem>)GetDimension(typeof(TItem));

	private Dimension GetDimension(Type itemType) {
		if (!_dimensions.TryGetValue(itemType, out var dimension))
			throw new InvalidOperationException($"A dimension for type '{itemType.ToStringCS()}' was not registered");

		return dimension;
	}

	#region Aux Types
	public record Dimension(ObjectSpaceDefinition.DimensionDefinition Definition, IStreamMappedRecylableList Container) : IDisposable {
		public void Dispose() => (Container as IDisposable)?.Dispose();
	};
	public record Dimension<T>(ObjectSpaceDefinition.DimensionDefinition Definition, StreamMappedRecyclableList<T> Container) : Dimension(Definition, Container) {
		public new StreamMappedRecyclableList<T> Container => (StreamMappedRecyclableList<T>)base.Container;
	}

	#endregion

}


