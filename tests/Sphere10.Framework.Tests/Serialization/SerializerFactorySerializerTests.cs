// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using NUnit.Framework;

namespace Sphere10.Framework.Tests;

/// <summary>Verifies serialized factory registrations, component layouts and unsupported reconstruction cases.</summary>
[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class SerializerFactorySerializerTests {
	/// <summary>Verifies exact registration codes and the next generated code after factory reconstruction.</summary>
	[Test]
	public void RegistrationsRoundTripWithExactCodesAndTheNextCode([Values] Endianness endianness) {
		var original = new SerializerFactory(SerializerFactory.Default).SetMinTypeCode(10000);
		var serializer = new SerializerFactorySerializer();
		var bytes = serializer.SerializeToBytes(original, endianness);
		Assert.That(bytes.Length, Is.GreaterThan(4096));
		Assert.That(serializer.CalculateSize(original), Is.EqualTo(bytes.Length));
		var restored = serializer.DeserializeBytes(bytes, endianness);
		Assert.That(restored.RegisteredTypes, Is.EquivalentTo(original.RegisteredTypes));
		foreach (var type in original.RegisteredTypes)
			Assert.That(restored.FindRegistration(type).TypeCode, Is.EqualTo(original.FindRegistration(type).TypeCode));
		Assert.That(restored.GenerateTypeCode(), Is.EqualTo(10000));
		Assert.That(serializer.SerializeToBytes(restored, endianness), Is.EqualTo(bytes));
	}

	/// <summary>Verifies factory reconstruction without requiring input length or cursor access.</summary>
	[Test]
	public void FactoryCanBeReadFromANonSeekableStream([Values] Endianness endianness) {
		var original = new SerializerFactory(SerializerFactory.Default).SetMinTypeCode(10000);
		var serializer = new SerializerFactorySerializer();
		var bytes = serializer.SerializeToBytes(original, endianness);
		using var raw = new MemoryStream(bytes, writable: false);
		using var stream = new NonSeekableReadStream(raw);
		using var reader = new EndianBinaryReader(EndianBitConverter.For(endianness), stream, leaveOpen: true);
		using var context = new SerializationContext();
		var restored = serializer.Deserialize(reader, context);
		Assert.That(restored.RegisteredTypes, Is.EquivalentTo(original.RegisteredTypes));
		Assert.That(restored.FindRegistration(typeof(long)).TypeCode, Is.EqualTo(original.FindRegistration(typeof(long)).TypeCode));
		var payload = original.GetSerializer<long>().SerializeToBytes(42L, endianness);
		Assert.That(restored.GetSerializer<long>().DeserializeBytes(payload, endianness), Is.EqualTo(42L));
		Assert.That(raw.Position, Is.EqualTo(bytes.Length));
	}

	/// <summary>Verifies that seekable input bounds reject oversized registration counts before allocation.</summary>
	[Test]
	public void OversizedRegistrationCountIsRejectedBeforeAllocation([Values] Endianness endianness) {
		var serializer = new SerializerFactorySerializer();
		var bytes = serializer.SerializeToBytes(new SerializerFactory(), endianness);
		EndianBitConverter.For(endianness).WriteTo(int.MaxValue, bytes, sizeof(long));
		Assert.That(() => serializer.DeserializeBytes(bytes, endianness), Throws.TypeOf<InvalidDataException>());
	}

	/// <summary>Verifies incremental registration reads for an untrusted count on nonseekable input.</summary>
	[Test]
	public void NonSeekableOversizedRegistrationCountReachesEndOfInputWithoutPreallocation([Values] Endianness endianness) {
		// An unavailable length must not turn an untrusted row count into an allocation request.
		var serializer = new SerializerFactorySerializer();
		var bytes = serializer.SerializeToBytes(new SerializerFactory(), endianness);
		EndianBitConverter.For(endianness).WriteTo(int.MaxValue, bytes, sizeof(long));
		using var raw = new MemoryStream(bytes, writable: false);
		using var stream = new NonSeekableReadStream(raw);
		using var reader = new EndianBinaryReader(EndianBitConverter.For(endianness), stream, leaveOpen: true);
		using var context = new SerializationContext();
		Assert.That(() => serializer.Deserialize(reader, context), Throws.TypeOf<EndOfStreamException>());
	}

	/// <summary>Verifies incremental member reads for an untrusted count on nonseekable input.</summary>
	[Test]
	public void NonSeekableOversizedMemberCountReachesEndOfInputWithoutPreallocation([Values] Endianness endianness) {
		// This one-row factory ends with an empty member table; replace only that table's count.
		var serializer = new SerializerFactorySerializer();
		var original = new SerializerFactory().Register<long, PrimitiveSerializer<long>>(1900);
		var bytes = serializer.SerializeToBytes(original, endianness);
		EndianBitConverter.For(endianness).WriteTo(int.MaxValue, bytes, bytes.Length - sizeof(int));
		using var raw = new MemoryStream(bytes, writable: false);
		using var stream = new NonSeekableReadStream(raw);
		using var reader = new EndianBinaryReader(EndianBitConverter.For(endianness), stream, leaveOpen: true);
		using var context = new SerializationContext();

		// The existing string length decoder reports missing data before a member row can be added.
		Assert.That(() => serializer.Deserialize(reader, context),
			Throws.TypeOf<InvalidOperationException>().With.Message.Contains("Unexpected end of stream"));
	}

	/// <summary>Verifies that the default factory resolves a factory codec that reconstructs usable serializers.</summary>
	[Test]
	public void DefaultFactorySerializerRegistrationRoundTripsAUsableFactory([Values] Endianness endianness) {
		// Exercise the registered value serializer, including its ordinary reference wrapper.
		var original = new SerializerFactory(SerializerFactory.Default).SetMinTypeCode(2100).RegisterAutoBuild<WireItem>();
		var serializer = SerializerFactory.Default.GetSerializer<SerializerFactory>();
		var restored = serializer.DeserializeBytes(serializer.SerializeToBytes(original, endianness), endianness);
		var payload = original.GetSerializer<WireItem>().SerializeToBytes(new WireItem { First = "default registration", Second = 42 }, endianness);
		var copy = restored.GetSerializer<WireItem>().DeserializeBytes(payload, endianness);
		Assert.That(copy.First, Is.EqualTo("default registration"));
		Assert.That(copy.Second, Is.EqualTo(42));
		Assert.That(restored.FindRegistration(typeof(WireItem)).TypeCode, Is.EqualTo(2100));
	}

	/// <summary>Verifies dependency-first composite reconstruction preserves constant sizing in either row order.</summary>
	[Test]
	public void NestedFixedSizeCompositesKeepTheirSizeInEitherRegistrationOrder([Values] Endianness endianness, [Values] bool childFirst) {
		var original = new SerializerFactory(SerializerFactory.Default);
		if (childFirst)
			original.RegisterAutoBuild<ConstantChild>(2200);
		original.RegisterAutoBuild<ConstantParent>(2300);
		var factorySerializer = new SerializerFactorySerializer();
		var restored = factorySerializer.DeserializeBytes(factorySerializer.SerializeToBytes(original, endianness), endianness);
		var originalSerializer = original.GetPureSerializer<ConstantParent>();
		var restoredSerializer = restored.GetPureSerializer<ConstantParent>();
		var item = new ConstantParent { Child = new ConstantChild { Value = 42 } };

		// Configuration snapshots the member serializer's fixed-size state, so children must already be configured.
		Assert.That(originalSerializer.IsConstantSize, Is.True);
		Assert.That(restoredSerializer.IsConstantSize, Is.EqualTo(originalSerializer.IsConstantSize));
		Assert.That(restoredSerializer.ConstantSize, Is.EqualTo(originalSerializer.ConstantSize));
		Assert.That(restoredSerializer.CalculateSize(item), Is.EqualTo(originalSerializer.CalculateSize(item)));
		var payload = originalSerializer.SerializeToBytes(item, endianness);
		Assert.That(restoredSerializer.SerializeToBytes(item, endianness), Is.EqualTo(payload));
		Assert.That(restoredSerializer.DeserializeBytes(payload, endianness).Child.Value, Is.EqualTo(42));
	}

	/// <summary>Verifies that member records distinguish inherited fields and hidden properties by declaring type.</summary>
	[Test]
	public void InheritedHiddenMembersRetainTheirDeclaringTypeAndMemberKind([Values] Endianness endianness) {
		var original = new SerializerFactory(SerializerFactory.Default).RegisterAutoBuild<HiddenDerived>(2400);
		var originalSerializer = (CompositeSerializer<HiddenDerived>)original.GetPureSerializer<HiddenDerived>();
		var item = new HiddenDerived { Value = 11, Other = 22 };
		((HiddenBase)item).Value = 33;
		((HiddenBase)item).Other = 44;
		var payload = originalSerializer.SerializeToBytes(item, endianness);
		var originalCopy = originalSerializer.DeserializeBytes(payload, endianness);
		var serializer = new SerializerFactorySerializer();
		var restored = serializer.DeserializeBytes(serializer.SerializeToBytes(original, endianness), endianness);
		var restoredSerializer = (CompositeSerializer<HiddenDerived>)restored.GetPureSerializer<HiddenDerived>();

		// Match the actual bindings rather than choosing a same-named member again from the derived type.
		Assert.That(restoredSerializer.MemberBindings.Select(binding => (binding.Member.DeclaringType, binding.Member.Name, binding.Member.IsField)),
			Is.EqualTo(originalSerializer.MemberBindings.Select(binding => (binding.Member.DeclaringType, binding.Member.Name, binding.Member.IsField))));
		var copy = restoredSerializer.DeserializeBytes(payload, endianness);
		Assert.That(new[] { ((HiddenBase)copy).Value, ((HiddenBase)copy).Other, copy.Value, copy.Other },
			Is.EqualTo(new[] { ((HiddenBase)originalCopy).Value, ((HiddenBase)originalCopy).Other, originalCopy.Value, originalCopy.Other }));
		Assert.That(restoredSerializer.SerializeToBytes(copy, endianness), Is.EqualTo(payload));
	}

	/// <summary>Verifies that reconstruction retains configured member order and reference wrappers.</summary>
	[Test]
	public void CompositePreservesActualMemberOrderAndReferenceMode([Values] Endianness endianness) {
		var original = new SerializerFactory(SerializerFactory.Default).RegisterAutoBuild<WireItem>();
		var serializer = (CompositeSerializer<WireItem>)original.GetPureSerializer<WireItem>();
		var first = serializer.MemberBindings.Single(binding => binding.Member.Name == nameof(WireItem.First));
		var second = serializer.MemberBindings.Single(binding => binding.Member.Name == nameof(WireItem.Second));
		serializer.MemberBindings[0] = second;
		serializer.MemberBindings[1] = first with { Serializer = original.GetPureSerializer<string>().AsReferenceSerializer(ReferenceSerializerMode.SupportNull) };
		var factorySerializer = new SerializerFactorySerializer();
		var bytes = factorySerializer.SerializeToBytes(original, endianness);
		var restored = factorySerializer.DeserializeBytes(bytes, endianness);
		var composite = (CompositeSerializer<WireItem>)restored.GetPureSerializer<WireItem>();
		Assert.That(composite.MemberBindings.Select(binding => binding.Member.Name), Is.EqualTo(new[] { "Second", "First" }));
		Assert.That(((ReferenceSerializer<string>)composite.MemberBindings[1].Serializer).Mode, Is.EqualTo(ReferenceSerializerMode.SupportNull));
		var item = new WireItem { First = "value", Second = 42 };
		var payload = serializer.SerializeToBytes(item, endianness);
		Assert.That(composite.SerializeToBytes(item, endianness), Is.EqualTo(payload));
		Assert.That(composite.DeserializeBytes(payload, endianness).First, Is.EqualTo("value"));
	}

	/// <summary>Verifies that reconstructed automatic serializers preserve cycles and reference identity.</summary>
	[Test]
	public void OwnedAutomaticFactoryPreservesCyclicReferences([Values] Endianness endianness) {
		var original = new SerializerFactory(SerializerFactory.Default).RegisterAutoBuild<LinkedItem>(900);
		var factorySerializer = new SerializerFactorySerializer();
		var bytes = factorySerializer.SerializeToBytes(original, endianness);
		var restored = factorySerializer.DeserializeBytes(bytes, endianness);
		var item = new LinkedItem { Value = 42 };
		item.Next = item;
		var payload = original.GetSerializer<LinkedItem>().SerializeToBytes(item, endianness);
		var copy = restored.GetSerializer<LinkedItem>().DeserializeBytes(payload, endianness);
		Assert.That(copy.Value, Is.EqualTo(42));
		Assert.That(copy.Next, Is.SameAs(copy));
		Assert.That(restored.FindRegistration(typeof(LinkedItem)).TypeCode, Is.EqualTo(original.FindRegistration(typeof(LinkedItem)).TypeCode));
	}

	/// <summary>Verifies nested generic collections, nulls and aliases through a reconstructed factory.</summary>
	[Test]
	public void CompositeGenericCollectionsKeepNestedValuesAndSharedReferences([Values] Endianness endianness) {
		var original = new SerializerFactory(SerializerFactory.Default).RegisterAutoBuild<CollectionItem>(901);
		var factorySerializer = new SerializerFactorySerializer();
		var bytes = factorySerializer.SerializeToBytes(original, endianness);
		var restored = factorySerializer.DeserializeBytes(bytes, endianness);
		var shared = new List<long> { 1, -2, long.MaxValue };
		var values = new Dictionary<string, List<long>> {
			["primary"] = shared,
			["alias"] = shared,
			["empty"] = new List<long>()
		};
		var item = new CollectionItem { Values = values, Alias = values };
		var payload = original.GetSerializer<CollectionItem>().SerializeToBytes(item, endianness);
		var serializer = restored.GetSerializer<CollectionItem>();
		var copy = serializer.DeserializeBytes(payload, endianness);
		Assert.That(copy.Values["primary"], Is.EqualTo(shared));
		Assert.That(copy.Values["empty"], Is.Empty);
		Assert.That(copy.Values["alias"], Is.SameAs(copy.Values["primary"]));
		Assert.That(copy.Alias, Is.SameAs(copy.Values));
		Assert.That(copy.Optional, Is.Null);
		Assert.That(serializer.SerializeToBytes(copy, endianness), Is.EqualTo(payload));
	}

	/// <summary>Verifies rejection of serializer instance state that registration records cannot reconstruct.</summary>
	[Test]
	public void IndependentlyConfiguredSerializerInstanceCannotBeReconstructed() {
		var factory = new SerializerFactory().Register(new ConfiguredIntSerializer { Offset = 37 }, 800);
		Assert.That(() => new SerializerFactorySerializer().CalculateSize(factory), Throws.TypeOf<NotSupportedException>().With.Message.Contains("cannot be reconstructed"));
	}

	/// <summary>Verifies that stored provenance selects the original serializer constructor.</summary>
	[Test]
	public void ParameterlessConstructionIsDistinctFromConstructorInjection([Values] Endianness endianness, [Values] bool parameterless) {
		var original = new SerializerFactory().Register(typeof(long), typeof(PrimitiveSerializer<long>), 800);
		if (parameterless)
			original.Register<Box<long>, BoxSerializer>(801);
		else
			original.Register(typeof(Box<long>), typeof(BoxSerializer), 801);
		var factorySerializer = new SerializerFactorySerializer();
		var restored = factorySerializer.DeserializeBytes(factorySerializer.SerializeToBytes(original, endianness), endianness);
		var payload = original.GetPureSerializer<Box<long>>().SerializeToBytes(new Box<long> { Value = 42 }, endianness);
		Assert.That(restored.GetPureSerializer<Box<long>>().DeserializeBytes(payload, endianness).Value, Is.EqualTo(42));
		Assert.That(((BoxSerializer)restored.GetPureSerializer<Box<long>>()).Offset, Is.EqualTo(parameterless ? 0 : 1000));
		Assert.That(factorySerializer.SerializeToBytes(restored, endianness), Is.EqualTo(factorySerializer.SerializeToBytes(original, endianness)));
	}

	/// <summary>Verifies that open-generic registration reconstruction preserves dependency hierarchies.</summary>
	[Test]
	public void OpenGenericConstructionPreservesItsSerializerHierarchy([Values] Endianness endianness) {
		var original = new SerializerFactory(SerializerFactory.Default).Register(typeof(Box<>), typeof(GenericBoxSerializer<>), 1800);
		var factorySerializer = new SerializerFactorySerializer();
		var bytes = factorySerializer.SerializeToBytes(original, endianness);
		var restored = factorySerializer.DeserializeBytes(bytes, endianness);
		Assert.That(restored.GetSerializerHierarchy(typeof(Box<long>)).Flatten(), Is.EqualTo(original.GetSerializerHierarchy(typeof(Box<long>)).Flatten()));
		var payload = original.GetSerializer<object>().SerializeToBytes(new Box<long> { Value = 42 }, endianness);
		Assert.That(((Box<long>)restored.GetSerializer<object>().DeserializeBytes(payload, endianness)).Value, Is.EqualTo(42));
	}

	/// <summary>Verifies enum and nullable-enum registrations and payloads retain their original codes.</summary>
	[Test]
	public void EnumAndNullableEnumKeepTheirRegistrations([Values] Endianness endianness) {
		var original = new SerializerFactory().RegisterEnum<WireEnum>(1900);
		var serializer = new SerializerFactorySerializer();
		var restored = serializer.DeserializeBytes(serializer.SerializeToBytes(original, endianness), endianness);
		var payload = original.GetPureSerializer<WireEnum>().SerializeToBytes(WireEnum.Value, endianness);
		Assert.That(restored.GetPureSerializer<WireEnum>().DeserializeBytes(payload, endianness), Is.EqualTo(WireEnum.Value));
		var nullablePayload = original.GetPureSerializer<WireEnum?>().SerializeToBytes(null, endianness);
		Assert.That(restored.GetPureSerializer<WireEnum?>().DeserializeBytes(nullablePayload, endianness), Is.Null);
		Assert.That(restored.FindRegistration(typeof(WireEnum)).TypeCode, Is.EqualTo(1900));
		Assert.That(restored.FindRegistration(typeof(WireEnum?)).TypeCode, Is.EqualTo(1901));
	}

	/// <summary>Verifies reconstructed polymorphic serializers resolve subtypes through their owning factory.</summary>
	[Test]
	public void PolymorphicRegistrationRetainsItsOwnFactory([Values] Endianness endianness) {
		var original = new SerializerFactory(SerializerFactory.Default).RegisterAutoBuild<WireBase>();
		var serializer = new SerializerFactorySerializer();
		var bytes = serializer.SerializeToBytes(original, endianness);
		var restored = serializer.DeserializeBytes(bytes, endianness);
		var payload = original.GetSerializer<WireBase>().SerializeToBytes(new WireVariant { Value = 42 }, endianness);
		var item = restored.GetSerializer<WireBase>().DeserializeBytes(payload, endianness);
		Assert.That(item, Is.TypeOf<WireVariant>());
		Assert.That(((WireVariant)item).Value, Is.EqualTo(42));
		Assert.That(((PolymorphicSerializer<WireBase>)restored.GetPureSerializer<WireBase>()).Factory, Is.SameAs(restored));
		Assert.That(serializer.SerializeToBytes(restored, endianness), Is.EqualTo(bytes));
	}

	/// <summary>Verifies rejection of polymorphic registrations whose factory ownership cannot be reproduced.</summary>
	[Test]
	public void CopiedPolymorphicRegistrationIsRejectedBeforeWritingBytes() {
		var original = new SerializerFactory(SerializerFactory.Default).RegisterAutoBuild<WireBase>();
		var copied = new SerializerFactory(original);
		using var raw = new MemoryStream();
		using var writer = new EndianBinaryWriter(EndianBitConverter.Little, raw, leaveOpen: true);
		using var context = new SerializationContext();
		Assert.That(() => new SerializerFactorySerializer().Serialize(copied, writer, context),
			Throws.TypeOf<NotSupportedException>().With.Message.Contains("another serializer factory"));
		Assert.That(raw.Length, Is.Zero);
	}

	/// <summary>Verifies rejection of composite activators whose behavior is not represented by member records.</summary>
	[Test]
	public void CompositeWithACustomActivatorCannotBeReconstructed() {
		var serializer = SerializerBuilder.For(() => new WireItem { First = "custom" })
			.Serialize(item => item.Second, PrimitiveSerializer<int>.Instance)
			.Build();
		var factory = new SerializerFactory(SerializerFactory.Default).Register(serializer, 1900);
		Assert.That(() => new SerializerFactorySerializer().CalculateSize(factory), Throws.TypeOf<NotSupportedException>().With.Message.Contains("cannot be reconstructed"));
	}

	/// <summary>Verifies that an unsupported captured activator is rejected without executing user code.</summary>
	[Test]
	public void CapturedFactoryDelegateIsRejectedWithoutInvocation() {
		var called = false;
		var factory = new SerializerFactory().Register<int, ConfiguredIntSerializer>(() => {
			called = true;
			return new ConfiguredIntSerializer { Offset = 37 };
		});
		Assert.That(() => new SerializerFactorySerializer().CalculateSize(factory), Throws.TypeOf<NotSupportedException>());
		Assert.That(called, Is.False);
	}

	/// <summary>Verifies that replacing a factory delegate invalidates its former construction provenance.</summary>
	[Test]
	public void ReplacedFactoryDelegateDoesNotRetainConstructorProvenance() {
		var factory = new SerializerFactory().Register<int, ConfiguredIntSerializer>();
		factory.FindRegistration(typeof(int)).Factory = (_, _) => new ConfiguredIntSerializer { Offset = 37 };
		Assert.That(() => new SerializerFactorySerializer().CalculateSize(factory), Throws.TypeOf<NotSupportedException>());
	}

	/// <summary>Verifies that changed registration identities fail preflight before any bytes are emitted.</summary>
	[Test]
	public void MutatedRegistrationIsRejectedBeforeWritingBytes([Values] bool copiedDefault, [Values("dataType", "serializerType")] string property) {
		var factory = copiedDefault ? new SerializerFactory(SerializerFactory.Default) : new SerializerFactory().Register<int, ConfiguredIntSerializer>();
		var registration = factory.FindRegistration(typeof(int));
		if (property == "dataType")
			registration.DataType = typeof(long);
		else
			registration.SerializerType = copiedDefault ? typeof(ConfiguredIntSerializer) : typeof(PrimitiveSerializer<int>);
		using var raw = new MemoryStream();
		using var writer = new EndianBinaryWriter(EndianBitConverter.Little, raw, leaveOpen: true);
		using var context = new SerializationContext();
		Assert.That(() => new SerializerFactorySerializer().Serialize(factory, writer, context), Throws.TypeOf<NotSupportedException>());
		Assert.That(raw.Length, Is.Zero);
	}

	/// <summary>Verifies that changing registration ownership fails preflight before output is written.</summary>
	[Test]
	public void ReassignedRegistrationOwnerIsRejectedBeforeWritingBytes([Values] bool openGeneric) {
		// The owner controls injected generic serializers, even when the constructor provenance still matches.
		var factory = new SerializerFactory(SerializerFactory.Default);
		if (openGeneric)
			factory.Register(typeof(Box<>), typeof(GenericBoxSerializer<>), 2500);
		else
			factory.Register(typeof(Box<long>), typeof(BoxSerializer), 2500);
		factory.FindRegistration(openGeneric ? typeof(Box<>) : typeof(Box<long>)).Owner = new SerializerFactory(SerializerFactory.Default);
		using var raw = new MemoryStream();
		using var writer = new EndianBinaryWriter(EndianBitConverter.Little, raw, leaveOpen: true);
		using var context = new SerializationContext();
		Assert.That(() => new SerializerFactorySerializer().Serialize(factory, writer, context),
			Throws.TypeOf<NotSupportedException>().With.Message.Contains("owner"));
		Assert.That(raw.Length, Is.Zero);
	}

	/// <summary>Verifies that changing a registered type code fails preflight before output is written.</summary>
	[Test]
	public void ReassignedRegistrationTypeCodeIsRejectedBeforeWritingBytes([Values] bool copiedDefault) {
		// Mutating the exposed record does not change the registry key used by the original factory.
		var factory = copiedDefault ? new SerializerFactory(SerializerFactory.Default) : new SerializerFactory().Register<int, ConfiguredIntSerializer>();
		factory.FindRegistration(typeof(int)).TypeCode = 100000;
		using var raw = new MemoryStream();
		using var writer = new EndianBinaryWriter(EndianBitConverter.Little, raw, leaveOpen: true);
		using var context = new SerializationContext();
		Assert.That(() => new SerializerFactorySerializer().Serialize(factory, writer, context),
			Throws.TypeOf<NotSupportedException>().With.Message.Contains("type code"));
		Assert.That(raw.Length, Is.Zero);
	}

	/// <summary>Removes seeking and length access from an ordinary stream for incremental-read coverage.</summary>
	private sealed class NonSeekableReadStream : StreamDecorator {
		/// <summary>Wraps the supplied readable stream without adding seek support.</summary>
		public NonSeekableReadStream(Stream stream)
			: base(stream) {
		}

		/// <summary>Reports that the wrapped input cannot be repositioned.</summary>
		public override bool CanSeek => false;

		/// <summary>Rejects length queries so decoding cannot depend on seekable framing.</summary>
		public override long Length => throw new NotSupportedException();

		/// <summary>Rejects cursor queries and updates while retaining sequential reads.</summary>
		public override long Position {
			get => throw new NotSupportedException();
			set => throw new NotSupportedException();
		}

		/// <summary>Rejects cursor repositioning to expose accidental seek dependencies.</summary>
		public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
	}

	/// <summary>Provides mixed-size members whose order and reference wrappers can be configured.</summary>
	public sealed class WireItem {
		public string First { get; set; }
		public int Second { get; set; }
	}

	/// <summary>Provides a link used to verify cyclic object identity after factory reconstruction.</summary>
	public sealed class LinkedItem {
		public int Value { get; set; }
		public LinkedItem Next { get; set; }
	}

	/// <summary>Contains a fixed-size child to verify composite dependency sizing.</summary>
	public sealed class ConstantParent {
		public ConstantChild Child { get; set; }
	}

	/// <summary>Provides the fixed-size leaf of the nested composite sizing scenario.</summary>
	public sealed class ConstantChild {
		public int Value { get; set; }
	}

	/// <summary>Defines fields and properties whose names are hidden by the derived test type.</summary>
	public class HiddenBase {
		public int Value;
		public int Other { get; set; }
	}

	/// <summary>Hides base members with opposite member kinds to verify declaring-type identity.</summary>
	public sealed class HiddenDerived : HiddenBase {
		public new int Value { get; set; }
		public new int Other;
	}

	/// <summary>Provides nested collections, aliases and nulls for reference-preservation coverage.</summary>
	public sealed class CollectionItem {
		public Dictionary<string, List<long>> Values { get; set; }
		public Dictionary<string, List<long>> Alias { get; set; }
		public Dictionary<string, List<long>> Optional { get; set; }
	}

	/// <summary>Encodes an independently configured offset that registration metadata cannot reconstruct.</summary>
	public sealed class ConfiguredIntSerializer : ItemSerializerBase<int> {
		public int Offset { get; set; }

		/// <summary>Reports the fixed integer payload size independently of its configured offset.</summary>
		public override long CalculateSize(SerializationContext context, int item) => sizeof(int);

		/// <summary>Writes the integer after applying this serializer instance's offset.</summary>
		public override void Serialize(int item, EndianBinaryWriter writer, SerializationContext context) => writer.Write(item + Offset);

		/// <summary>Restores the integer by removing this serializer instance's offset.</summary>
		public override int Deserialize(EndianBinaryReader reader, SerializationContext context) => reader.ReadInt32() - Offset;
	}

	/// <summary>Provides a generic payload for parameterless and injected serializer registrations.</summary>
	public sealed class Box<T> {
		public T Value { get; set; }
	}

	/// <summary>Provides a nondefault enum value for exact registration and payload round trips.</summary>
	public enum WireEnum {
		Value = 42
	}

	/// <summary>Declares the polymorphic base used to verify factory ownership during subtype resolution.</summary>
	[KnownSubType<WireVariant>]
	public abstract class WireBase {
	}

	/// <summary>Provides the known concrete subtype for polymorphic factory round trips.</summary>
	public sealed class WireVariant : WireBase {
		public int Value { get; set; }
	}

	/// <summary>Serializes a generic box through its constructor-injected component serializer.</summary>
	public sealed class GenericBoxSerializer<T> : ItemSerializerBase<Box<T>> {
		private readonly IItemSerializer<T> _serializer;

		/// <summary>Retains the component serializer supplied by factory constructor injection.</summary>
		public GenericBoxSerializer(IItemSerializer<T> serializer) => _serializer = serializer;

		/// <summary>Calculates the boxed value size through the injected component serializer.</summary>
		public override long CalculateSize(SerializationContext context, Box<T> item) => _serializer.CalculateSize(context, item.Value);

		/// <summary>Writes the boxed value through the injected component serializer.</summary>
		public override void Serialize(Box<T> item, EndianBinaryWriter writer, SerializationContext context) => _serializer.Serialize(item.Value, writer, context);

		/// <summary>Reads the component value and restores its containing box.</summary>
		public override Box<T> Deserialize(EndianBinaryReader reader, SerializationContext context) => new() { Value = _serializer.Deserialize(reader, context) };
	}

	/// <summary>Makes parameterless and injected construction produce distinguishable box encodings.</summary>
	public sealed class BoxSerializer : ItemSerializerBase<Box<long>> {
		/// <summary>Creates the unshifted encoding used by parameterless registration.</summary>
		public BoxSerializer() { }

		/// <summary>Selects an offset encoding to make injected construction observable.</summary>
		public BoxSerializer(IItemSerializer<long> serializer) => Offset = 1000;
		public int Offset { get; }

		/// <summary>Reports the fixed size of a boxed long payload.</summary>
		public override long CalculateSize(SerializationContext context, Box<long> item) => sizeof(long);

		/// <summary>Writes the boxed value with the constructor-selected offset.</summary>
		public override void Serialize(Box<long> item, EndianBinaryWriter writer, SerializationContext context) => writer.Write(item.Value + Offset);

		/// <summary>Restores the boxed value using the constructor-selected offset.</summary>
		public override Box<long> Deserialize(EndianBinaryReader reader, SerializationContext context) => new() { Value = reader.ReadInt64() - Offset };
	}
}
