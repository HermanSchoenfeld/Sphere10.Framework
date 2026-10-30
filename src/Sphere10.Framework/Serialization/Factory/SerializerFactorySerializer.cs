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
using System.Reflection;
using Sphere10.Framework.Mapping;

namespace Sphere10.Framework;

/// <summary>Serializes a factory's type codes, registrations and serializer construction settings.</summary>
/// <remarks>Captured delegates and independently configured serializer instances cannot be reconstructed.</remarks>
public sealed class SerializerFactorySerializer : ItemSerializerBase<SerializerFactory> {
	private readonly PrimitiveSerializer<long> _longSerializer;
	private readonly PrimitiveSerializer<int> _integerSerializer;
	private readonly RegistrationSerializer _registrationSerializer;

	public SerializerFactorySerializer() {
		// Retain the shared prefix and registration components for every factory operation.
		_longSerializer = PrimitiveSerializer<long>.Instance;
		_integerSerializer = PrimitiveSerializer<int>.Instance;
		_registrationSerializer = RegistrationSerializer.Instance;
	}

	public static SerializerFactorySerializer Instance { get; } = new();

	public override long CalculateSize(SerializationContext context, SerializerFactory item) {
		// Capture the same registration records used by Serialize before sizing their nested members.
		var registrations = Capture(item);
		var size = _longSerializer.CalculateSize(context, item.MinGeneratedTypeCode)
			+ _integerSerializer.CalculateSize(context, registrations.Length);
		foreach (var row in registrations)
			size += _registrationSerializer.CalculateSize(context, row);
		return size;
	}

	public override void Serialize(SerializerFactory item, EndianBinaryWriter writer, SerializationContext context) {
		// Reject unsupported construction choices before writing any factory bytes.
		var registrations = Capture(item);

		// Preserve the generated-code boundary and every registration's original type code.
		_longSerializer.Serialize(item.MinGeneratedTypeCode, writer, context);
		_integerSerializer.Serialize(registrations.Length, writer, context);
		foreach (var row in registrations)
			_registrationSerializer.Serialize(row, writer, context);
	}

	public override SerializerFactory Deserialize(EndianBinaryReader reader, SerializationContext context) {
		// Read the complete registration table before resolving dependencies between serializers.
		var minGeneratedTypeCode = _longSerializer.Deserialize(reader, context);
		var count = ReadCount(reader, context, _registrationSerializer.MinimumSize, _integerSerializer);

		// Nonseekable input cannot validate its claimed count up front; grow only as complete rows are decoded.
		var registrations = new List<Registration>(reader.BaseStream.CanSeek ? count : 0);
		for (var index = 0; index < count; index++)
			registrations.Add(_registrationSerializer.Deserialize(reader, context));

		// Rebuild the factory only after all referenced registration records are available.
		return Build(minGeneratedTypeCode, registrations.ToArray());
	}

