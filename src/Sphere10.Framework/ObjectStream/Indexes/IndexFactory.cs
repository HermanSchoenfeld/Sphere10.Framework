// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Reflection;
using Sphere10.Framework.Mapping;

namespace Sphere10.Framework;

/// <summary>
/// Utility helpers for constructing projection and checksum indexes over <see cref="ObjectStream"/> instances.
/// </summary>
internal static class IndexFactory {

	/// <summary>
	/// Extracts the item type T from an ObjectStream&lt;T&gt; instance.
	/// Walks the type hierarchy to find the closed generic ObjectStream&lt;T&gt;.
	/// </summary>
	private static Type GetObjectStreamItemType(ObjectStream objectStream) {
		var type = objectStream.GetType();
		while (type != null) {
			if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ObjectStream<>))
				return type.GetGenericArguments()[0];
			type = type.BaseType;
		}
		throw new InvalidOperationException($"Cannot determine item type from {objectStream.GetType().ToStringCS()}");
	}

	/// <summary>
	/// Gets a property projection delegate typed as Func&lt;TItem, TKey&gt; where TItem is the
	/// dimension's item type. When the member is declared on a base class, wraps the original
	/// Func&lt;TBase, TKey&gt; into a Func&lt;TDerived, TKey&gt;.
	/// </summary>
	private static Delegate GetProjectionForItemType(Member member, Type itemType) {
		if (member.DeclaringType == itemType)
			return member.AsDelegate();

		// member.AsDelegate() returns Func<DeclaringType, PropertyType>.
		// We need Func<itemType, PropertyType> — wrap via a lambda that casts.
		var baseDelegate = member.AsDelegate();
		var wrapperMethod = typeof(IndexFactory)
			.GetMethod(nameof(WrapProjection), BindingFlags.NonPublic | BindingFlags.Static)
			.MakeGenericMethod(member.DeclaringType, itemType, member.PropertyType);
		return (Delegate)wrapperMethod.Invoke(null, new object[] { baseDelegate });
	}

	/// <summary>
	/// Wraps a Func&lt;TBase, TKey&gt; into a Func&lt;TDerived, TKey&gt;
	/// where TDerived : TBase.
	/// </summary>
	private static Func<TDerived, TKey> WrapProjection<TBase, TDerived, TKey>(Func<TBase, TKey> baseProjection)
		where TDerived : TBase
		=> item => baseProjection(item);
	
	#region Prjection Index

	internal static IClusteredStreamsAttachment CreateMemberIndex(
		ObjectStream objectStream, 
		string indexName,
		Member member, 
		IItemSerializer keySerializer = null,
		object keyComparer = null) {
		var itemType = GetObjectStreamItemType(objectStream);
		Guard.Ensure(itemType.IsAssignableFrom(member.DeclaringType) || member.DeclaringType.IsAssignableFrom(itemType));
		
		var projection = GetProjectionForItemType(member, itemType);

		var genericContainerType = typeof(ObjectStream<>).MakeGenericType(itemType);
		var genericContainer = Convert.ChangeType(objectStream, genericContainerType);


		// Use MakeGenericMethod to call the generic method with the specific types
		var method = typeof(IndexFactory)
			.GetMethod(nameof(CreateProjectionIndex), BindingFlags.NonPublic | BindingFlags.Static)
			.MakeGenericMethod(itemType, member.PropertyType);

		return (IClusteredStreamsAttachment)method.Invoke(null, new object[] { genericContainer, indexName, projection, keySerializer, keyComparer });

	}

	internal static ProjectionIndex<TItem, TKey> CreateProjectionIndex<TItem, TKey>(
		ObjectStream<TItem> objectStream,
		string indexName,
		Func<TItem, TKey> projection,
		IItemSerializer<TKey> keySerializer = null,
		IEqualityComparer<TKey> keyComparer = null) {
		keySerializer ??= ItemSerializer<TKey>.Default;
		var keyChecksumKeyIndex = new ProjectionIndex<TItem, TKey>(objectStream, indexName, projection, keySerializer, keyComparer);
		return keyChecksumKeyIndex;
	}

	#endregion

	#region Unique Projection Index

	internal static IClusteredStreamsAttachment CreateUniqueMemberIndex(ObjectStream objectStream, string indexName, Member member, IItemSerializer keySerializer = null, object keyComparer = null) {
		var itemType = GetObjectStreamItemType(objectStream);
		Guard.Ensure(itemType.IsAssignableFrom(member.DeclaringType) || member.DeclaringType.IsAssignableFrom(itemType));
		
		var projection = GetProjectionForItemType(member, itemType);

		var genericContainerType = typeof(ObjectStream<>).MakeGenericType(itemType);
		var genericContainer = Convert.ChangeType(objectStream, genericContainerType);


		// Use MakeGenericMethod to call the generic method with the specific types
		var method = typeof(IndexFactory)
			.GetMethod(nameof(CreateUniqueProjectionIndex), BindingFlags.NonPublic | BindingFlags.Static)
			.MakeGenericMethod(itemType, member.PropertyType);

		return (IClusteredStreamsAttachment)method.Invoke(null, new object[] { genericContainer, indexName, projection, keySerializer, keyComparer });

	}

	internal static UniqueProjectionIndex<TItem, TKey> CreateUniqueProjectionIndex<TItem, TKey>(ObjectStream<TItem> objectStream, string indexName, Func<TItem, TKey> projection, IItemSerializer<TKey> keySerializer = null, IEqualityComparer<TKey> keyComparer = null) {
		keySerializer ??= ItemSerializer<TKey>.Default;
		keyComparer ??= EqualityComparer<TKey>.Default; // TODO: should this use a ComparerFactory?
		var keyChecksumKeyIndex = new UniqueProjectionIndex<TItem, TKey>(objectStream, indexName, projection, keySerializer, keyComparer);
		return keyChecksumKeyIndex;
	}

	#endregion

	#region Projection Checksum Index

	internal static IClusteredStreamsAttachment CreateMemberChecksumIndex(
		ObjectStream objectStream, 
		string indexName, 
		Member member, 
		IItemSerializer keySerializer = null, 
		object keyChecksummer = null, 
		object keyFetcher = null, 
		object keyComparer = null,
		IndexNullPolicy indexNullPolicy = IndexNullPolicy.IgnoreNull
	) {
		var itemType = GetObjectStreamItemType(objectStream);
		Guard.Ensure(itemType.IsAssignableFrom(member.DeclaringType) || member.DeclaringType.IsAssignableFrom(itemType));
		
		var projection = GetProjectionForItemType(member, itemType);

		var genericContainerType = typeof(ObjectStream<>).MakeGenericType(itemType);
		var genericContainer = Convert.ChangeType(objectStream, genericContainerType);

		// Use MakeGenericMethod to call the generic method with the specific types
		var method = typeof(IndexFactory)
			.GetMethod(nameof(CreateProjectionChecksumIndex), BindingFlags.NonPublic | BindingFlags.Static)
			.MakeGenericMethod(itemType, member.PropertyType);

		return (IClusteredStreamsAttachment)method.Invoke(null, new object[] { genericContainer, indexName, projection, keySerializer, keyChecksummer, keyFetcher, keyComparer, indexNullPolicy });

	}

	internal static ProjectionChecksumIndex<TItem, TKey> CreateProjectionChecksumIndex<TItem, TKey>(
		ObjectStream<TItem> objectStream,
		string indexName, 
		Func<TItem, TKey> projection, 
		IItemSerializer<TKey> keySerializer = null, 
		IItemChecksummer<TKey> keyChecksummer = null, 
		Func<long, TKey> keyFetcher = null, 
		IEqualityComparer<TKey> keyComparer= null,
		IndexNullPolicy indexNullPolicy = IndexNullPolicy.IgnoreNull
	) {

		keySerializer ??= ItemSerializer<TKey>.Default;
		keyChecksummer ??= new ItemDigestor<TKey>(keySerializer, objectStream.Streams.Endianness);
		keyFetcher ??= x => projection(objectStream.LoadItem(x));
		keyComparer ??= EqualityComparer<TKey>.Default;
		var keyChecksumKeyIndex = new ProjectionChecksumIndex<TItem, TKey>(
			objectStream,
			indexName,
			projection,
			keyChecksummer,
			keyFetcher,
			keyComparer,
			indexNullPolicy
		);

		return keyChecksumKeyIndex;
	}

	#endregion

	#region Unique Projection Checksum Index

	internal static IClusteredStreamsAttachment CreateUniqueMemberChecksumIndex(
		ObjectStream objectStream, 
		string indexName, 
		Member member, 
		IItemSerializer keySerializer = null,
		object keyChecksummer = null, 
		object keyFetcher = null, 
		object keyComparer = null,
		IndexNullPolicy indexNullPolicy = IndexNullPolicy.IgnoreNull
	) {
		var itemType = GetObjectStreamItemType(objectStream);
		Guard.Ensure(itemType.IsAssignableFrom(member.DeclaringType) || member.DeclaringType.IsAssignableFrom(itemType));
		
		var projection = GetProjectionForItemType(member, itemType);

		var genericContainerType = typeof(ObjectStream<>).MakeGenericType(itemType);
		var genericContainer = Convert.ChangeType(objectStream, genericContainerType);

		// Use MakeGenericMethod to call the generic method with the specific types
		var method = typeof(IndexFactory)
			.GetMethod(nameof(CreateUniqueProjectionChecksumIndex), BindingFlags.NonPublic | BindingFlags.Static)
			.MakeGenericMethod(itemType, member.PropertyType);

		return (IClusteredStreamsAttachment)method.Invoke(null, new object[] { genericContainer, indexName, projection, keySerializer, keyChecksummer, keyFetcher, keyComparer, indexNullPolicy });

	}

	internal static UniqueProjectionChecksumIndex<TItem, TKey> CreateUniqueProjectionChecksumIndex<TItem, TKey>(
		ObjectStream<TItem> objectStream,
		string indexName, 
		Func<TItem, TKey> projection, 
		IItemSerializer<TKey> keySerializer = null, 
		IItemChecksummer<TKey> keyChecksummer = null, 
		Func<long, TKey> keyFetcher = null, 
		IEqualityComparer<TKey> keyComparer = null,
		IndexNullPolicy indexNullPolicy = IndexNullPolicy.IgnoreNull
	) {
		keySerializer ??= ItemSerializer<TKey>.Default;
		keyChecksummer ??= new ItemDigestor<TKey>(keySerializer, objectStream.Streams.Endianness);
		keyFetcher ??= x => projection(objectStream.LoadItem(x));
		keyComparer ??= EqualityComparer<TKey>.Default;
		var uniqueKeyChecksumIndex = new UniqueProjectionChecksumIndex<TItem, TKey>(
			objectStream,
			indexName,
			projection,
			keyChecksummer,
			keyFetcher,
			keyComparer,
			indexNullPolicy
		);

		return uniqueKeyChecksumIndex;
	}

	#endregion

	#region RecyclableIndex Index

	internal static RecyclableIndexIndex CreateRecyclableIndexIndex(ObjectStream objectStream, string indexName) {
		var keyChecksumKeyIndex = new RecyclableIndexIndex(objectStream, indexName);
		return keyChecksumKeyIndex;
	}

	#endregion
}

