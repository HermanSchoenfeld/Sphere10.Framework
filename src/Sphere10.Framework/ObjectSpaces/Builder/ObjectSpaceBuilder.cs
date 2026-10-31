// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Sphere10.Framework.ObjectSpaces;

/// <summary>Configures dimensions, factories and storage settings for independently built ObjectSpaces.</summary>
public class ObjectSpaceBuilder {

	private static readonly string ErrMsgUseSerializerFactory = $"Unable to use a custom {nameof(SerializerFactory)} since {typeof(IItemSerializer<>).ToStringCS()}'s have already been registered. Ensure all serializer registrations are made to your custom factory and not with this {nameof(ObjectSpaceBuilder)}.";
	private static readonly string ErrMsgUseComparerFactory = $"Unable to use a custom {nameof(ComparerFactory)} since {typeof(IEqualityComparer).ToStringCS()}'s have already been registered. Ensure all comparer registrations are made to your custom factory and not with this {nameof(ObjectSpaceBuilder)}.";
	private static readonly string ErrMsgUsingSerializer = $"Unable to use a custom {typeof(IItemSerializer<>).ToStringCS()} since a custom {nameof(SerializerFactory)} has been registered. Ensure all serializer registrations are made with your custom factory and not with this {nameof(ObjectSpaceBuilder)}.";
	private static readonly string ErrMsgUsingEqualityComparer = $"Unable to use a custom {typeof(IEqualityComparer<>).ToStringCS()} since a custom {nameof(ComparerFactory)} has been registered. Ensure all comparer registrations are made with your custom factory and not with this {nameof(ObjectSpaceBuilder)}.";
	private static readonly string ErrMsgUsingComparer = $"Unable to use a custom {typeof(IComparer<>).ToStringCS()} since a custom {nameof(ComparerFactory)} has been registered. Ensure all comparer registrations are made with your custom factory and not with this {nameof(ObjectSpaceBuilder)}.";

	private ObjectSpaceType? _type;
	private string _filepath;
	private MemoryStream _memoryStream;
	private string _pagesPath;
	private long _maxMemory;
	private long _pageSize;
	private int _clusterSize;
	private BTreeSpec _indexBTreeSpec;
	private bool _clusterSizeSpecified;
	private bool _clusteredStreamsPolicySpecified;
	private ObjectSpaceTraits _traits;
	private int _metaDataCapacity;
	private CHF _hashFunction;
	private ClusteredStreamsPolicy _clusteredStreamsPolicy;
	private FileAccessMode _accessMode;
	private IList<IObjectSpaceDimensionBuilder> _dimensions;
	private SerializerFactory _serializerFactory;
	private bool _usingCustomSerializerFactory;
	private bool _specifiedCustomSerializer;
	private ComparerFactory _comparerFactory;
	private bool _usingCustomComparerFactory;
	private bool _specifiedCustomComparer;

	/// <summary>Starts with Framework storage defaults and local copies of the default factories.</summary>
	public ObjectSpaceBuilder() {
		// Start with complete storage settings and one set of ObjectSpace behavior flags.
		_type = null;
		_filepath = null;
		_memoryStream = null;
		_pagesPath = Sphere10FrameworkDefaults.TransactionalPageFolder;
		_maxMemory = Sphere10FrameworkDefaults.ObjectSpaceMaxMemory;
		_pageSize = Sphere10FrameworkDefaults.ObjectSpacePageSize;
		_clusterSize = Sphere10FrameworkDefaults.ClusterSize;
		_indexBTreeSpec = new BTreeSpec(BTreeSpec.Default.Type, Sphere10FrameworkDefaults.ObjectSpaceIndexBTreeOrder);
		_traits = ObjectSpaceTraits.None;
		_hashFunction = Sphere10FrameworkDefaults.HashFunction;
		_clusteredStreamsPolicy = Sphere10FrameworkDefaults.ContainerPolicy;
		WithEndianness(Sphere10FrameworkDefaults.Endianness);
		_accessMode = FileAccessMode.OpenOrCreate;
		_dimensions = new List<IObjectSpaceDimensionBuilder>();

		// Keep application registrations local to this builder's factories.
		_serializerFactory = new SerializerFactory(SerializerFactory.Default);
		_usingCustomSerializerFactory = false;
		_specifiedCustomSerializer = false;

		_comparerFactory = new ComparerFactory(ComparerFactory.Default);
		_usingCustomComparerFactory = false;
		_specifiedCustomComparer = false;
	}

