// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.IO;
using Sphere10.Framework.ObjectSpaces;

namespace Sphere10.Framework.Tests.ObjectSpaces;

/// <summary>
/// Helper for creating ObjectSpaces configured with the SafeBox-inspired model.
/// All five dimension types are registered with appropriate change tracking, equality
/// comparers, and unique indexes (discovered from annotations).
/// </summary>
public static class SafeBoxTestHelper {

	/// <summary>
	/// Creates an ObjectSpace populated with the SafeBox model dimensions.
	/// Cross-dimension references are serialized inline with context-level deduplication.
	/// </summary>
	public static ObjectSpace CreateSafeBoxObjectSpace(TestTraits traits, Dictionary<string, object> activationArgs = default) {
		activationArgs ??= [];
		var disposables = new Disposables();

		var builder = new ObjectSpaceBuilder();
		builder
			.AutoLoad()
			// Dimensions — annotations on the model classes provide [Identity], [UniqueIndex], [Root], etc.
			.AddDimension<SafeBoxIdentity>()
				.WithChangeTrackingVia(x => x.Dirty)
				.UsingEqualityComparer(
					EqualityComparerBuilder.For<SafeBoxIdentity>().By(x => x.Name)
				)
				.Done()
			.AddDimension<SafeBoxAccount>()
				.WithChangeTrackingVia(x => x.Dirty)
				.UsingEqualityComparer(
					EqualityComparerBuilder.For<SafeBoxAccount>().By(x => x.AccountNumber)
				)
				.Done()
			.AddDimension<SafeBoxBlock>()
				.WithChangeTrackingVia(x => x.Dirty)
				.UsingEqualityComparer(
					EqualityComparerBuilder.For<SafeBoxBlock>().By(x => x.Height)
				)
				.Done()
			.AddDimension<SafeBoxTransaction>()
				.WithChangeTrackingVia(x => x.Dirty)
				.UsingEqualityComparer(
					EqualityComparerBuilder.For<SafeBoxTransaction>().By(x => x.TxHash)
				)
				.Done()
			.AddDimension<SafeBoxPermission>()
				.WithChangeTrackingVia(x => x.Dirty)
				.UsingEqualityComparer(
					EqualityComparerBuilder.For<SafeBoxPermission>().By(x => x.PermissionName)
				)
				.Done();

		// Persistent ignorance (auto-save)
		if (traits.HasFlag(TestTraits.PersistentIgnorant))
			builder.AutoSave();

		// Memory mapped
		if (traits.HasFlag(TestTraits.MemoryMapped)) {
			if (!activationArgs.TryGetValue("stream", out var stream)) {
				stream = new MemoryStream();
				disposables.Add((MemoryStream)stream);
			}
			builder.UseMemoryStream((MemoryStream)stream);
		}

		// File mapped
		if (traits.HasFlag(TestTraits.FileMapped)) {
			if (!activationArgs.TryGetValue("folder", out var folder)) {
				folder = Tools.FileSystem.GetTempEmptyDirectory(true);
				disposables.Add(Tools.Scope.DeleteDirOnDispose((string)folder));
			}
			var file = Path.Combine((string)folder, "safebox.db");
			builder.UseFile(file);
		}

		// Merkleized
		if (traits.HasFlag(TestTraits.Merklized))
			builder.Merkleized();

		var objectSpace = builder.Build();
		objectSpace.Disposables.Add(disposables);
		return objectSpace;
	}

	/// <summary>Memory-mapped test case subsets (fastest for integration loops).</summary>
	public static readonly IEnumerable<TestTraits> MemoryMappedTestCases = [
		TestTraits.MemoryMapped,
	];

	/// <summary>All supported test trait combinations.</summary>
	public static readonly IEnumerable<TestTraits> AllTestCases = [
		TestTraits.MemoryMapped,
		TestTraits.FileMapped,
	];
}
