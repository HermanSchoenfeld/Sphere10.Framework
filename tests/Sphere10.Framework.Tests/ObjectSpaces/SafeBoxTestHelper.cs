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
using SB = Sphere10.Framework.Tests.SafeBox;

namespace Sphere10.Framework.Tests.ObjectSpaces;

/// <summary>
/// Helper for creating ObjectSpaces configured with the SafeBox-inspired model.
/// Registers separate dimensions for the polymorphic Identity/User/Group hierarchy,
/// plus Account, Block, Transaction, and Permission.
/// </summary>
public static class SafeBoxTestHelper {

	/// <summary>
	/// Creates an ObjectSpace populated with the SafeBox model dimensions.
	/// Identity, User, and Group are registered as separate dimensions to exercise
	/// polymorphic arrays (Group.Members typed as Identity[] containing Users, Groups, etc.).
	/// </summary>
	public static ObjectSpace CreateSafeBoxObjectSpace(TestTraits traits, Dictionary<string, object> activationArgs = default) {
		activationArgs ??= [];
		var disposables = new Disposables();

		var builder = new ObjectSpaceBuilder();
		builder
			.AutoLoad()
			// Identity hierarchy — each concrete type gets its own dimension.
			// Group.Members (Identity[]) exercises polymorphic cross-dimension arrays.
			.AddDimension<SB.Identity>()
				.WithChangeTrackingVia(x => x.Dirty)
				.UsingEqualityComparer(
					EqualityComparerBuilder.For<SB.Identity>().By(x => x.Name)
				)
				.Done()
			.AddDimension<SB.User>()
				.WithChangeTrackingVia(x => x.Dirty)
				.UsingEqualityComparer(
					EqualityComparerBuilder.For<SB.User>().By(x => x.Name)
				)
				.Done()
			.AddDimension<SB.Group>()
				.WithChangeTrackingVia(x => x.Dirty)
				.UsingEqualityComparer(
					EqualityComparerBuilder.For<SB.Group>().By(x => x.Name)
				)
				.Done()
			.AddDimension<SB.Account>()
				.WithChangeTrackingVia(x => x.Dirty)
				.UsingEqualityComparer(
					EqualityComparerBuilder.For<SB.Account>().By(x => x.AccountNumber)
				)
				.Done()
			.AddDimension<SB.Block>()
				.WithChangeTrackingVia(x => x.Dirty)
				.UsingEqualityComparer(
					EqualityComparerBuilder.For<SB.Block>().By(x => x.Height)
				)
				.Done()
			.AddDimension<SB.Transaction>()
				.WithChangeTrackingVia(x => x.Dirty)
				.UsingEqualityComparer(
					EqualityComparerBuilder.For<SB.Transaction>().By(x => x.TxHash)
				)
				.Done()
			.AddDimension<SB.Permission>()
				.WithChangeTrackingVia(x => x.Dirty)
				.UsingEqualityComparer(
					EqualityComparerBuilder.For<SB.Permission>().By(x => x.PermissionName)
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
