// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace Sphere10.Framework.Tests;

/// <summary>Verifies opaque metadata allocation, reopening and legacy compatibility in clustered storage.</summary>
[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ClusteredStreamsMetaDataTests {
	/// <summary>Verifies that the metadata flag selects the blob layout without changing the clustered-storage version.</summary>
	[Test]
	public void MetadataPresenceUsesTheHeaderFlagWithinVersionOne([Values] Endianness endianness, [Values] bool storeMetaData) {
		using var raw = new MemoryStream();
		var data = new byte[] { 41, 42, 43 };
		using (var streams = new ClusteredStreams(raw, 32, endianness: endianness)) {
			if (storeMetaData)
				streams.ConfigureMetaData(() => new byte[] { 1, 2, 3 }, 8);
			streams.Load();
			streams.AddBytes(data);
		}

		// Both layouts keep version one; the persisted flag alone selects the metadata allocation.
		var bytes = raw.ToArray();
		var policy = (ClusteredStreamsPolicy)EndianBitConverter.For(endianness).ToUInt32(bytes, ClusteredStreamsHeader.PolicyOffset);
		Assert.That(bytes[ClusteredStreamsHeader.VersionOffset], Is.EqualTo(1));
		Assert.That(policy.HasFlag(ClusteredStreamsPolicy.MetaData), Is.EqualTo(storeMetaData));

		// Normal reopening must locate the same payload with either metadata presence value.
		using var reopened = ClusteredStreams.FromStream(raw, endianness, autoLoad: true);
		Assert.That(reopened.HasMetaData, Is.EqualTo(storeMetaData));
		Assert.That(reopened.DataOffset, Is.EqualTo(storeMetaData ? ClusteredStreamsHeader.MetaDataOffset + 8 : ClusteredStreamsHeader.ByteLength));
		using var stored = reopened.OpenRead(0);
		Assert.That(stored.ToArray(), Is.EqualTo(data));
	}

	/// <summary>Verifies that metadata and stream payloads survive reopening without reserving another logical stream.</summary>
	[Test]
	public void MetadataAndStreamsReopenWithoutConsumingAReservedStream([Values] Endianness endianness) {
		using var raw = new MemoryStream();
		var payload = StringSerializer.UTF8.SerializeToBytes("initial", endianness);
		var data = Enumerable.Range(0, 100).Select(value => (byte)value).ToArray();
		var initialization = new List<bool>();
		using (var streams = new ClusteredStreams(raw, 32, ClusteredStreamsPolicy.Performance, endianness: endianness)) {
			streams.ConfigureMetaData(() => payload, 64);
			streams.RegisterInitAction(wasEmpty => initialization.Add(wasEmpty));
			streams.Load();

			// The fixed header owns the capacity DWORD and keeps ordinary reserved-stream accounting.
			Assert.That(streams.Header.Version, Is.EqualTo(ClusteredStreamsHeader.VersionNumber));
			Assert.That(streams.Header.Policy.HasFlag(ClusteredStreamsPolicy.MetaData), Is.True);
			Assert.That(streams.Header.ReservedStreams, Is.Zero);
			Assert.That(streams.MetaDataCapacity, Is.EqualTo(64));
			Assert.That(streams.Header.MetaDataSize, Is.EqualTo(64));
			Assert.That(streams.DataOffset, Is.EqualTo(ClusteredStreamsHeader.MetaDataOffset + 64));
			using (streams.EnterAccessScope())
				streams.Header.MapExtensionProperty(ClusteredStreamsHeader.MetaDataSizeOffset - 1 - ClusteredStreamsHeader.ExtensionPropertiesOffset, 1, PrimitiveSerializer<byte>.Instance).Value = 0xED;
			Assert.That(() => streams.Header.MapExtensionProperty(ClusteredStreamsHeader.MetaDataSizeOffset - ClusteredStreamsHeader.ExtensionPropertiesOffset,
				sizeof(int), PrimitiveSerializer<int>.Instance), Throws.InstanceOf<ArgumentException>());

			Assert.That(StringSerializer.UTF8.DeserializeBytes(streams.ReadMetaData(), endianness), Is.EqualTo("initial"));
			streams.WriteMetaData(StringSerializer.UTF8.SerializeToBytes("updated", endianness));
			streams.AddBytes(data);
			Assert.That(streams.Count, Is.EqualTo(1));
			Assert.That(initialization, Is.EqualTo(new[] { true }));
		}

		// The same header and opaque region bootstrap the cluster map on reopening.
		using var reopened = ClusteredStreams.FromStream(raw, endianness, autoLoad: true);
		Assert.That(reopened.HasMetaData, Is.True);
		Assert.That(reopened.Policy, Is.EqualTo(ClusteredStreamsPolicy.Performance));
		Assert.That(StringSerializer.UTF8.DeserializeBytes(reopened.ReadMetaData(), endianness), Is.EqualTo("updated"));
		using (reopened.EnterAccessScope())
			Assert.That(reopened.Header.MapExtensionProperty(ClusteredStreamsHeader.MetaDataSizeOffset - 1 - ClusteredStreamsHeader.ExtensionPropertiesOffset, 1, PrimitiveSerializer<byte>.Instance).Value, Is.EqualTo(0xED));
		using var stored = reopened.OpenRead(0);
		Assert.That(stored.ToArray(), Is.EqualTo(data));
		Assert.That(raw.CanRead && raw.CanWrite, Is.True);
	}

	/// <summary>Verifies zero padding after shorter replacements and rejects writes beyond the allocated capacity.</summary>
	[Test]
	public void WritingMetadataPadsTheAllocationAndRejectsOverflow([Values] Endianness endianness) {
		using var raw = new MemoryStream();
		using var streams = new ClusteredStreams(raw, 32, endianness: endianness);
		streams.ConfigureMetaData(() => StringSerializer.UTF8.SerializeToBytes("a longer original record", endianness), 64);
		streams.Load();
		streams.AddBytes(new byte[] { 41, 42, 43 });
		streams.WriteMetaData(StringSerializer.UTF8.SerializeToBytes("short", endianness));

		// Replacing the blob pads its fixed allocation without shifting any clustered stream.
		Assert.That(StringSerializer.UTF8.DeserializeBytes(streams.ReadMetaData(), endianness), Is.EqualTo("short"));
		var bytes = raw.ToArray();
		Assert.That(EndianBitConverter.For(endianness).ToInt32(bytes, ClusteredStreamsHeader.MetaDataSizeOffset), Is.EqualTo(64));
		var used = checked((int)StringSerializer.UTF8.CalculateSize("short"));
		Assert.That(bytes.AsSpan(ClusteredStreamsHeader.MetaDataOffset + used, 64 - used).ToArray(), Is.All.Zero);
		Assert.That(() => streams.WriteMetaData(new byte[65]), Throws.InvalidOperationException);
		Assert.That(raw.ToArray(), Is.EqualTo(bytes));
		using var stored = streams.OpenRead(0);
		Assert.That(stored.ToArray(), Is.EqualTo(new byte[] { 41, 42, 43 }));
	}

	/// <summary>Verifies that clearing logical streams preserves the metadata blob and its allocation.</summary>
	[Test]
	public void ClearingStreamsPreservesMetadataAndCapacity([Values] Endianness endianness) {
		using var raw = new MemoryStream();
		using var streams = new ClusteredStreams(raw, 32, reservedStreams: 1, endianness: endianness);
		streams.ConfigureMetaData(() => new byte[] { 1, 2, 3 }, 40);
		streams.Load();
		streams.AddBytes(new byte[] { 41, 42, 43 });
		var prefix = raw.ToArray().AsSpan(ClusteredStreamsHeader.MetaDataSizeOffset, sizeof(int) + 40).ToArray();
		streams.Clear();

		Assert.That(streams.Count, Is.EqualTo(1));
		Assert.That(streams.Header.ReservedStreams, Is.EqualTo(1));
		Assert.That(raw.ToArray().AsSpan(ClusteredStreamsHeader.MetaDataSizeOffset, sizeof(int) + 40).ToArray(), Is.EqualTo(prefix));
		Assert.That(streams.MetaDataCapacity, Is.EqualTo(40));
		streams.AddBytes(new byte[] { 44 });
		using var stored = streams.OpenRead(1);
		Assert.That(stored.ToArray(), Is.EqualTo(new byte[] { 44 }));
	}

	/// <summary>Verifies that metadata reads reflect backing-store changes rather than a cached copy.</summary>
	[Test]
	public void ReadsObserveCurrentMetadataBytes([Values] Endianness endianness) {
		using var raw = new MemoryStream();
		using var streams = new ClusteredStreams(raw, 32, endianness: endianness);
		streams.ConfigureMetaData(() => StringSerializer.UTF8.SerializeToBytes("original", endianness), 32);
		streams.Load();
		Assert.That(StringSerializer.UTF8.DeserializeBytes(streams.ReadMetaData(), endianness), Is.EqualTo("original"));

		// Reads use the backing blob directly, including changes made independently of this owner.
		raw.Position = ClusteredStreamsHeader.MetaDataOffset;
		raw.Write(StringSerializer.UTF8.SerializeToBytes("recovered", endianness));
		Assert.That(StringSerializer.UTF8.DeserializeBytes(streams.ReadMetaData(), endianness), Is.EqualTo("recovered"));
	}

	/// <summary>Verifies that recreating rolled-back storage reuses the original metadata bytes.</summary>
	[Test]
	public void CreationBytesAreCapturedOnceWhenNewStorageRollsBackToEmpty([Values] Endianness endianness) {
		using var raw = new MemoryStream();
		using var streams = new ClusteredStreams(raw, 32, endianness: endianness);
		var calls = 0;
		var bytes = new byte[] { 1, 2, 3 };
		streams.ConfigureMetaData(() => {
			calls++;
			return bytes;
		}, 16);
		streams.Load();
		bytes[0] = 99;
		raw.SetLength(0);
		streams.Initialize();

		Assert.That(calls, Is.EqualTo(1));
		Assert.That(raw.ToArray().AsSpan(ClusteredStreamsHeader.MetaDataOffset, 3).ToArray(), Is.EqualTo(new byte[] { 1, 2, 3 }));
		Assert.That(raw.Length, Is.EqualTo(streams.DataOffset));
	}

	/// <summary>Verifies that creation settings leave existing legacy bytes and header extensions unchanged.</summary>
	[Test]
	public void ConfiguredMetadataDoesNotMigrateLegacyStorage([Values] Endianness endianness) {
		using var raw = new MemoryStream();
		using (var streams = new ClusteredStreams(raw, 32, endianness: endianness, autoLoad: true)) {
			using (streams.EnterAccessScope())
				streams.Header.MapExtensionProperty(255 - ClusteredStreamsHeader.ExtensionPropertiesOffset, 1, PrimitiveSerializer<byte>.Instance).Value = 0xEE;
			streams.AddBytes(new byte[] { 41, 42, 43 });
		}
		var bytes = raw.ToArray();
		using var reopened = ClusteredStreams.FromStream(raw, endianness);
		reopened.ConfigureMetaData(() => throw new InvalidOperationException("Existing storage must not invoke the creation provider."));
		reopened.Load();

		Assert.That(reopened.HasMetaData, Is.False);
		Assert.That(reopened.Header.Version, Is.EqualTo(ClusteredStreamsHeader.VersionNumber));
		Assert.That(reopened.Header.Policy.HasFlag(ClusteredStreamsPolicy.MetaData), Is.False);
		Assert.That(reopened.MetaDataCapacity, Is.Zero);
		Assert.That(reopened.DataOffset, Is.EqualTo(ClusteredStreamsHeader.ByteLength));
		using (reopened.EnterAccessScope())
			Assert.That(reopened.Header.MapExtensionProperty(255 - ClusteredStreamsHeader.ExtensionPropertiesOffset, 1, PrimitiveSerializer<byte>.Instance).Value, Is.EqualTo(0xEE));
		Assert.That(raw.ToArray(), Is.EqualTo(bytes));
	}

	/// <summary>Verifies that an insufficient metadata budget fails before writing the storage header.</summary>
	[Test]
	public void InvalidCreationCapacityDoesNotWriteAHeader() {
		using var raw = new MemoryStream();
		using var streams = new ClusteredStreams(raw, 32);
		streams.ConfigureMetaData(() => new byte[] { 1, 2, 3 }, 2);
		Assert.That(() => streams.Load(), Throws.InvalidOperationException);
		Assert.That(raw.Length, Is.Zero);
	}

	/// <summary>Verifies that activation prevents later changes to the metadata allocation.</summary>
	[Test]
	public void ActivationFixesTheMetadataCapacity() {
		using var raw = new MemoryStream();
		using var streams = new ClusteredStreams(raw, 32);
		streams.ConfigureMetaData(() => new byte[] { 1 }, 8);
		streams.Load();
		var bytes = raw.ToArray();
		Assert.That(() => streams.ConfigureMetaData(() => new byte[16], 16), Throws.InvalidOperationException);
		Assert.That(streams.MetaDataCapacity, Is.EqualTo(8));
		Assert.That(raw.ToArray(), Is.EqualTo(bytes));
	}

	/// <summary>Verifies that truncated or invalid metadata allocations fail before cluster loading changes storage.</summary>
	[Test]
	public void InvalidMetadataFramingIsRejectedBeforeOpeningClusters([Values("truncated", "negative", "overflow")] string defect, [Values] Endianness endianness) {
		byte[] bytes;
		using (var raw = new MemoryStream()) {
			using var streams = new ClusteredStreams(raw, 32, endianness: endianness);
			streams.ConfigureMetaData(() => new byte[] { 1, 2, 3 }, 8);
			streams.Load();
			bytes = raw.ToArray();
		}
		if (defect == "truncated")
			bytes = bytes.AsSpan(0, ClusteredStreamsHeader.MetaDataSizeOffset + 2).ToArray();
		else
			EndianBitConverter.For(endianness).GetBytes(defect == "negative" ? -1 : int.MaxValue).CopyTo(bytes, ClusteredStreamsHeader.MetaDataSizeOffset);
		using var damaged = new MemoryStream(bytes);
		if (defect == "truncated") {
			Assert.That(() => ClusteredStreams.FromStream(damaged, endianness), Throws.TypeOf<InvalidOperationException>());
			Assert.That(() => ClusteredStreams.ReadStoredClusterSize(damaged, endianness), Throws.TypeOf<InvalidOperationException>());
		} else {
			using var reopened = ClusteredStreams.FromStream(damaged, endianness);
			Assert.That(() => reopened.Load(), Throws.TypeOf<InvalidDataException>());
			Assert.That(() => ClusteredStreams.ReadStoredClusterSize(damaged, endianness), Throws.TypeOf<InvalidDataException>());
		}
		Assert.That(damaged.ToArray(), Is.EqualTo(bytes));
	}
}