	/// <summary>Selects the transactional file backend at the supplied path.</summary>
	public ObjectSpaceBuilder UseFile(string filePath) {
		_type = ObjectSpaceType.FileMapped;
		_filepath = filePath;
		return this;
	}

	/// <summary>Selects memory storage, borrowing a supplied stream or creating a new one for each build.</summary>
	public ObjectSpaceBuilder UseMemoryStream(MemoryStream stream = null) {
		_type = ObjectSpaceType.MemoryMapped;
		_memoryStream = stream;
		return this;
	}

	/// <summary>Selects the directory used by the file backend's transactional pages.</summary>
	public ObjectSpaceBuilder KeepUncommittedPagesAt(string folder) {
		_pagesPath = folder;
		return this;
	}

	public ObjectSpaceBuilder UsingHashFunction(CHF hashFunction) {
		_hashFunction = hashFunction;
		return this;
	}

	public ObjectSpaceBuilder ReadOnly() {
		_accessMode.SetFlags(FileAccessMode.Append, false);
		_accessMode.SetFlags(FileAccessMode.Truncate, false);
		_accessMode.SetFlags(FileAccessMode.Write, false);
		return this;
	}

	public ObjectSpaceBuilder AutoLoad() {
		_accessMode.SetFlags(FileAccessMode.AutoLoad, true);
		return this;
	}

	public ObjectSpaceBuilder WithMaxMemory(long bytes) {
		_maxMemory = bytes;
		return this;
	}

	public ObjectSpaceBuilder WithPageSize(long bytes) {
		_pageSize = bytes;
		return this;
	}

	public ObjectSpaceBuilder WithClusterSize(int bytes) {
		_clusterSize = bytes;
		_clusterSizeSpecified = true;
		return this;
	}	

	public ObjectSpaceBuilder WithClusteredStreamsPolicy(ClusteredStreamsPolicy clusteredStreamsPolicy) {
		_clusteredStreamsPolicy = clusteredStreamsPolicy;
		_clusteredStreamsPolicySpecified = true;
		return this;
	}

	/// <summary>Sets the B-tree type and order for all member indexes. Preserve this specification when reopening storage.</summary>
	public ObjectSpaceBuilder WithIndexBTreeSpec(BTreeSpec spec) {
		Guard.ArgumentNotNull(spec, nameof(spec));
		_indexBTreeSpec = spec;
		return this;
	}

	/// <summary>Sets the B-tree order for all member indexes. Existing storage must be reopened with the same order.</summary>
	public ObjectSpaceBuilder WithIndexBTreeOrder(int order) {
		_indexBTreeSpec = new BTreeSpec(_indexBTreeSpec.Type, order);
		return this;
	}

	/// <summary>Selects the type for all member indexes. Preserve the selection on reopen.</summary>
	public ObjectSpaceBuilder WithIndexBTreeType(BTreeType type) {
		_indexBTreeSpec = new BTreeSpec(type, _indexBTreeSpec.Order);
		return this;
	}

	/// <summary>Sets the byte order for storage and metadata through the ObjectSpace endian trait.</summary>
	public ObjectSpaceBuilder WithEndianness(Endianness endianness) {
		if (!Enum.IsDefined(endianness))
			throw new ArgumentOutOfRangeException(nameof(endianness));
		_traits = endianness == Endianness.BigEndian ? _traits | ObjectSpaceTraits.BigEndian : _traits & ~ObjectSpaceTraits.BigEndian;
		return this;
	}

