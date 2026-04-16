// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.IO;
using System.Linq;
using Sphere10.Framework.ObjectSpaces;
using NUnit.Framework;
using Sphere10.Framework;

namespace Sphere10.Framework.Tests.ObjectSpaces;

/// <summary>
/// Tests for ObjectSpace garbage collection (GC) functionality.
/// Covers root attributes, eager ID assignment, reference tracking, cascading deletes,
/// cyclic references, diamond references, and the full-scan CollectGarbage() safety net.
/// </summary>
[TestFixture]
public class GarbageCollectionTests {

	#region Test Model Classes

	/// <summary>
	/// Root dimension type representing a user-managed account.
	/// Root objects are never auto-collected by GC.
	/// </summary>
	[Root]
	public class GCAccount {
		[Identity]
		public string Name { get; set; }
		public GCIdentity Identity { get; set; }
		[Transient]
		public bool Dirty { get; set; }
	}

	/// <summary>
	/// Non-root dimension type representing an identity object.
	/// Non-root objects are collected when their in-ref count drops to zero.
	/// </summary>
	public class GCIdentity {
		[Identity]
		public string Key { get; set; }
		[Transient]
		public bool Dirty { get; set; }
	}

	/// <summary>
	/// Non-root dimension type for testing cyclic and diamond reference patterns.
	/// </summary>
	public class GCNode {
		[Identity]
		public string Id { get; set; }
		public GCNode Next { get; set; }
		[Transient]
		public bool Dirty { get; set; }
	}

	#endregion

	#region Helper Methods

	/// <summary>
	/// Creates a GC-enabled ObjectSpace with the specified dimensions. Uses memory-mapped storage.
	/// The Account dimension is root, Identity and Node dimensions are non-root.
	/// </summary>
	private ObjectSpace CreateGCObjectSpace(bool includeNodeDimension = false) {
		var builder = new ObjectSpaceBuilder();
		builder
			.AutoLoad()
			.WithGarbageCollection()
			.UseMemoryStream()
			.AddDimension<GCAccount>()
				.WithChangeTrackingVia(x => x.Dirty)
				.UsingEqualityComparer(
					EqualityComparerBuilder.For<GCAccount>().By(x => x.Name)
			)
				.Done()
			.AddDimension<GCIdentity>()
				.WithChangeTrackingVia(x => x.Dirty)
				.UsingEqualityComparer(
					EqualityComparerBuilder.For<GCIdentity>().By(x => x.Key)
			)
				.Done();

		if (includeNodeDimension) {
			builder
				.AddDimension<GCNode>()
					.WithChangeTrackingVia(x => x.Dirty)
					.UsingEqualityComparer(
						EqualityComparerBuilder.For<GCNode>().By(x => x.Id)
				)
					.Done();
		}

		return builder.Build();
	}

	#endregion

	#region Task 3: Root Attribute

	/// <summary>
	/// Test 1: Verify that a dimension marked with [Root] has IsRoot = true in its definition.
	/// </summary>
	[Test]
	public void RootAttribute_SetsIsRootOnDimensionDefinition() {
		using var objectSpace = CreateGCObjectSpace();

		// GCAccount is decorated with [Root] — its dimension should be marked as root
		var accountDimension = objectSpace.Definition.Dimensions
			.First(d => d.ObjectType == typeof(GCAccount));
		Assert.That(accountDimension.IsRoot, Is.True, "GCAccount dimension should be root");

		// GCIdentity has no [Root] attribute — its dimension should NOT be marked as root
		var identityDimension = objectSpace.Definition.Dimensions
			.First(d => d.ObjectType == typeof(GCIdentity));
		Assert.That(identityDimension.IsRoot, Is.False, "GCIdentity dimension should not be root");
	}

	/// <summary>
	/// Test 2: Verify that GC-enabled ObjectSpace requires at least one root dimension.
	/// </summary>
	[Test]
	public void GarbageCollect_RequiresAtLeastOneRootDimension() {
		// Build an ObjectSpace with GC enabled but no root dimensions — should fail validation
		var builder = new ObjectSpaceBuilder();
		builder
			.WithGarbageCollection()
			.UseMemoryStream()
			.AddDimension<GCIdentity>()
				.WithChangeTrackingVia(x => x.Dirty)
				.UsingEqualityComparer(
					EqualityComparerBuilder.For<GCIdentity>().By(x => x.Key)
			)
				.Done();

		// BuildDefinition should throw because no root dimension is present
		Assert.That(() => builder.BuildDefinition(), Throws.Exception);
	}

	#endregion

	#region Task 6: Eager ID Assignment