	private static Registration[] Capture(SerializerFactory factory) {
		Guard.ArgumentNotNull(factory, nameof(factory));

		// Reject inconsistent registration identity before activating serializers or writing any bytes.
		var registrations = factory.RegisteredTypes.Select(factory.FindRegistration).ToArray();
		foreach (var source in registrations) {
			if (!ReferenceEquals(source.Owner, factory))
				throw new NotSupportedException("A serializer registration uses another factory as its owner and cannot be reconstructed.");
			if (!factory.TryGetRegistration(source.TypeCode, out var keyed) || !ReferenceEquals(source, keyed))
				throw new NotSupportedException("A serializer registration has an inconsistent type code and cannot be reconstructed.");
		}

		// Recognize shared built-in registrations without serializing their implementation state.
		var defaults = SerializerFactory.Default.RegisteredTypes.ToDictionary(type => type, SerializerFactory.Default.FindRegistration);

		// Record known construction choices; captured delegates and independent instances have no such recipe.
		return registrations.Select(source => {
			var row = new Registration { TypeCode = source.TypeCode, DataType = source.DataType, SerializerType = source.SerializerType };
			if (defaults.TryGetValue(source.DataType, out var standard) && source.SerializerType == standard.SerializerType && ReferenceEquals(source.Factory, standard.Factory))
				row.Construction = ConstructionKind.Default;
			else
				row.Construction = source.Construction switch {
					SerializerFactory.RegistrationConstruction.Constructor => ConstructionKind.Constructor,
					SerializerFactory.RegistrationConstruction.Parameterless => ConstructionKind.Parameterless,
					SerializerFactory.RegistrationConstruction.Composite => ConstructionKind.Composite,
					SerializerFactory.RegistrationConstruction.Polymorphic => ConstructionKind.Polymorphic,
					_ => throw new NotSupportedException($"Serializer registration for {source.DataType} uses a custom delegate or serializer instance that cannot be reconstructed.")
				};

			// Composite bindings carry the actual wire order and each member's serializer wrappers.
			if (row.Construction == ConstructionKind.Composite) {
				if (!source.SerializerType.IsConstructedGenericTypeOf(typeof(CompositeSerializer<>)) || source.DataType.GetConstructor(Type.EmptyTypes) == null)
					throw new NotSupportedException("Composite reconstruction requires a public parameterless constructor.");
				var serializer = factory.GetPureSerializer(source.DataType);
				var bindings = (MemberSerializationBinding[])source.SerializerType.GetProperty(nameof(CompositeSerializer<object>.MemberBindings)).GetValue(serializer);
				row.Members = bindings.Select(binding => CaptureMember(binding, factory)).ToArray();
			}
			// A polymorphic serializer must dispatch through the factory being captured.
			if (row.Construction == ConstructionKind.Polymorphic) {
				var serializer = factory.GetPureSerializer(source.DataType);
				if (!ReferenceEquals(source.SerializerType.GetProperty(nameof(PolymorphicSerializer<object>.Factory)).GetValue(serializer), factory))
					throw new NotSupportedException("A polymorphic registration uses another serializer factory.");
			}
			return row;
		}).ToArray();
	}

	private static SerializerFactory Build(long minGeneratedTypeCode, Registration[] registrations) {
		// Register every type first so references between composite serializers can be resolved later.
		var factory = new SerializerFactory().SetMinTypeCode(minGeneratedTypeCode);
		var composites = new Dictionary<Type, IItemSerializer>();
		foreach (var row in registrations) {
			switch (row.Construction) {
				case ConstructionKind.Default:
					var standard = SerializerFactory.Default.FindRegistration(row.DataType);
					if (standard.SerializerType != row.SerializerType)
						throw new InvalidDataException("The standard serializer registration changed.");
					factory.RegisterInternal(row.TypeCode, row.DataType, row.SerializerType, null, standard.Factory, standard.Construction);
					break;
				case ConstructionKind.Constructor:
					factory.Register(row.DataType, row.SerializerType, row.TypeCode);
					break;
				case ConstructionKind.Parameterless:
					typeof(SerializerFactory).GetMethods().Single(method => method.Name == nameof(SerializerFactory.Register) && method.IsGenericMethodDefinition
						&& method.GetGenericArguments().Length == 2 && method.GetParameters().Length == 1)
						.MakeGenericMethod(row.DataType, row.SerializerType).Invoke(factory, new object[] { (long?)row.TypeCode });
					break;
				case ConstructionKind.Composite:
					if (row.SerializerType != typeof(CompositeSerializer<>).MakeGenericType(row.DataType))
						throw new InvalidDataException("Invalid composite serializer registration.");
					var composite = CompositeSerializer.Create(row.DataType);
					composites.Add(row.DataType, composite);
					factory.RegisterInternal(row.TypeCode, row.DataType, row.SerializerType, composite, null, SerializerFactory.RegistrationConstruction.Composite);
					break;
				case ConstructionKind.Polymorphic:
					if (!row.DataType.IsAbstract || row.SerializerType != typeof(PolymorphicSerializer<>).MakeGenericType(row.DataType))
						throw new InvalidDataException("Invalid polymorphic serializer registration.");
					factory.RegisterInternal(row.TypeCode, row.DataType, row.SerializerType, null, (registration, type) => PolymorphicSerializer.Create(registration.Owner, type),
						SerializerFactory.RegistrationConstruction.Polymorphic);
					break;
				default:
					throw new InvalidDataException("Unknown serializer construction kind.");
			}
		}
		// Resolve composite instances before attaching bindings, including self-referential member graphs.
		foreach (var (type, _) in composites)
			factory.GetPureSerializer(type);

		// Configure member dependencies first: composite configuration captures their fixed-size characteristics.
		var compositeRows = registrations.Where(row => row.Construction == ConstructionKind.Composite).ToDictionary(row => row.DataType);
		var configuring = new HashSet<Type>();
		var configured = new HashSet<Type>();
		foreach (var type in compositeRows.Keys)
			ConfigureComposite(type);

		// Make registrations read-only only after their member bindings are complete.
		factory.FinishRegistrations();
		return factory;

		void ConfigureComposite(Type type) {
			if (configured.Contains(type) || !compositeRows.TryGetValue(type, out var row) || !configuring.Add(type))
				return;

			// A cycle stops at the still-unconfigured serializer, retaining the existing variable-size behavior.
			foreach (var member in row.Members)
				ConfigureDependencies(member.Resolve().PropertyType);
			CompositeSerializer.Configure(composites[type], type, row.Members.Select(member => member.Build(factory)));
			configuring.Remove(type);
			configured.Add(type);
		}

		void ConfigureDependencies(Type type) {
			// Arrays and generic wrappers can cache their element serializers while being activated.
			if (type.HasElementType)
				ConfigureDependencies(type.GetElementType());
			foreach (var argument in type.GetGenericArguments())
				ConfigureDependencies(argument);
			ConfigureComposite(type);
		}
	}