	/// <summary>Stores metadata when creating an object space. Existing storage keeps its original layout.</summary>
	public ObjectSpaceBuilder StoreMetaData(int capacity = 0) {
		Guard.ArgumentNotNegative(capacity, nameof(capacity));
		_traits |= ObjectSpaceTraits.MetaData;
		_metaDataCapacity = capacity;
		return this;
	}

	/// <summary>Enables the trait that saves tracked dirty objects during flush.</summary>
	public ObjectSpaceBuilder AutoSave() {
		_traits |= ObjectSpaceTraits.AutoSave;
		return this;
	}

	/// <summary>
	/// Enables reference-counting garbage collection. Non-root dimension objects with zero
	/// incoming references will be automatically collected on save/delete and via explicit <c>CollectGarbage()</c>.
	/// </summary>
	public ObjectSpaceBuilder WithGarbageCollection() {
		_traits |= ObjectSpaceTraits.GarbageCollect;
		return this;
	}

	/// <summary>Enables ObjectSpace Merkle aggregation and adds a Merkle attachment to each configured dimension.</summary>
	public ObjectSpaceBuilder Merkleized() {
		_traits |= ObjectSpaceTraits.Merkleized;
		foreach(var dimension in _dimensions) {
			if (!dimension.Indexes.Any(x => x.Type == ObjectSpaceDefinition.IndexType.MerkleTree))
				dimension.Merkleized();
		}
		return this;
	}

	public ObjectSpaceBuilder UsingSerializerFactory(SerializerFactory factory) {
		Guard.ArgumentNotNull(factory, nameof(factory));
		Guard.Against(_specifiedCustomSerializer, ErrMsgUseSerializerFactory);
		_serializerFactory = factory;
		_usingCustomSerializerFactory = true;
		return this;
	}

	public bool ContainsSerializer<TItem>() => ContainsSerializer(typeof(TItem));

	public bool ContainsSerializer(Type type) => _serializerFactory.ContainsSerializer(type);

	public ObjectSpaceBuilder UsingSerializer<TItem, TItemSerializer>() where TItemSerializer : IItemSerializer<TItem>, new() {
		Guard.Against(_usingCustomSerializerFactory, ErrMsgUsingSerializer);
		_specifiedCustomSerializer = true;
		_serializerFactory.Register<TItem, TItemSerializer>();
		return this;
	}

	public ObjectSpaceBuilder UsingSerializer<TItem>(IItemSerializer<TItem> serializer) {
		Guard.Against(_usingCustomSerializerFactory, ErrMsgUsingSerializer);
		_specifiedCustomSerializer = true;
		_serializerFactory.Register(serializer);
		return this;
	}

	public ObjectSpaceBuilder MakeSerializer<TItem>() => MakeSerializer(typeof(TItem));

	public ObjectSpaceBuilder MakeSerializer(Type type) {
		Guard.Ensure(!_serializerFactory.ContainsSerializer(type), $"Serializer for type {type.ToStringCS()} has already been registered.");
		_specifiedCustomSerializer = true;
		_serializerFactory.RegisterAutoBuild(type);
		return this;
	}

	public ObjectSpaceBuilder UsingComparerFactory(ComparerFactory factory) {
		Guard.ArgumentNotNull(factory, nameof(factory));
		Guard.Against(_specifiedCustomComparer, ErrMsgUseComparerFactory);
		_comparerFactory = factory;
		_usingCustomComparerFactory = true;
		return this;
	}

	public ObjectSpaceBuilder UsingEqualityComparer<TItem, TItemComparer>() where TItemComparer : IEqualityComparer<TItem>, new() {
		Guard.Against(_usingCustomComparerFactory, ErrMsgUsingEqualityComparer);
		_specifiedCustomComparer = true;
		_comparerFactory.RegisterEqualityComparer<TItem, TItemComparer>();
		return this;
	}

