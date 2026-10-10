// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using Sphere10.Framework.Mapping;

namespace Sphere10.Framework.ObjectSpaces;

/// <summary>Retains the fluent builder type and supplies the non-generic dimension-builder adapters.</summary>
public interface IObjectSpaceDimensionBuilder<out TBuilder> : IObjectSpaceDimensionBuilder
	where TBuilder : IObjectSpaceDimensionBuilder {
	new TBuilder AsRoot();

	TBuilder WithRecyclableIndexes(string indexName = null);

	TBuilder Merkleized(string indexName = null);

	new TBuilder UsingComparer(object comparer);

	new TBuilder UsingEqualityComparer(object comparer);

	new TBuilder WithIdentifier(Member member, string indexName = null);

	new TBuilder WithIndexOn(Member member, string indexName = null, IndexNullPolicy nullPolicy = IndexNullPolicy.IgnoreNull);

	new TBuilder WithUniqueIndexOn(Member member, string indexName = null, IndexNullPolicy nullPolicy = IndexNullPolicy.IgnoreNull);

	new TBuilder OptimizeAssumingAverageItemSize(int bytes);

	#region Base Interface Bridges

	IObjectSpaceDimensionBuilder IObjectSpaceDimensionBuilder.AsRoot() => AsRoot();

	IObjectSpaceDimensionBuilder IObjectSpaceDimensionBuilder.WithRecyclableIndexes() => WithRecyclableIndexes(null);

	IObjectSpaceDimensionBuilder IObjectSpaceDimensionBuilder.Merkleized() => Merkleized(null);

	IObjectSpaceDimensionBuilder IObjectSpaceDimensionBuilder.UsingComparer(object comparer) => UsingComparer(comparer);

	IObjectSpaceDimensionBuilder IObjectSpaceDimensionBuilder.UsingEqualityComparer(object comparer) => UsingEqualityComparer(comparer);

	IObjectSpaceDimensionBuilder IObjectSpaceDimensionBuilder.WithIdentifier(Member member, string indexName) => WithIdentifier(member, indexName);

	IObjectSpaceDimensionBuilder IObjectSpaceDimensionBuilder.WithIndexOn(Member member, string indexName, IndexNullPolicy nullPolicy) => WithIndexOn(member, indexName, nullPolicy);

	IObjectSpaceDimensionBuilder IObjectSpaceDimensionBuilder.WithUniqueIndexOn(Member member, string indexName, IndexNullPolicy nullPolicy) => WithUniqueIndexOn(member, indexName, nullPolicy);

	IObjectSpaceDimensionBuilder IObjectSpaceDimensionBuilder.OptimizeAssumingAverageItemSize(int bytes) => OptimizeAssumingAverageItemSize(bytes);

	#endregion

}