	private static Member CaptureMember(MemberSerializationBinding binding, SerializerFactory factory) {
		// Preserve reflection identity separately from the optional serializer decorators.
		var serializer = binding.Serializer;
		var row = new Member { Name = binding.Member.Name, DeclaringType = binding.Member.DeclaringType, IsField = binding.Member.IsField };
		// Unwrap the reference layer while retaining its exact null/reference policy.
		row.IsReference = serializer.GetType().IsConstructedGenericTypeOf(typeof(ReferenceSerializer<>));
		if (row.IsReference) {
			row.ReferenceMode = (ReferenceSerializerMode)serializer.GetType().GetProperty(nameof(ReferenceSerializer<object>.Mode)).GetValue(serializer);
			serializer = ((IItemSerializerDecorator)serializer).InternalSerializer;
		}
		// Polymorphic dispatch must use this factory and ultimately resolve to its registered pure serializer.
		row.IsPolymorphic = serializer.GetType().IsConstructedGenericTypeOf(typeof(PolymorphicSerializer<>));
		if (row.IsPolymorphic) {
			if (!ReferenceEquals(serializer.GetType().GetProperty(nameof(PolymorphicSerializer<object>.Factory)).GetValue(serializer), factory))
				throw new NotSupportedException("A composite member uses another serializer factory.");
			if (!binding.Member.PropertyType.IsAbstract)
				serializer = (IItemSerializer)serializer.GetType().GetProperty(nameof(PolymorphicSerializer<object>.PureSerializer)).GetValue(serializer);
		}
		// Instance-specific member configuration cannot be reproduced from registration data alone.
		if (!ReferenceEquals(serializer, factory.GetPureSerializer(binding.Member.PropertyType)))
			throw new NotSupportedException($"Member {binding.Member.Name} has independent serializer configuration; provide explicit application configuration.");
		return row;
	}

	private static int ReadCount(EndianBinaryReader reader, SerializationContext context, long minimumSize, PrimitiveSerializer<int> integerSerializer) {
		// Validate the claimed row count against the available bytes when the source exposes its length.
		var count = integerSerializer.Deserialize(reader, context);
		if (count < 0 || reader.BaseStream.CanSeek && count > (reader.BaseStream.Length - reader.BaseStream.Position) / minimumSize)
			throw new InvalidDataException("Invalid serializer factory row count.");
		return count;
	}

