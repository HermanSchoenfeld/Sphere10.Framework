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
using System.Text;
using NUnit.Framework;

namespace Sphere10.Framework.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class StreamMappedPropertyCacheTests {

	[Test]
	public void ExistingConstructorsObserveIndependentStreamWrites([Values] bool sharedReaderWriter) {
		using var stream = new MemoryStream(new byte[sizeof(long)]);
		using var reader = new EndianBinaryReader(EndianBitConverter.Little, new NonClosingStream(stream));
		using var writer = new EndianBinaryWriter(EndianBitConverter.Little, new NonClosingStream(stream));
		var property = sharedReaderWriter
			? new StreamMappedProperty<long>(stream, 0, sizeof(long), PrimitiveSerializer<long>.Instance, reader, writer)
			: new StreamMappedProperty<long>(stream, 0, sizeof(long), PrimitiveSerializer<long>.Instance);
		Assert.That(property.UseCache, Is.False);
		Assert.That(property.Value, Is.Zero);
		stream.Position = 0;
		writer.Write(42L);
		Assert.That(property.Value, Is.EqualTo(42));
	}

	[Test]
	public void AtInfersTheRegisteredFixedSizeSerializerAndPreservesOffset([Values] Endianness endianness) {
		const int offset = 5;
		var bytes = new byte[20];
		Array.Fill(bytes, (byte)0xA7);
		using var stream = new MemoryStream(bytes);
		var property = StreamMappedProperty.At<long>(stream, offset, endianness: endianness);
		property.Value = 0x1234567890ABCDEFL;
		Assert.That(property.Value, Is.EqualTo(0x1234567890ABCDEFL));
		Assert.That(bytes.AsSpan(offset, sizeof(long)).ToArray(), Is.EqualTo(PrimitiveSerializer<long>.Instance.SerializeToBytes(property.Value, endianness)));
		Assert.That(bytes.AsSpan(0, offset).ToArray(), Is.All.EqualTo((byte)0xA7));
		Assert.That(bytes.AsSpan(offset + sizeof(long)).ToArray(), Is.All.EqualTo((byte)0xA7));
	}

	[Test]
	public void AtRequiresAConstantSizeSerializer() {
		using var stream = new MemoryStream();
		Assert.That(() => StreamMappedProperty.At(stream, 0, new StringSerializer(Encoding.UTF8)), Throws.TypeOf<ArgumentException>());
	}

	[Test]
	public void CachedValuesRequireExplicitInvalidation([Values] bool useCache) {
		using var stream = new MemoryStream(new byte[sizeof(long)]);
		var serializer = new TrackingSerializer();
		var property = StreamMappedProperty.From(stream, 0, sizeof(long), serializer, useCache: useCache);
		Assert.That(property.Value, Is.Zero);
		Assert.That(property.Value, Is.Zero);
		Assert.That(serializer.ReadContexts.Count, Is.EqualTo(useCache ? 1 : 2));
		stream.Position = 0;
		stream.Write(PrimitiveSerializer<long>.Instance.SerializeToBytes(42L, Sphere10FrameworkDefaults.Endianness));
		Assert.That(property.Value, Is.EqualTo(useCache ? 0 : 42));
		property.FlushCache();
		Assert.That(property.Value, Is.EqualTo(42));
		Assert.That(serializer.ReadContexts.Count, Is.EqualTo(useCache ? 2 : 4));
	}

	[Test]
	public void UncachedAssignmentsAlwaysReachTheStream([Values] bool useCache) {
		using var stream = new MemoryStream(new byte[sizeof(long)]);
		var serializer = new TrackingSerializer();
		var property = StreamMappedProperty.From(stream, 0, sizeof(long), serializer, useCache: useCache);
		var changes = new List<(long OldValue, long NewValue)>();
		property.ValueChanged += (oldValue, newValue) => changes.Add((oldValue, newValue));
		property.Value = 5;
		stream.Position = 0;
		stream.Write(PrimitiveSerializer<long>.Instance.SerializeToBytes(6L, Sphere10FrameworkDefaults.Endianness));
		property.Value = 5;
		Assert.That(serializer.WriteContexts.Count, Is.EqualTo(useCache ? 1 : 2));
		Assert.That(serializer.ReadContexts, Is.Empty, "Assignments must not add an implicit read.");
		Assert.That(changes, Is.EqualTo(useCache ? new[] { (0L, 5L) } : new[] { (0L, 5L), (5L, 5L) }));
		using var reader = new EndianBinaryReader(EndianBitConverter.Little, new NonClosingStream(stream));
		stream.Position = 0;
		Assert.That(reader.ReadInt64(), Is.EqualTo(useCache ? 6 : 5));
	}

	[Test]
	public void InvalidatingCachedValuesAlsoPreventsAStaleEqualityWriteShortcut() {
		using var stream = new MemoryStream(new byte[sizeof(long)]);
		var property = StreamMappedProperty.At<long>(stream, 0, useCache: true);
		property.Value = 5;
		stream.Position = 0;
		stream.Write(PrimitiveSerializer<long>.Instance.SerializeToBytes(6L, Sphere10FrameworkDefaults.Endianness));
		property.FlushCache();
		property.Value = 5;
		property.FlushCache();
		Assert.That(property.Value, Is.EqualTo(5));
	}