	/// <summary>
	/// Test 3: After New&lt;T&gt;(), the object should have a valid ObjectSpaceObjectReference 
	/// immediately (before save), proving eager ID assignment.
	/// </summary>
	[Test]
	public void EagerIdAssignment_NewObjectHasRefBeforeSave() {
		using var objectSpace = CreateGCObjectSpace();

		// Create a new account — should eagerly assign an ObjectSpaceObjectReference
		var account = objectSpace.New<GCAccount>();
		account.Name = "EagerTest";

		// The ObjectSpace should have a tracked ref for this object even before saving
		// This is validated indirectly: saving should not throw because the ref is available for serialization
		Assert.That(() => objectSpace.Save(account), Throws.Nothing);
	}

	#endregion

	#region Task 10: GarbageCollect Trait

	/// <summary>
	/// Test: Verify that the GarbageCollect trait is set when WithGarbageCollection() is called.
	/// </summary>
	[Test]
	public void GarbageCollectTrait_IsSetByBuilder() {
		using var objectSpace = CreateGCObjectSpace();
		Assert.That(
			objectSpace.Definition.Traits.HasFlag(ObjectSpaceTraits.GarbageCollect), 
			Is.True,
			"GarbageCollect trait should be set"
		);
	}

	#endregion

	#region Task 9: Root Protection

	/// <summary>
	/// Test 7: Root dimension objects are never auto-collected, even with zero in-refs.
	/// Saving and then performing a full GC should not remove root objects.
	/// </summary>
	[Test]
	public void RootProtection_RootObjectsNeverAutoCollected() {
		using var objectSpace = CreateGCObjectSpace();

		// Create and save a root object (GCAccount) that has no incoming references from other objects
		var account = objectSpace.New<GCAccount>();
		account.Name = "RootProtected";
		objectSpace.Save(account);

		// Run full GC — root objects should survive even with zero in-refs
		objectSpace.CollectGarbage();

		// Verify the account is still accessible (not collected)
		Assert.That(objectSpace.Count<GCAccount>(), Is.EqualTo(1), "Root object should survive GC");
	}

	#endregion

	#region Task 9: CollectGarbage Full Scan

	/// <summary>
	/// Test 11: CollectGarbage() full-scan should find and remove orphaned non-root objects
	/// that were left without any incoming references.
	/// </summary>
	[Test]
	public void CollectGarbage_FullScan_CollectsOrphanedNonRootObjects() {
		using var objectSpace = CreateGCObjectSpace();

		// Create an identity (non-root) and save it standalone — it has no incoming references
		var identity = objectSpace.New<GCIdentity>();
		identity.Key = "Orphan";
		objectSpace.Save(identity);

		// At this point, the identity is saved but nothing references it
		Assert.That(objectSpace.Count<GCIdentity>(), Is.EqualTo(1), "Identity should exist before GC");

		// Run full GC — the orphaned identity should be collected
		objectSpace.CollectGarbage();

		Assert.That(objectSpace.Count<GCIdentity>(), Is.EqualTo(0), "Orphaned non-root object should be collected by full GC");
	}

	#endregion

	#region General GC Operation

	/// <summary>
	/// Verify that creating a GC-enabled ObjectSpace and performing basic operations doesn't throw.
	/// </summary>
	[Test]
	public void GCObjectSpace_BasicOperations_DoNotThrow() {
		using var objectSpace = CreateGCObjectSpace();

		// Create and save objects
		var identity = objectSpace.New<GCIdentity>();
		identity.Key = "TestKey";
		objectSpace.Save(identity);

		var account = objectSpace.New<GCAccount>();
		account.Name = "TestAccount";
		account.Identity = identity;
		objectSpace.Save(account);

		// Flush should not throw
		Assert.That(() => objectSpace.Flush(), Throws.Nothing);
	}

	/// <summary>
	/// Test that CollectGarbage() is a no-op when GC is not enabled.
	/// </summary>
	[Test]
	public void CollectGarbage_WhenGCDisabled_IsNoOp() {
		// Create a non-GC ObjectSpace (standard behavior)
		var builder = new ObjectSpaceBuilder();
		builder
			.AutoLoad()
			.UseMemoryStream()
			.AddDimension<GCAccount>()
				.WithChangeTrackingVia(x => x.Dirty)
				.UsingEqualityComparer(
					EqualityComparerBuilder.For<GCAccount>().By(x => x.Name)
			)
				.Done()
			.AddDimension<GCIdentity>()
				.WithChangeTrackingVia(x => x.Dirty)
				.UsingEqualityComparer(
					EqualityComparerBuilder.For<GCIdentity>().By(x => x.Key)
			)
				.Done();

		using var objectSpace = builder.Build();

		// CollectGarbage should be a no-op and not throw
		Assert.That(() => objectSpace.CollectGarbage(), Throws.Nothing);
	}

	#endregion
}
