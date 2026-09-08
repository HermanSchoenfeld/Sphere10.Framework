// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Linq;
using Sphere10.Framework.ObjectSpaces;
using NUnit.Framework;


namespace Sphere10.Framework.Tests.ObjectSpaces;

[TestFixture]
public class MerkleizedTests {


	[Test]
	[TestCaseSource(typeof(TestsHelper), nameof(TestsHelper.MerkleizedTestCases))]
	public void CheckRootsChanged(TestTraits testTraits) {
		using var objectSpace = TestsHelper.CreateObjectSpace(testTraits);
		var chf = objectSpace.Definition.HashFunction;
		var digestSize = Hashers.GetDigestSizeBytes(chf);

		var savedAccount = TestsHelper.CreateAccount();
		var accountDigest = Hashers.Hash(chf, objectSpace.Serializers.GetSerializer<Account>().SerializeBytesLE(savedAccount));
		objectSpace.Save(savedAccount);
		
		// This is necessary to flush out merkle tree changes
		objectSpace.Flush(); 

		var dim1 = objectSpace.Dimensions[0];
		var dim2 = objectSpace.Dimensions[1];

		// Verify account dimension has single item root
		using var dim1Scope = dim1.Container.ObjectStream.EnterAccessScope();
		var accountRoot = dim1.Container.ObjectStream.Streams.Header.MapExtensionProperty(0, sizeof(bool) + digestSize, new ConstantSizeNullableByteArraySerializer(digestSize)).Value;
		Assert.That(accountRoot, Is.EqualTo(accountDigest).Using(ByteArrayEqualityComparer.Instance));

		// Verify identity dimension has null root (empty tree)
		using var dim2Scope = dim2.Container.ObjectStream.EnterAccessScope();
		var identityRoot = dim2.Container.ObjectStream.Streams.Header.MapExtensionProperty(0, sizeof(bool) + digestSize, new ConstantSizeNullableByteArraySerializer(digestSize)).Value;
		Assert.That(identityRoot, Is.Null);
			
		// Verify spatial root is both account/identity
		// Note: null roots are treated as zero hash for spatial root computation
		var identityRootForSpatial = identityRoot ?? Hashers.ZeroHash(chf);
		var spaceRoot = objectSpace.Streams.Header.MapExtensionProperty(0, sizeof(bool) + digestSize, new ConstantSizeNullableByteArraySerializer(digestSize)).Value;
		Assert.That(spaceRoot, Is.EqualTo(MerkleTree.ComputeMerkleRoot(new [] { accountRoot, identityRootForSpatial }, chf)).Using(ByteArrayEqualityComparer.Instance));

	}

	[Test]
	public void IntegrityCheckThroughAttachmentRejectsCorruptHeaderRoot() {
		using var objectSpace = TestsHelper.CreateObjectSpace(TestTraits.MemoryMapped | TestTraits.Merklized);
		objectSpace.Flush();
		using var access = objectSpace.EnterAccessScope();
		var digestSize = Hashers.GetDigestSizeBytes(objectSpace.Definition.HashFunction);
		var rootProperty = objectSpace.Streams.Header.MapExtensionProperty(0, sizeof(bool) + digestSize, new ConstantSizeNullableByteArraySerializer(digestSize));
		var originalRoot = rootProperty.Value;
		using var restore = Tools.Scope.ExecuteOnDispose(() => {
			rootProperty.Value = originalRoot;
			objectSpace.Streams.Header.FlushCache();
		});
		var corruptRoot = (byte[])originalRoot.Clone();
		corruptRoot[0] ^= 1;
		rootProperty.Value = corruptRoot;
		objectSpace.Streams.Header.FlushCache();
		IClusteredStreamsAttachment attachment = objectSpace.Streams.Attachments[Sphere10FrameworkDefaults.DefaultSpatialMerkleTreeIndexName];

		Assert.That(() => attachment.VerifyIntegrity(), Throws.TypeOf<InvalidOperationException>().With.Message.Contains("header root"));
	}

	[Test]
	public void IntegrityCheckThroughAttachmentRejectsMismatchedDimensionRoot() {
		using var objectSpace = TestsHelper.CreateObjectSpace(TestTraits.MemoryMapped | TestTraits.Merklized);
		objectSpace.Flush();
		using var access = objectSpace.EnterAccessScope();
		var spatialIndex = (ObjectSpaceMerkleTreeIndex)objectSpace.Streams.Attachments[Sphere10FrameworkDefaults.DefaultSpatialMerkleTreeIndexName];
		var storage = (MerkleTreeStorageAttachment)spatialIndex.MerkleTree;
		var originalLeaf = storage.Leafs.Read(0);
		using var restore = Tools.Scope.ExecuteOnDispose(() => {
			storage.Leafs.Update(0, originalLeaf);
			storage.Flush();
		});
		var corruptLeaf = (byte[])originalLeaf.Clone();
		corruptLeaf[0] ^= 1;
		storage.Leafs.Update(0, corruptLeaf);
		storage.Flush();
		IClusteredStreamsAttachment attachment = spatialIndex;

		Assert.That(() => attachment.VerifyIntegrity(), Throws.TypeOf<InvalidOperationException>().With.Message.Contains("dimension 0"));
	}
}