	[Test]
	public void HeaderExtensionCachingIsOptionalAndHeaderInvalidationReachesMappedProperties([Values] bool useCache, [Values] Endianness endianness) {
		using var stream = new MemoryStream();
		using var concurrent = new ConcurrentStream(new NonClosingStream(stream));
		using var accessScope = concurrent.EnterAccessScope();
		var header = new ClusteredStreamsHeader(concurrent, endianness);
		header.Create(1, 32, 0);
		var property = useCache
			? header.MapExtensionProperty(0, sizeof(long), PrimitiveSerializer<long>.Instance, useCache: true)
			: header.MapExtensionProperty(0, sizeof(long), PrimitiveSerializer<long>.Instance);
		Assert.That(property.UseCache, Is.EqualTo(useCache));
		property.Value = 7;
		stream.Position = ClusteredStreamsHeader.ExtensionPropertiesOffset;
		stream.Write(PrimitiveSerializer<long>.Instance.SerializeToBytes(8L, endianness));
		Assert.That(property.Value, Is.EqualTo(useCache ? 7 : 8));
		header.FlushCache();
		Assert.That(property.Value, Is.EqualTo(8));
	}

	[Test]
	public void HeaderScalarCacheRetainsItsOwnedLifetimeAndExplicitInvalidation() {
		using var stream = new MemoryStream();
		using var concurrent = new ConcurrentStream(new NonClosingStream(stream));
		using var accessScope = concurrent.EnterAccessScope();
		var header = new ClusteredStreamsHeader(concurrent, Endianness.LittleEndian);
		header.Create(1, 32, 0);
		Assert.That(header.TotalClusters, Is.Zero);
		stream.Position = ClusteredStreamsHeader.TotalClustersOffset;
		stream.Write(PrimitiveSerializer<long>.Instance.SerializeToBytes(8L, Endianness.LittleEndian));
		Assert.That(header.TotalClusters, Is.Zero);
		header.FlushCache();
		Assert.That(header.TotalClusters, Is.EqualTo(8));
	}

	[Test]
	public void EachConversionUsesAnIndependentSerializationContext([Values] bool useCache) {
		using var stream = new MemoryStream(new byte[sizeof(long)]);
		using var reader = new EndianBinaryReader(EndianBitConverter.Big, new NonClosingStream(stream));
		using var writer = new EndianBinaryWriter(EndianBitConverter.Big, new NonClosingStream(stream));
		var serializer = new TrackingSerializer();
		var property = StreamMappedProperty.From(stream, 0, sizeof(long), serializer, reader, writer, useCache: useCache);
		property.Value = 11;
		property.Value = 12;
		property.FlushCache();
		Assert.That(property.Value, Is.EqualTo(12));
		property.FlushCache();
		Assert.That(property.Value, Is.EqualTo(12));
		Assert.That(serializer.WriteContexts[0], Is.Not.SameAs(serializer.WriteContexts[1]));
		Assert.That(serializer.ReadContexts[0], Is.Not.SameAs(serializer.ReadContexts[1]));
		Assert.That(serializer.ReadContexts[0], Is.Not.SameAs(serializer.WriteContexts[0]));
		Assert.That(stream.ToArray(), Is.EqualTo(PrimitiveSerializer<long>.Instance.SerializeToBytes(12L, Endianness.BigEndian)));
		stream.Position = 0;
		writer.Write(13L);
		stream.Position = 0;
		Assert.That(reader.ReadInt64(), Is.EqualTo(13), "Supplied readers and writers remain usable by their owner.");
	}

	[Test]
	public void FailedReadsDoNotPopulateTheCache([Values] bool useCache) {
		using var stream = new MemoryStream(new byte[sizeof(long)]);
		var serializer = new TrackingSerializer { FailRead = true };
		var property = StreamMappedProperty.From(stream, 0, sizeof(long), serializer, useCache: useCache);
		Assert.That(() => property.Value, Throws.Exception.SameAs(serializer.Error));
		serializer.FailRead = false;
		Assert.That(property.Value, Is.Zero);
		Assert.That(serializer.ReadContexts.Count, Is.EqualTo(2));
	}

	[Test]
	public void FailedWritesDoNotPublishValuesOrEvents([Values] bool useCache) {
		using var stream = new MemoryStream(new byte[sizeof(long)]);
		var serializer = new TrackingSerializer { FailWrite = true };
		var property = StreamMappedProperty.From(stream, 0, sizeof(long), serializer, useCache: useCache);
		var changes = new List<(long OldValue, long NewValue)>();
		property.ValueChanged += (oldValue, newValue) => changes.Add((oldValue, newValue));
		Assert.That(() => property.Value = 42, Throws.Exception.SameAs(serializer.Error));
		Assert.That(changes, Is.Empty);
		serializer.FailWrite = false;
		property.Value = 42;
		Assert.That(changes, Is.EqualTo(new[] { (0L, 42L) }));
		Assert.That(property.Value, Is.EqualTo(42));
	}

	private sealed class TrackingSerializer : ConstantSizeItemSerializerBase<long> {
		public TrackingSerializer()
			: base(sizeof(long), false) {
		}

		public List<SerializationContext> ReadContexts { get; } = new();
		public List<SerializationContext> WriteContexts { get; } = new();
		public bool FailRead { get; set; }
		public bool FailWrite { get; set; }
		public Exception Error { get; } = new InvalidOperationException("Injected serializer failure");

		public override void Serialize(long item, EndianBinaryWriter writer, SerializationContext context) {
			WriteContexts.Add(context);
			if (FailWrite)
				throw Error;
			writer.Write(item);
		}

		public override long Deserialize(EndianBinaryReader reader, SerializationContext context) {
			ReadContexts.Add(context);
			if (FailRead)
				throw Error;
			return reader.ReadInt64();
		}
	}
}