	internal enum ConstructionKind : byte {
		Default,
		Constructor,
		Parameterless,
		Composite,
		Polymorphic
	}

	internal sealed class Registration {
		public long TypeCode { get; set; }
		public Type DataType { get; set; }
		public Type SerializerType { get; set; }
		public ConstructionKind Construction { get; set; }
		internal Member[] Members { get; set; } = Array.Empty<Member>();
	}

	internal sealed class Member {
		public string Name { get; set; }
		public Type DeclaringType { get; set; }
		public bool IsField { get; set; }
		public bool IsPolymorphic { get; set; }
		public bool IsReference { get; set; }
		public ReferenceSerializerMode ReferenceMode { get; set; }

		internal MemberSerializationBinding Build(SerializerFactory factory) {
			var member = Resolve();

			// Reapply dispatch and reference decorators in the reverse order used when capturing them.
			var serializer = factory.GetPureSerializer(member.PropertyType);
			if (IsPolymorphic && !serializer.GetType().IsConstructedGenericTypeOf(typeof(PolymorphicSerializer<>)))
				serializer = PolymorphicSerializer.Create(factory, member.PropertyType);
			if (IsReference)
				serializer = serializer.AsReferenceSerializer(ReferenceMode);
			return new MemberSerializationBinding(member, serializer);
		}

		internal Sphere10.Framework.Mapping.Member Resolve() {
			// Resolve only the member declared by the recorded type; inherited names can identify different members.
			const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
			MemberInfo info = IsField ? DeclaringType.GetField(Name, flags) : DeclaringType.GetProperty(Name, flags);
			return (info ?? throw new InvalidDataException("A persisted serializer member is missing.")).ToMember();
		}
	}

	internal sealed class RegistrationSerializer : ItemSerializerBase<Registration> {
		private readonly PrimitiveSerializer<long> _longSerializer;
		private readonly PrimitiveSerializer<int> _integerSerializer;
		private readonly TypeSerializer _typeSerializer;
		private readonly EnumSerializer<ConstructionKind> _constructionSerializer;
		private readonly MemberSerializer _memberSerializer;

		private RegistrationSerializer() {
			// Capture the registration fields and its nested member serializer once.
			_longSerializer = PrimitiveSerializer<long>.Instance;
			_integerSerializer = PrimitiveSerializer<int>.Instance;
			_typeSerializer = TypeSerializer.Instance;
			_constructionSerializer = EnumSerializer<ConstructionKind>.Instance;
			_memberSerializer = MemberSerializer.Instance;
			MinimumSize = _longSerializer.ConstantSize + _constructionSerializer.ConstantSize
				+ _integerSerializer.ConstantSize + StringSerializer.Unicode.CalculateSize(string.Empty) * 2;
		}

		public static RegistrationSerializer Instance { get; } = new();

		internal long MinimumSize { get; }

		public override long CalculateSize(SerializationContext context, Registration item) {
			// The registration prefix describes construction; its ordered member rows describe composite layout.
			var size = _longSerializer.CalculateSize(context, item.TypeCode) + _typeSerializer.CalculateSize(context, item.DataType)
				+ _typeSerializer.CalculateSize(context, item.SerializerType) + _constructionSerializer.CalculateSize(context, item.Construction)
				+ _integerSerializer.CalculateSize(context, item.Members.Length);
			foreach (var member in item.Members)
				size += _memberSerializer.CalculateSize(context, member);
			return size;
		}

		public override void Serialize(Registration item, EndianBinaryWriter writer, SerializationContext context) {
			// Write registration identity and the constructor choice before its composite member table.
			_longSerializer.Serialize(item.TypeCode, writer, context);
			_typeSerializer.Serialize(item.DataType, writer, context);
			_typeSerializer.Serialize(item.SerializerType, writer, context);
			_constructionSerializer.Serialize(item.Construction, writer, context);

			// Member order is part of the wire layout and must not be inferred again from reflection.
			_integerSerializer.Serialize(item.Members.Length, writer, context);
			foreach (var member in item.Members)
				_memberSerializer.Serialize(member, writer, context);
		}