	public ObjectSpaceBuilder UsingEqualityComparer<TItem>(IEqualityComparer<TItem> comparer) {
		Guard.Against(_usingCustomComparerFactory, ErrMsgUsingEqualityComparer);
		_specifiedCustomComparer = true;
		_comparerFactory.RegisterEqualityComparer(comparer);
		return this;
	}

	public ObjectSpaceBuilder UsingEqualityComparer(Type type, object comparer) {
		Guard.Against(_usingCustomComparerFactory, ErrMsgUsingComparer);
		_specifiedCustomComparer = true;
		_comparerFactory.RegisterEqualityComparer(type, comparer);
		return this;
	}

	public ObjectSpaceBuilder UsingComparer<TItem, TComparer>() where TComparer : IComparer<TItem>, new() {
		Guard.Against(_usingCustomComparerFactory, ErrMsgUsingComparer);
		_specifiedCustomComparer = true;
		_comparerFactory.RegisterComparer<TItem, TComparer>();
		return this;
	}

	public ObjectSpaceBuilder UsingComparer<TItem>(IComparer<TItem> comparer) {
		Guard.Against(_usingCustomComparerFactory, ErrMsgUsingComparer);
		_specifiedCustomComparer = true;
		_comparerFactory.RegisterComparer(comparer);
		return this;
	}

	public ObjectSpaceBuilder UsingComparer(Type type, object comparer) {
		Guard.Against(_usingCustomComparerFactory, ErrMsgUsingComparer);
		_specifiedCustomComparer = true;
		_comparerFactory.RegisterComparer(type, comparer);
		return this;
	}

	public ObjectSpaceDimensionBuilder<T> AddDimension<T>(bool ignoreAnnotations = false) 
		=> (ObjectSpaceDimensionBuilder<T>)AddDimension(typeof(T), ignoreAnnotations);

	public IObjectSpaceDimensionBuilder AddDimension(Type type, bool ignoreAnnotations = false) {
		var dimensionBuilder = (IObjectSpaceDimensionBuilder)typeof(ObjectSpaceDimensionBuilder<>).MakeGenericType(type).ActivateWithCompatibleArgs(this);
		_dimensions.Add(dimensionBuilder);

		if (!ignoreAnnotations) {

			// Check for [Root] attribute — marks the dimension as a GC root so its objects are never auto-collected
			if (type.TryGetCustomAttributeOfType<RootAttribute>(false, out _))
				dimensionBuilder.AsRoot();

			if (type.TryGetCustomAttributeOfType<EqualityComparerAttribute>(false, out var equalityComparerAttribute))
				dimensionBuilder.UsingEqualityComparer(equalityComparerAttribute.EqualityComparerType.ActivateWithCompatibleArgs());

			foreach(var member in SerializerBuilder.GetSerializableMembers(type)) {
				if (member.MemberInfo.TryGetCustomAttributeOfType<IdentityAttribute>(false, out var identityAttribute)) 
					dimensionBuilder.WithIdentifier(member, identityAttribute.IndexName); 

				if (member.MemberInfo.TryGetCustomAttributeOfType<IndexAttribute>(false, out var indexAttribute)) 
					dimensionBuilder.WithIndexOn(member, indexAttribute.IndexName, indexAttribute.NullPolicy); 

				if (member.MemberInfo.TryGetCustomAttributeOfType<UniqueIndexAttribute>(false, out var uniqueIndexAttribute)) 
					dimensionBuilder.WithUniqueIndexOn(member, uniqueIndexAttribute.IndexName, uniqueIndexAttribute.NullPolicy); 

			}
		}
		return dimensionBuilder;
	}

