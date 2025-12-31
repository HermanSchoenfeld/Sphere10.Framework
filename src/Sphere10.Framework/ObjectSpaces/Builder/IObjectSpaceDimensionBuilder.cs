// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using Sphere10.Framework.Mapping;
using System;
using System.Collections.Generic;

namespace Sphere10.Framework.ObjectSpaces;

public interface IObjectSpaceDimensionBuilder {

	Type ItemType { get; }

	IEnumerable<ObjectSpaceDefinition.IndexDefinition> Indexes { get; }

	IObjectSpaceDimensionBuilder WithRecyclableIndexes();

	IObjectSpaceDimensionBuilder Merkleized();

	IObjectSpaceDimensionBuilder UsingComparer(object comparer);

	IObjectSpaceDimensionBuilder UsingEqualityComparer(object comparer);

	IObjectSpaceDimensionBuilder WithIdentifier(Member member, string indexName = null);

	IObjectSpaceDimensionBuilder WithIndexOn(Member member, string indexName = null, IndexNullPolicy nullPolicy = IndexNullPolicy.IgnoreNull);

	IObjectSpaceDimensionBuilder WithUniqueIndexOn(Member member, string indexName = null, IndexNullPolicy nullPolicy = IndexNullPolicy.IgnoreNull);

	IObjectSpaceDimensionBuilder WithChangeTrackingVia(Member member);

	IObjectSpaceDimensionBuilder OptimizeAssumingAverageItemSize(int bytes);

	ObjectSpaceDefinition.DimensionDefinition BuildDefinition();

	ObjectSpaceBuilder Done(); 

}