		public override Registration Deserialize(EndianBinaryReader reader, SerializationContext context) {
			// Validate the member count before allocating the ordered member table.
			var item = new Registration {
				TypeCode = _longSerializer.Deserialize(reader, context),
				DataType = _typeSerializer.Deserialize(reader, context),
				SerializerType = _typeSerializer.Deserialize(reader, context),
				Construction = _constructionSerializer.Deserialize(reader, context)
			};

			// The member serializer owns each row's identity and settings; unbounded input grows incrementally.
			var count = ReadCount(reader, context, _memberSerializer.MinimumSize, _integerSerializer);
			var members = new List<Member>(reader.BaseStream.CanSeek ? count : 0);
			for (var index = 0; index < count; index++)
				members.Add(_memberSerializer.Deserialize(reader, context));
			item.Members = members.ToArray();
			return item;
		}
	}

	internal sealed class MemberSerializer : ItemSerializerBase<Member> {
		private readonly StringSerializer _stringSerializer;
		private readonly TypeSerializer _typeSerializer;
		private readonly PrimitiveSerializer<bool> _booleanSerializer;
		private readonly EnumSerializer<ReferenceSerializerMode> _referenceModeSerializer;

		private MemberSerializer() {
			// Preserve reflection identity and decorator settings through the same cached components.
			_stringSerializer = StringSerializer.UTF8;
			_typeSerializer = TypeSerializer.Instance;
			_booleanSerializer = PrimitiveSerializer<bool>.Instance;
			_referenceModeSerializer = EnumSerializer<ReferenceSerializerMode>.Instance;
			MinimumSize = _booleanSerializer.ConstantSize * 3 + _referenceModeSerializer.ConstantSize
				+ _stringSerializer.CalculateSize(string.Empty) + StringSerializer.Unicode.CalculateSize(string.Empty);
		}

		public static MemberSerializer Instance { get; } = new();

		internal long MinimumSize { get; }

		// Reflection identity and decorator flags have the same representation for every member kind.
		public override long CalculateSize(SerializationContext context, Member item)
			=> _stringSerializer.CalculateSize(context, item.Name) + _typeSerializer.CalculateSize(context, item.DeclaringType)
				+ _booleanSerializer.CalculateSize(context, item.IsField) + _booleanSerializer.CalculateSize(context, item.IsPolymorphic)
				+ _booleanSerializer.CalculateSize(context, item.IsReference) + _referenceModeSerializer.CalculateSize(context, item.ReferenceMode);

		public override void Serialize(Member item, EndianBinaryWriter writer, SerializationContext context) {
			// Member identity comes before the serializer decorators applied to its declared value type.
			_stringSerializer.Serialize(item.Name, writer, context);
			_typeSerializer.Serialize(item.DeclaringType, writer, context);
			_booleanSerializer.Serialize(item.IsField, writer, context);

			// Preserve these flags even when a particular wrapper is absent from this member.
			_booleanSerializer.Serialize(item.IsPolymorphic, writer, context);
			_booleanSerializer.Serialize(item.IsReference, writer, context);
			_referenceModeSerializer.Serialize(item.ReferenceMode, writer, context);
		}

		// The registration serializer preserves row order; this row restores identity and wrapper settings.
		public override Member Deserialize(EndianBinaryReader reader, SerializationContext context)
			=> new() {
				Name = _stringSerializer.Deserialize(reader, context),
				DeclaringType = _typeSerializer.Deserialize(reader, context),
				IsField = _booleanSerializer.Deserialize(reader, context),
				IsPolymorphic = _booleanSerializer.Deserialize(reader, context),
				IsReference = _booleanSerializer.Deserialize(reader, context),
				ReferenceMode = _referenceModeSerializer.Deserialize(reader, context)
			};
	}
}
