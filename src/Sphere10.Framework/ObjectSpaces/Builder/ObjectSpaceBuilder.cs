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
	private int _indexBTreeOrder;
	private bool _indexBTreeOrderSpecified;
	private bool _clusterSizeSpecified;
	private bool _clusteredStreamsPolicySpecified;
	private ObjectSpaceType? _boundType;
	private ObjectSpaceDefinition _boundDefinition;
	private Sphere10FrameworkFileDescriptor _boundFileDescriptor;
	private MemoryStream _boundMemoryStream;
	private Dictionary<Type, int> _boundDimensionClusterSizes = new(TypeEquivalenceComparer.Instance);
	private int _boundClusterSize;
	private ClusteredStreamsPolicy _boundPolicy;
	private bool _merkleized;
	private CHF _hashFunction;
	private ClusteredStreamsPolicy _clusteredStreamsPolicy;
	private Endianness _endianness;
	private FileAccessMode _accessMode;
	private IList<IObjectSpaceDimensionBuilder> _dimensions;
	private SerializerFactory _serializerFactory;
	private bool _usingCustomSerializerFactory;
	private bool _specifiedCustomSerializer;
	private ComparerFactory _comparerFactory;
	private bool _usingCustomComparerFactory;
	private bool _specifiedCustomComparer;
	private bool _autoSave;
	private bool _garbageCollect;

	public ObjectSpaceBuilder() {
		_type = null;
		_filepath = null;
		_memoryStream = null;
		_pagesPath = Sphere10FrameworkDefaults.TransactionalPageFolder;
		_maxMemory = Sphere10FrameworkDefaults.ObjectSpaceMaxMemory;
		_pageSize = Sphere10FrameworkDefaults.ObjectSpacePageSize;
		_clusterSize = Sphere10FrameworkDefaults.ClusterSize;
		_indexBTreeOrder = Sphere10FrameworkDefaults.ObjectSpaceIndexBTreeOrder;
		_merkleized = false;
		_hashFunction = Sphere10FrameworkDefaults.HashFunction;
		_clusteredStreamsPolicy = Sphere10FrameworkDefaults.ContainerPolicy;
		_endianness = Sphere10FrameworkDefaults.Endianness;
		_accessMode = FileAccessMode.OpenOrCreate;
		_dimensions = new List<IObjectSpaceDimensionBuilder>();

		_serializerFactory = new SerializerFactory(SerializerFactory.Default);
		_usingCustomSerializerFactory = false;
		_specifiedCustomSerializer = false;

		_comparerFactory = new ComparerFactory(ComparerFactory.Default);
		_usingCustomComparerFactory = false;
		_specifiedCustomComparer = false;

		_autoSave = false;
		_garbageCollect = false;
	}

	public ObjectSpaceBuilder UseFile(string filePath) {
		_type = ObjectSpaceType.FileMapped;
		_filepath = filePath;
		if (!MatchesBoundStorage())
			ResetBoundConfiguration();
		return this;
	}

	public ObjectSpaceBuilder UseMemoryStream(MemoryStream stream = null) {
		_type = ObjectSpaceType.MemoryMapped;
		_memoryStream = stream;
		if (!MatchesBoundStorage())
			ResetBoundConfiguration();
		return this;
	}

	public ObjectSpaceBuilder KeepUncommittedPagesAt(string folder) {
		_pagesPath = folder;
		if (!MatchesBoundStorage())
			ResetBoundConfiguration();
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

	/// <summary>Sets the B-tree order for all member indexes. Existing storage must be reopened with the same order.</summary>
	public ObjectSpaceBuilder WithIndexBTreeOrder(int order) {
		Guard.ArgumentGTE(order, 3, nameof(order));
		_indexBTreeOrder = order;
		_indexBTreeOrderSpecified = true;
		return this;
	}

	public ObjectSpaceBuilder WithEndianness(Endianness endianness) {
		_endianness = endianness;
		return this;
	}

	public ObjectSpaceBuilder AutoSave() {
		_autoSave = true;
		return this;
	}

	/// <summary>
	/// Enables reference-counting garbage collection. Non-root dimension objects with zero
	/// incoming references will be automatically collected on save/delete and via explicit <c>CollectGarbage()</c>.
	/// </summary>
	public ObjectSpaceBuilder WithGarbageCollection() {
		_garbageCollect = true;
		return this;
	}

	public ObjectSpaceBuilder Merkleized() {
		_merkleized = true;
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
	
	public ObjectSpaceDefinition BuildDefinition() {
		var definition = new ObjectSpaceDefinition(_merkleized, _autoSave) {
			HashFunction = _hashFunction,
			Dimensions = _dimensions.Select(x => x.BuildDefinition()).ToArray()
		};
		if (_indexBTreeOrderSpecified)
			definition.IndexBTreeOrder = _indexBTreeOrder;
		else if (MatchesBoundStorage())
			definition.IndexBTreeOrder = _boundDefinition.IndexBTreeOrder;
		if (MatchesBoundStorage()) {
			foreach (var dimension in definition.Dimensions)
				if (!dimension.AverageObjectSizeBytes.HasValue && _boundDimensionClusterSizes.TryGetValue(dimension.ObjectType, out var boundSize))
					dimension.AverageObjectSizeBytes = boundSize;
		}
		// Wire in the GarbageCollect trait if enabled via the builder
		if (_garbageCollect)
			definition.Traits |= ObjectSpaceTraits.GarbageCollect;
		definition.Validate().ThrowOnFailure();
		return definition;
	}

	public ObjectSpace Build() {
		Guard.Ensure(_type.HasValue, "Object Space type has not been specified (file or memory mapped).");
		var selectedType = _type.Value;
		var definition = BuildDefinition();
		var retained = MatchesBoundStorage();
		var clusterSize = _clusterSizeSpecified ? _clusterSize : retained ? _boundClusterSize
			: selectedType == ObjectSpaceType.MemoryMapped ? Sphere10FrameworkDefaults.MemoryObjectSpaceClusterSize : Sphere10FrameworkDefaults.FileObjectSpaceClusterSize;
		var policy = _clusteredStreamsPolicySpecified ? _clusteredStreamsPolicy : retained ? _boundPolicy
			: selectedType == ObjectSpaceType.MemoryMapped ? Sphere10FrameworkDefaults.MemoryObjectSpaceContainerPolicy : Sphere10FrameworkDefaults.FileObjectSpaceContainerPolicy;
		ObjectSpace space;
		switch (selectedType) {
			case ObjectSpaceType.FileMapped:
				var descriptor = Sphere10FrameworkFileDescriptor.From(_filepath, _pagesPath, _pageSize, _maxMemory, clusterSize, policy, _endianness);
				space = new FileObjectSpace(descriptor, definition, _serializerFactory, _comparerFactory, _accessMode);
				break;
			case ObjectSpaceType.MemoryMapped:
				if (_clusterSizeSpecified || retained) {
					space = _memoryStream is null
						? new MemoryObjectSpace(definition, _serializerFactory, _comparerFactory, clusterSize, policy, _endianness)
						: new MemoryObjectSpace(_memoryStream, definition, _serializerFactory, _comparerFactory, clusterSize, policy, _endianness);
				} else {
					space = _memoryStream is null
						? new MemoryObjectSpace(definition, _serializerFactory, _comparerFactory, policy, _endianness)
						: new MemoryObjectSpace(_memoryStream, definition, _serializerFactory, _comparerFactory, policy, _endianness);
				}
				break;
			default:
				throw new ArgumentOutOfRangeException();
		}
		if (space.RequiresLoad) {
			EventHandlerEx<object> onLoaded = null;
			onLoaded = _ => {
				CaptureBoundConfiguration(space, selectedType);
				space.Loaded -= onLoaded;
			};
			space.Loaded += onLoaded;
		} else {
			CaptureBoundConfiguration(space, selectedType);
		}
		return space;
	}

	private bool MatchesBoundStorage() {
		if (_boundType != _type || _boundDefinition is not { HasBoundIndexBTreeOrder: true })
			return false;
		if (_type == ObjectSpaceType.MemoryMapped)
			return _memoryStream != null && ReferenceEquals(_memoryStream, _boundMemoryStream);
		if (_type != ObjectSpaceType.FileMapped || _boundFileDescriptor == null || string.IsNullOrWhiteSpace(_filepath))
			return false;
		var selected = TransactionalFileDescriptor.From(_filepath, _pagesPath);
		// Transaction marker IDs are based on the exact full path; case changes conservatively require declared settings.
		return string.Equals(Path.GetFullPath(_filepath), _boundFileDescriptor.CaseCorrectPath, StringComparison.Ordinal)
			&& string.Equals(TransactionalFileMappedListBase<byte>.GetPagesDirectoryPath(selected),
				TransactionalFileMappedListBase<byte>.GetPagesDirectoryPath(_boundFileDescriptor), StringComparison.Ordinal);
	}

	private void ResetBoundConfiguration() {
		_boundType = null;
		_boundDefinition = null;
		_boundFileDescriptor = null;
		_boundMemoryStream = null;
		_boundDimensionClusterSizes.Clear();
	}

	private void CaptureBoundConfiguration(ObjectSpace space, ObjectSpaceType selectedType) {
		if (space.RequiresLoad || !space.Definition.HasBoundIndexBTreeOrder)
			return;
		if (selectedType == ObjectSpaceType.MemoryMapped && _memoryStream == null)
			return;
		_boundType = selectedType;
		_boundDefinition = space.Definition;
		_boundClusterSize = space.Streams.Header.ClusterSize;
		_boundPolicy = space.Streams.Policy;
		_boundDimensionClusterSizes = space.Dimensions.ToDictionary(dimension => dimension.Definition.ObjectType,
			dimension => dimension.Container.ObjectStream.Streams.Header.ClusterSize, TypeEquivalenceComparer.Instance);
		if (space is FileObjectSpace file) {
			_boundFileDescriptor = file.File;
			_boundMemoryStream = null;
		} else {
			_boundMemoryStream = ((MemoryObjectSpace)space).Stream;
			_boundFileDescriptor = null;
		}
	}

	private enum ObjectSpaceType {
		FileMapped,
		MemoryMapped
	}

}