	public ObjectSpaceDimensionBuilder<T> Configure<T>() {
		var dim = _dimensions.FirstOrDefault(x => x.ItemType == typeof(T));
		if (dim is null)
			throw new InvalidOperationException($"No dimension for type {typeof(T).ToStringCS()} was found");
		return (ObjectSpaceDimensionBuilder<T>)dim;
	}	

	/// <summary>Constructs a fresh ObjectSpace using the configured backend and dimension definitions.</summary>
	public ObjectSpace Build() {
		// Use the caller's configured settings and the selected backend's construction defaults.
		Guard.Ensure(_type.HasValue, "Object Space type has not been specified (file or memory mapped).");
		var selectedType = _type.Value;
		var definition = BuildDefinition();
		var endianness = _traits.HasFlag(ObjectSpaceTraits.BigEndian) ? Endianness.BigEndian : Endianness.LittleEndian;
		var clusterSize = _clusterSizeSpecified ? _clusterSize
			: selectedType == ObjectSpaceType.MemoryMapped ? Sphere10FrameworkDefaults.MemoryObjectSpaceClusterSize : Sphere10FrameworkDefaults.FileObjectSpaceClusterSize;
		var policy = _clusteredStreamsPolicySpecified ? _clusteredStreamsPolicy
			: selectedType == ObjectSpaceType.MemoryMapped ? Sphere10FrameworkDefaults.MemoryObjectSpaceContainerPolicy : Sphere10FrameworkDefaults.FileObjectSpaceContainerPolicy;
		ObjectSpace space;
		switch (selectedType) {
			case ObjectSpaceType.FileMapped:
				var descriptor = Sphere10FrameworkFileDescriptor.From(_filepath, _pagesPath, _pageSize, _maxMemory, clusterSize, policy, endianness);
				space = new FileObjectSpace(descriptor, definition, _serializerFactory, _comparerFactory, _accessMode);
				break;
			case ObjectSpaceType.MemoryMapped:
				if (_clusterSizeSpecified) {
					space = _memoryStream is null
						? new MemoryObjectSpace(definition, _serializerFactory, _comparerFactory, clusterSize, policy, endianness)
						: new MemoryObjectSpace(_memoryStream, definition, _serializerFactory, _comparerFactory, clusterSize, policy, endianness);
				} else {
					space = _memoryStream is null
						? new MemoryObjectSpace(definition, _serializerFactory, _comparerFactory, policy, endianness)
						: new MemoryObjectSpace(_memoryStream, definition, _serializerFactory, _comparerFactory, policy, endianness);
				}
				break;
			default:
				throw new ArgumentOutOfRangeException();
		}
		return space;
	}

	/// <summary>Builds an independent, validated definition with complete per-index tree specifications.</summary>
	public ObjectSpaceDefinition BuildDefinition() {
		// Compose the shared specification and flags without duplicating individual trait booleans.
		var definition = new ObjectSpaceDefinition {
			Traits = _traits,
			HashFunction = _hashFunction,
			MetaDataCapacity = _metaDataCapacity,
			IndexBTreeSpec = _indexBTreeSpec,
			Dimensions = _dimensions.Select(x => x.BuildDefinition()).ToArray()
		};

		// Validate and resolve complete per-index layouts before constructing storage.
		definition.Validate().ThrowOnFailure();
		definition.ResolveIndexBTreeSpecs();
		return definition;
	}

	/// <summary>Builds an unloaded ObjectSpace over clustered storage whose metadata has already been parsed.</summary>
	internal ObjectSpace Build(ClusteredStreams streams) {
		// Storage helpers perform metadata configuration and loading after construction.
		return new ObjectSpace(streams, BuildDefinition(), _serializerFactory, _comparerFactory,
			FileAccessMode.Default, Sphere10FrameworkDefaults.MemoryObjectSpaceDimensionClusterSize);
	}

	/// <summary>The backend selected for ordinary ObjectSpace construction.</summary>
	private enum ObjectSpaceType {
		FileMapped,

		MemoryMapped
	}

}


