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
	private readonly RegistrationSerializer _registrationSerializer;

	/// <summary>Creates a factory serializer with its own registration and member component serializers.</summary>
	public SerializerFactorySerializer() {
		// Each factory serializer owns its hierarchical registration and member components.
		_registrationSerializer = new RegistrationSerializer();
	}

	/// <summary>Calculates the encoded size of reconstructible registrations and their ordered member bindings.</summary>
	public override long CalculateSize(SerializationContext context, SerializerFactory item) {
		// Capture the same registration records used by Serialize before sizing their nested members.
		var registrations = Capture(item);
		var size = PrimitiveSerializer<long>.Instance.CalculateSize(context, item.MinGeneratedTypeCode)
			+ PrimitiveSerializer<int>.Instance.CalculateSize(context, registrations.Length);
		foreach (var row in registrations)
			size += _registrationSerializer.CalculateSize(context, row);
		return size;
	}

	/// <summary>Writes original type codes and construction settings, rejecting unsupported registrations before output.</summary>
	public override void Serialize(SerializerFactory item, EndianBinaryWriter writer, SerializationContext context) {
		// Reject unsupported construction choices before writing any factory bytes.
		var registrations = Capture(item);

		// Preserve the generated-code boundary and every registration's original type code.
		PrimitiveSerializer<long>.Instance.Serialize(item.MinGeneratedTypeCode, writer, context);
		PrimitiveSerializer<int>.Instance.Serialize(registrations.Length, writer, context);
		foreach (var row in registrations)
			_registrationSerializer.Serialize(row, writer, context);
	}

	/// <summary>Reads the registration table and reconstructs a finished factory with its original serializer bindings.</summary>
	public override SerializerFactory Deserialize(EndianBinaryReader reader, SerializationContext context) {
		// Read the complete registration table before resolving dependencies between serializers.
		var minGeneratedTypeCode = PrimitiveSerializer<long>.Instance.Deserialize(reader, context);
		var count = ReadCount(reader, context, _registrationSerializer.MinimumSize);

		// Nonseekable input cannot validate its claimed count up front; grow only as complete rows are decoded.
		var registrations = new List<Registration>(reader.BaseStream.CanSeek ? count : 0);
		for (var index = 0; index < count; index++)
			registrations.Add(_registrationSerializer.Deserialize(reader, context));

		// Rebuild the factory only after all referenced registration records are available.
		return Build(minGeneratedTypeCode, registrations.ToArray());
	}

	// Captures supported registration provenance and composite bindings without persisting arbitrary executable state.
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

	// Recreates exact registrations before configuring composite dependencies and finishing the reconstructed factory.
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

		// Configures one composite after its dependencies, terminating recursive member graphs at an active node.
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

		// Configures composite element and generic-argument dependencies before their enclosing serializer activates.
		void ConfigureDependencies(Type type) {
			// Arrays and generic wrappers can cache their element serializers while being activated.
			if (type.HasElementType)
				ConfigureDependencies(type.GetElementType());
			foreach (var argument in type.GetGenericArguments())
				ConfigureDependencies(argument);
			ConfigureComposite(type);
		}
	}

	// Captures a member's declared identity and serializer wrappers, rejecting independent member configuration.
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

	// Reads a nonnegative row count and bounds it by available bytes when the source exposes its length.
	private static int ReadCount(EndianBinaryReader reader, SerializationContext context, long minimumSize) {
		// Validate the claimed row count against the available bytes when the source exposes its length.
		var count = PrimitiveSerializer<int>.Instance.Deserialize(reader, context);
		if (count < 0 || reader.BaseStream.CanSeek && count > (reader.BaseStream.Length - reader.BaseStream.Position) / minimumSize)
			throw new InvalidDataException("Invalid serializer factory row count.");
		return count;
	}

	/// <summary>Identifies the supported construction path recorded for a persisted serializer registration.</summary>
	internal enum ConstructionKind : byte {
		/// <summary>Reuses the corresponding built-in registration's construction function.</summary>
		Default,

		/// <summary>Activates the serializer through its compatible registered-type constructor.</summary>
		Constructor,

		/// <summary>Activates a serializer registered through its parameterless constructor.</summary>
		Parameterless,

		/// <summary>Reconstructs a composite serializer from its ordered member records.</summary>
		Composite,

		/// <summary>Recreates polymorphic dispatch through the reconstructed factory.</summary>
		Polymorphic
	}

	/// <summary>The persisted type identity, construction choice and ordered bindings of one factory registration.</summary>
	internal sealed class Registration {
		/// <summary>The original numeric code used to identify this registration's data type.</summary>
		public long TypeCode { get; set; }

		/// <summary>The registered CLR data type, including open generic definitions when applicable.</summary>
		public Type DataType { get; set; }

		/// <summary>The CLR serializer type to activate for this registration.</summary>
		public Type SerializerType { get; set; }

		/// <summary>The supported construction path used to recreate the registered serializer.</summary>
		public ConstructionKind Construction { get; set; }

		/// <summary>The composite member bindings in their original serialization order; empty for other constructions.</summary>
		internal Member[] Members { get; set; } = Array.Empty<Member>();
	}

	/// <summary>The declared reflection identity and serializer wrapper settings of one composite member.</summary>
	internal sealed class Member {
		/// <summary>The name of the field or property declared by <see cref="DeclaringType"/>.</summary>
		public string Name { get; set; }

		/// <summary>The CLR type that declares the recorded member, distinguishing hidden or inherited names.</summary>
		public Type DeclaringType { get; set; }

		/// <summary>Whether the member is a field rather than a property.</summary>
		public bool IsField { get; set; }

		/// <summary>Whether the member serializer includes polymorphic dispatch through its owning factory.</summary>
		public bool IsPolymorphic { get; set; }

		/// <summary>Whether the member serializer is wrapped by a reference serializer.</summary>
		public bool IsReference { get; set; }

		/// <summary>The original reference handling flags, used when <see cref="IsReference"/> is true.</summary>
		public ReferenceSerializerMode ReferenceMode { get; set; }

		// Rebuilds this member binding against the reconstructed factory, preserving decorator order and flags.
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

		// Resolves the recorded declared field or property and rejects missing CLR members.
		internal Sphere10.Framework.Mapping.Member Resolve() {
			// Resolve only the member declared by the recorded type; inherited names can identify different members.
			const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
			MemberInfo info = IsField ? DeclaringType.GetField(Name, flags) : DeclaringType.GetProperty(Name, flags);
			return (info ?? throw new InvalidDataException("A persisted serializer member is missing.")).ToMember();
		}
	}

	/// <summary>Serializes one registration and delegates its ordered composite bindings to the member serializer.</summary>
	internal sealed class RegistrationSerializer : ItemSerializerBase<Registration> {
		private readonly MemberSerializer _memberSerializer;

		/// <summary>Creates the member component and calculates the minimum encoded registration width.</summary>
		internal RegistrationSerializer() {
			// Each registration serializer owns the component used for its nested member rows.
			_memberSerializer = new MemberSerializer();
			MinimumSize = PrimitiveSerializer<long>.Instance.ConstantSize + EnumSerializer<ConstructionKind>.Instance.ConstantSize
				+ PrimitiveSerializer<int>.Instance.ConstantSize + StringSerializer.Unicode.CalculateSize(string.Empty) * 2;
		}

		/// <summary>The smallest possible registration encoding, used to validate a claimed row count.</summary>
		internal long MinimumSize { get; }

		/// <summary>Calculates the registration prefix and the sizes of its ordered member records.</summary>
		public override long CalculateSize(SerializationContext context, Registration item) {
			// The registration prefix describes construction; its ordered member rows describe composite layout.
			var size = PrimitiveSerializer<long>.Instance.CalculateSize(context, item.TypeCode) + TypeSerializer.Instance.CalculateSize(context, item.DataType)
				+ TypeSerializer.Instance.CalculateSize(context, item.SerializerType) + EnumSerializer<ConstructionKind>.Instance.CalculateSize(context, item.Construction)
				+ PrimitiveSerializer<int>.Instance.CalculateSize(context, item.Members.Length);
			foreach (var member in item.Members)
				size += _memberSerializer.CalculateSize(context, member);
			return size;
		}

		/// <summary>Writes registration identity and construction followed by its ordered member records.</summary>
		public override void Serialize(Registration item, EndianBinaryWriter writer, SerializationContext context) {
			// Write registration identity and the constructor choice before its composite member table.
			PrimitiveSerializer<long>.Instance.Serialize(item.TypeCode, writer, context);
			TypeSerializer.Instance.Serialize(item.DataType, writer, context);
			TypeSerializer.Instance.Serialize(item.SerializerType, writer, context);
			EnumSerializer<ConstructionKind>.Instance.Serialize(item.Construction, writer, context);

			// Member order is part of the wire layout and must not be inferred again from reflection.
			PrimitiveSerializer<int>.Instance.Serialize(item.Members.Length, writer, context);
			foreach (var member in item.Members)
				_memberSerializer.Serialize(member, writer, context);
		}

		/// <summary>Reads a registration and its validated member table without resolving serializer dependencies.</summary>
		public override Registration Deserialize(EndianBinaryReader reader, SerializationContext context) {
			// Validate the member count before allocating the ordered member table.
			var item = new Registration {
				TypeCode = PrimitiveSerializer<long>.Instance.Deserialize(reader, context),
				DataType = TypeSerializer.Instance.Deserialize(reader, context),
				SerializerType = TypeSerializer.Instance.Deserialize(reader, context),
				Construction = EnumSerializer<ConstructionKind>.Instance.Deserialize(reader, context)
			};

			// The member serializer owns each row's identity and settings; unbounded input grows incrementally.
			var count = ReadCount(reader, context, _memberSerializer.MinimumSize);
			var members = new List<Member>(reader.BaseStream.CanSeek ? count : 0);
			for (var index = 0; index < count; index++)
				members.Add(_memberSerializer.Deserialize(reader, context));
			item.Members = members.ToArray();
			return item;
		}
	}

	/// <summary>Serializes a composite member's declared identity and its reference and polymorphic wrappers.</summary>
	internal sealed class MemberSerializer : ItemSerializerBase<Member> {
		/// <summary>Calculates the minimum encoded member width from the component serializers.</summary>
		internal MemberSerializer() {
			// Derive each row's minimum width from the existing primitive and value serializers.
			MinimumSize = PrimitiveSerializer<bool>.Instance.ConstantSize * 3 + EnumSerializer<ReferenceSerializerMode>.Instance.ConstantSize
				+ StringSerializer.UTF8.CalculateSize(string.Empty) + StringSerializer.Unicode.CalculateSize(string.Empty);
		}

		/// <summary>The smallest possible member encoding, used to validate a claimed binding count.</summary>
		internal long MinimumSize { get; }

		/// <summary>Calculates the encoded size of reflection identity and serializer wrapper settings.</summary>
		public override long CalculateSize(SerializationContext context, Member item) {
			// Reflection identity and decorator flags have the same representation for every member kind.
			return StringSerializer.UTF8.CalculateSize(context, item.Name) + TypeSerializer.Instance.CalculateSize(context, item.DeclaringType)
				+ PrimitiveSerializer<bool>.Instance.CalculateSize(context, item.IsField) + PrimitiveSerializer<bool>.Instance.CalculateSize(context, item.IsPolymorphic)
				+ PrimitiveSerializer<bool>.Instance.CalculateSize(context, item.IsReference) + EnumSerializer<ReferenceSerializerMode>.Instance.CalculateSize(context, item.ReferenceMode);
		}

		/// <summary>Writes declared member identity followed by its polymorphic and reference handling flags.</summary>
		public override void Serialize(Member item, EndianBinaryWriter writer, SerializationContext context) {
			// Member identity comes before the serializer decorators applied to its declared value type.
			StringSerializer.UTF8.Serialize(item.Name, writer, context);
			TypeSerializer.Instance.Serialize(item.DeclaringType, writer, context);
			PrimitiveSerializer<bool>.Instance.Serialize(item.IsField, writer, context);

			// Preserve these flags even when a particular wrapper is absent from this member.
			PrimitiveSerializer<bool>.Instance.Serialize(item.IsPolymorphic, writer, context);
			PrimitiveSerializer<bool>.Instance.Serialize(item.IsReference, writer, context);
			EnumSerializer<ReferenceSerializerMode>.Instance.Serialize(item.ReferenceMode, writer, context);
		}

		/// <summary>Reads member identity and wrapper settings without resolving the CLR member or activating its serializer.</summary>
		public override Member Deserialize(EndianBinaryReader reader, SerializationContext context) {
			// The registration serializer preserves row order; this row restores identity and wrapper settings.
			return new Member {
				Name = StringSerializer.UTF8.Deserialize(reader, context),
				DeclaringType = TypeSerializer.Instance.Deserialize(reader, context),
				IsField = PrimitiveSerializer<bool>.Instance.Deserialize(reader, context),
				IsPolymorphic = PrimitiveSerializer<bool>.Instance.Deserialize(reader, context),
				IsReference = PrimitiveSerializer<bool>.Instance.Deserialize(reader, context),
				ReferenceMode = EnumSerializer<ReferenceSerializerMode>.Instance.Deserialize(reader, context)
			};
		}
	}
}
