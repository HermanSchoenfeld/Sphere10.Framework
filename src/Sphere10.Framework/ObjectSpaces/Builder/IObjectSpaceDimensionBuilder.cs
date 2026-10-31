// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using Sphere10.Framework.Mapping;

namespace Sphere10.Framework.ObjectSpaces;

public interface IObjectSpaceDimensionBuilder {

	Type ItemType { get; }

	IEnumerable<ObjectSpaceDefinition.IndexDefinition> Indexes { get; }

	/// <summary>
	/// Marks this dimension as a GC root. Root objects are user-managed and never auto-collected.
	/// </summary>
	IObjectSpaceDimensionBuilder AsRoot();

	IObjectSpaceDimensionBuilder WithRecyclableIndexes();

	IObjectSpaceDimensionBuilder Merkleized();

	IObjectSpaceDimensionBuilder UsingComparer(object comparer);

	IObjectSpaceDimensionBuilder UsingEqualityComparer(object comparer);

	IObjectSpaceDimensionBuilder WithIdentifier(Member member, string indexName = null);

	IObjectSpaceDimensionBuilder WithIndexOn(Member member, string indexName = null, IndexNullPolicy nullPolicy = IndexNullPolicy.IgnoreNull);

	IObjectSpaceDimensionBuilder WithUniqueIndexOn(Member member, string indexName = null, IndexNullPolicy nullPolicy = IndexNullPolicy.IgnoreNull);

	IObjectSpaceDimensionBuilder WithChangeTrackingVia(Member member);

	/// <summary>Replaces attachment definitions in their supplied physical order.</summary>
	IObjectSpaceDimensionBuilder WithIndexes(ObjectSpaceDefinition.IndexDefinition[] indexes);

	IObjectSpaceDimensionBuilder OptimizeAssumingAverageItemSize(int bytes);

	ObjectSpaceDefinition.DimensionDefinition BuildDefinition();

	ObjectSpaceBuilder Done(); 

}

